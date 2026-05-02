using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Mapgen.Analyzer.Mapper.Diagnostics;
using Mapgen.Analyzer.Mapper.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mapgen.Analyzer.Mapper.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AmbiguousConstructorCodeFixProvider))]
[Shared]
public class AmbiguousConstructorCodeFixProvider : CodeFixProvider
{
  private const string ConstructorIndentation = "  ";
  private const string StatementIndentation = "    ";

  public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.AmbiguousConstructorSelection];

  public override FixAllProvider GetFixAllProvider() => null!; // Disable batch fixing

  public override async Task RegisterCodeFixesAsync(CodeFixContext context)
  {
    var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
    if (root is null)
    {
      return;
    }

    var diagnostic = context.Diagnostics.First();
    var diagnosticSpan = diagnostic.Location.SourceSpan;
    var node = root.FindNode(diagnosticSpan);

    // Find the containing class
    var classDeclaration = node.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
    if (classDeclaration is null)
    {
      return;
    }

    // Find the method declaration
    var methodDeclaration = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
    if (methodDeclaration is null)
    {
      return;
    }

    // Get semantic model to analyze types
    var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
    if (semanticModel is null)
    {
      return;
    }

    // Get the method symbol to get the return type
    var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken);
    if (methodSymbol?.ReturnType is not INamedTypeSymbol returnType)
    {
      return;
    }

    // Get all public constructors
    var constructors = returnType.InstanceConstructors
      .Where(c => c.DeclaredAccessibility == Accessibility.Public)
      .OrderBy(c => c.Parameters.Length) // Order by parameter count: parameterless first
      .ToList();

    if (constructors.Count == 0)
    {
      return;
    }

    // Create nested actions for each constructor
    var nestedActions = ImmutableArray.CreateBuilder<CodeAction>();

    foreach (var constructor in constructors)
    {
      if (constructor.Parameters.Length == 0)
      {
        // Parameterless constructor - use UseEmptyConstructor()
        nestedActions.Add(CodeAction.Create(
          title: "Use empty constructor",
          createChangedDocument: c => AddUseEmptyConstructorAsync(context.Document, classDeclaration, c),
          equivalenceKey: nameof(AmbiguousConstructorCodeFixProvider) + "_UseEmptyConstructor"));
      }
      else
      {
        // Parameterized constructor - use UseConstructor(...)
        var signature = GetConstructorSignature(constructor, returnType.Name);
        var title = $"Use {signature}";
        
        nestedActions.Add(CodeAction.Create(
          title: title,
          createChangedDocument: c => AddUseConstructorAsync(context.Document, classDeclaration, constructor, c),
          equivalenceKey: nameof(AmbiguousConstructorCodeFixProvider) + "_UseConstructor_" + constructor.Parameters.Length));
      }
    }

    // Create parent action
    var parentTitle = $"Pick {returnType.Name} constructor";
    var codeAction = CodeAction.Create(
      title: parentTitle,
      nestedActions: nestedActions.ToImmutable(),
      isInlinable: false);

    context.RegisterCodeFix(codeAction, diagnostic);
  }

  private async Task<Document> AddUseEmptyConstructorAsync(
    Document document,
    ClassDeclarationSyntax classDeclaration,
    CancellationToken cancellationToken)
  {
    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
    if (root is null)
    {
      return document;
    }

    var newClass = GetOrCreateConstructor(classDeclaration);
    if (newClass is null)
    {
      return document;
    }

    var constructor = SyntaxHelpers.FindConstructor(newClass);
    if (constructor?.Body is null)
    {
      return document;
    }

    // Create UseEmptyConstructor() statement
    var statement = SyntaxFactory.ParseStatement("UseEmptyConstructor();")
      .WithLeadingTrivia(SyntaxFactory.Whitespace(StatementIndentation))
      .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));

    var newBody = constructor.Body.AddStatements(statement);
    
    // Ensure closing brace has proper formatting
    var closingBrace = newBody.CloseBraceToken
      .WithLeadingTrivia(SyntaxFactory.Whitespace(ConstructorIndentation));
    newBody = newBody.WithCloseBraceToken(closingBrace);
    
    var newConstructor = constructor.WithBody(newBody);
    var finalClass = newClass.ReplaceNode(constructor, newConstructor);

    var newRoot = root.ReplaceNode(classDeclaration, finalClass);
    return document.WithSyntaxRoot(newRoot);
  }

  private async Task<Document> AddUseConstructorAsync(
    Document document,
    ClassDeclarationSyntax classDeclaration,
    IMethodSymbol constructor,
    CancellationToken cancellationToken)
  {
    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
    if (root is null)
    {
      return document;
    }

    var newClass = GetOrCreateConstructor(classDeclaration);
    if (newClass is null)
    {
      return document;
    }

    var constructorDeclaration = SyntaxHelpers.FindConstructor(newClass);
    if (constructorDeclaration?.Body is null)
    {
      return document;
    }

    // Generate UseConstructor() call with lambda parameters
    var parameters = constructor.Parameters
      .Select(p => $"source => source.TODO")
      .ToList();

    string statementText;
    if (parameters.Count == 1)
    {
      statementText = $"UseConstructor({parameters[0]});";
    }
    else
    {
      // Multi-line format for multiple parameters
      var indentedParams = string.Join($",\n{StatementIndentation}  ", parameters);
      statementText = $"UseConstructor(\n{StatementIndentation}  {indentedParams}\n{StatementIndentation});";
    }

    var statement = SyntaxFactory.ParseStatement(statementText)
      .WithLeadingTrivia(SyntaxFactory.Whitespace(StatementIndentation))
      .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));

    // Find and annotate all TODO identifiers for rename functionality
    var todoIdentifiers = statement.DescendantNodes()
      .OfType<IdentifierNameSyntax>()
      .Where(id => id.Identifier.Text.StartsWith("TODO"))
      .ToList();

    if (todoIdentifiers.Any())
    {
      // Annotate the first TODO for automatic rename
      var firstTodo = todoIdentifiers.First();
      var annotatedIdentifier = firstTodo.WithAdditionalAnnotations(RenameAnnotation.Create());
      statement = statement.ReplaceNode(firstTodo, annotatedIdentifier);
    }

    var newBody = constructorDeclaration.Body.AddStatements(statement);
    
    // Ensure closing brace has proper formatting with newline
    var closingBrace = newBody.CloseBraceToken
      .WithLeadingTrivia(SyntaxFactory.Whitespace(ConstructorIndentation));
    newBody = newBody.WithCloseBraceToken(closingBrace);
    
    var newConstructor = constructorDeclaration.WithBody(newBody);
    var finalClass = newClass.ReplaceNode(constructorDeclaration, newConstructor);

    var newRoot = root.ReplaceNode(classDeclaration, finalClass);
    return document.WithSyntaxRoot(newRoot);
  }

  private ClassDeclarationSyntax? GetOrCreateConstructor(ClassDeclarationSyntax classDeclaration)
  {
    var existingConstructor = SyntaxHelpers.FindConstructor(classDeclaration);

    // If constructor exists with parameters, skip (invalid mapper per MAPPER010)
    if (existingConstructor?.ParameterList.Parameters.Count > 0)
    {
      return null;
    }

    // If constructor already exists, return as-is
    if (existingConstructor is not null)
    {
      return classDeclaration;
    }

    // Create new parameterless constructor
    var openBrace = SyntaxFactory.Token(
      SyntaxFactory.TriviaList(),
      SyntaxKind.OpenBraceToken,
      SyntaxFactory.TriviaList(SyntaxFactory.EndOfLine("\n")));
    
    var closeBrace = SyntaxFactory.Token(
      SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(ConstructorIndentation)),
      SyntaxKind.CloseBraceToken,
      SyntaxFactory.TriviaList());
    
    var body = SyntaxFactory.Block(openBrace, default, closeBrace);
    
    var constructor = SyntaxFactory.ConstructorDeclaration(classDeclaration.Identifier.Text)
      .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
      .WithParameterList(SyntaxFactory.ParameterList())
      .WithBody(body)
      .WithLeadingTrivia(
        SyntaxFactory.EndOfLine("\n"),  // Add blank line before constructor
        SyntaxFactory.Whitespace(ConstructorIndentation))
      .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));

    // Insert constructor after first method, or at the end if no methods exist
    var firstMethod = classDeclaration.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault();
    return firstMethod is not null
      ? classDeclaration.WithMembers(classDeclaration.Members.Insert(classDeclaration.Members.IndexOf(firstMethod) + 1, constructor))
      : classDeclaration.AddMembers(constructor);
  }

  private string GetConstructorSignature(IMethodSymbol constructor, string typeName)
  {
    if (constructor.Parameters.Length == 0)
    {
      return $"{typeName}()";
    }

    var parameters = constructor.Parameters
      .Select(p => $"{p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} {p.Name}");

    return $"{typeName}({string.Join(", ", parameters)})";
  }
}


