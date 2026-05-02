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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnmappedPropertyCodeFixProvider))]
[Shared]
public class UnmappedPropertyCodeFixProvider : CodeFixProvider
{
  public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.MissingPropertyMapping];

  public override FixAllProvider GetFixAllProvider() => null!; // Disable batch fixing

  public override async Task RegisterCodeFixesAsync(CodeFixContext context)
  {
    var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
    if (root is null)
    {
      return;
    }

    var diagnostic = context.Diagnostics.First();

    // Extract property name from diagnostic properties
    if (!diagnostic.Properties.TryGetValue("propertyName", out var propertyName) || string.IsNullOrEmpty(propertyName))
    {
      return;
    }

    var diagnosticSpan = diagnostic.Location.SourceSpan;
    var node = root.FindNode(diagnosticSpan);

    // Find the containing class
    var classDeclaration = node.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
    if (classDeclaration is null)
    {
      return;
    }

    // Create parent action with nested options
    var title = $"Fix {propertyName} member";

    var nestedActions = ImmutableArray.Create(
      CodeAction.Create(
        title: "Add MapMember",
        createChangedDocument: c => AddMapMemberAsync(context.Document, classDeclaration, propertyName!, c),
        equivalenceKey: nameof(UnmappedPropertyCodeFixProvider) + "_MapMember"),
      CodeAction.Create(
        title: "Add IgnoreMember",
        createChangedDocument: c => AddIgnoreMemberAsync(context.Document, classDeclaration, propertyName!, c),
        equivalenceKey: nameof(UnmappedPropertyCodeFixProvider) + "_IgnoreMember")
    );

    var codeAction = CodeAction.Create(
      title: title,
      nestedActions: nestedActions,
      isInlinable: false);

    context.RegisterCodeFix(codeAction, diagnostic);
  }

  private async Task<Document> AddMapMemberAsync(
    Document document,
    ClassDeclarationSyntax classDeclaration,
    string propertyName,
    CancellationToken cancellationToken)
  {
    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
    if (root is null)
    {
      return document;
    }

    var (newClass, _) = GetOrCreateConstructor(classDeclaration);
    if (newClass is null)
    {
      return document;
    }

    var constructor = SyntaxHelpers.FindConstructor(newClass);
    if (constructor?.Body is null)
    {
      return document;
    }

    // Create MapMember statement with TODO placeholder
    var mapMemberStatement = CreateMapMemberStatement(propertyName);

    // Add statement to constructor body
    var newBody = constructor.Body.AddStatements(mapMemberStatement);
    var newConstructor = constructor.WithBody(newBody);
    var finalClass = newClass.ReplaceNode(constructor, newConstructor);

    var newRoot = root.ReplaceNode(classDeclaration, finalClass);
    return document.WithSyntaxRoot(newRoot);
  }

  private async Task<Document> AddIgnoreMemberAsync(
    Document document,
    ClassDeclarationSyntax classDeclaration,
    string propertyName,
    CancellationToken cancellationToken)
  {
    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
    if (root is null)
    {
      return document;
    }

    var (newClass, _) = GetOrCreateConstructor(classDeclaration);
    if (newClass is null)
    {
      return document;
    }

    var constructor = SyntaxHelpers.FindConstructor(newClass);
    if (constructor?.Body is null)
    {
      return document;
    }

    // Create IgnoreMember statement
    var ignoreMemberStatement = CreateIgnoreMemberStatement(propertyName);

    // Add statement to constructor body
    var newBody = constructor.Body.AddStatements(ignoreMemberStatement);
    var newConstructor = constructor.WithBody(newBody);
    var finalClass = newClass.ReplaceNode(constructor, newConstructor);

    var newRoot = root.ReplaceNode(classDeclaration, finalClass);
    return document.WithSyntaxRoot(newRoot);
  }

  private (ClassDeclarationSyntax? newClass, bool constructorCreated) GetOrCreateConstructor(ClassDeclarationSyntax classDeclaration)
  {
    var existingConstructor = SyntaxHelpers.FindConstructor(classDeclaration);

    // If constructor exists with parameters, skip (invalid mapper per MAPPER010)
    if (existingConstructor?.ParameterList.Parameters.Count > 0)
    {
      return (null, false);
    }

    // If constructor already exists, return as-is
    if (existingConstructor is not null)
    {
      return (classDeclaration, false);
    }

    // Create new constructor
    var className = classDeclaration.Identifier.Text;
    var constructor = SyntaxFactory.ConstructorDeclaration(className)
      .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
      .WithParameterList(SyntaxFactory.ParameterList())
      .WithBody(SyntaxFactory.Block())
      .WithLeadingTrivia(SyntaxFactory.Whitespace("  ")) // Indentation
      .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));

    // Find the first method to insert constructor after it
    var firstMethod = classDeclaration.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault();

    ClassDeclarationSyntax newClass;
    if (firstMethod is not null)
    {
      var methodIndex = classDeclaration.Members.IndexOf(firstMethod);
      var members = classDeclaration.Members.Insert(methodIndex + 1, constructor);
      newClass = classDeclaration.WithMembers(members);
    }
    else
    {
      // No method found, add at the end
      newClass = classDeclaration.AddMembers(constructor);
    }

    return (newClass, true);
  }

  private StatementSyntax CreateMapMemberStatement(string propertyName)
  {
    // Create: MapMember(dest => dest.PropertyName, src => src.TODO)
    // where TODO is marked with RenameAnnotation for immediate editing

    // dest => dest.PropertyName
    var destParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("dest"));
    var destPropertyAccess = SyntaxFactory.MemberAccessExpression(
      SyntaxKind.SimpleMemberAccessExpression,
      SyntaxFactory.IdentifierName("dest"),
      SyntaxFactory.IdentifierName(propertyName));
    var destLambda = SyntaxFactory.SimpleLambdaExpression(destParameter, destPropertyAccess);

    // src => src.TODO (with RenameAnnotation on TODO)
    var srcParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("src"));
    var todoIdentifier = SyntaxFactory.IdentifierName("TODO")
      .WithAdditionalAnnotations(RenameAnnotation.Create());
    var srcPropertyAccess = SyntaxFactory.MemberAccessExpression(
      SyntaxKind.SimpleMemberAccessExpression,
      SyntaxFactory.IdentifierName("src"),
      todoIdentifier);
    var srcLambda = SyntaxFactory.SimpleLambdaExpression(srcParameter, srcPropertyAccess);

    // MapMember invocation
    var mapMemberInvocation = SyntaxFactory.InvocationExpression(
      SyntaxFactory.IdentifierName("MapMember"),
      SyntaxFactory.ArgumentList(
        SyntaxFactory.SeparatedList([SyntaxFactory.Argument(destLambda), SyntaxFactory.Argument(srcLambda)])));

    return SyntaxFactory.ExpressionStatement(mapMemberInvocation)
      .WithLeadingTrivia(SyntaxFactory.Whitespace("    ")); // Indentation for statement inside constructor
  }

  private StatementSyntax CreateIgnoreMemberStatement(string propertyName)
  {
    // Create: IgnoreMember(dest => dest.PropertyName)

    // dest => dest.PropertyName
    var destParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("dest"));
    var destPropertyAccess = SyntaxFactory.MemberAccessExpression(
      SyntaxKind.SimpleMemberAccessExpression,
      SyntaxFactory.IdentifierName("dest"),
      SyntaxFactory.IdentifierName(propertyName));
    var destLambda = SyntaxFactory.SimpleLambdaExpression(destParameter, destPropertyAccess);

    // IgnoreMember invocation
    var ignoreMemberInvocation = SyntaxFactory.InvocationExpression(
      SyntaxFactory.IdentifierName("IgnoreMember"),
      SyntaxFactory.ArgumentList(
        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(destLambda))));

    return SyntaxFactory.ExpressionStatement(ignoreMemberInvocation)
      .WithLeadingTrivia(SyntaxFactory.Whitespace("    ")); // Indentation for statement inside constructor
  }
}
