using System;
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
  private const string PropertyNameDiagnosticProperty = "propertyName";
  private const string DestinationParameterName = "dest";
  private const string SourceParameterName = "src";
  private const string TodoPlaceholder = "TODO";
  private const string MapMemberMethodName = "MapMember";
  private const string IgnoreMemberMethodName = "IgnoreMember";
  private const string ConstructorIndentation = "  ";
  private const string StatementIndentation = "    ";

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
    if (!diagnostic.Properties.TryGetValue(PropertyNameDiagnosticProperty, out var propertyName) || string.IsNullOrEmpty(propertyName))
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

  private Task<Document> AddMapMemberAsync(
    Document document,
    ClassDeclarationSyntax classDeclaration,
    string propertyName,
    CancellationToken cancellationToken)
  {
    return AddConfigurationMethodAsync(document, classDeclaration, propertyName, CreateMapMemberStatement, cancellationToken);
  }

  private Task<Document> AddIgnoreMemberAsync(
    Document document,
    ClassDeclarationSyntax classDeclaration,
    string propertyName,
    CancellationToken cancellationToken)
  {
    return AddConfigurationMethodAsync(document, classDeclaration, propertyName, CreateIgnoreMemberStatement, cancellationToken);
  }

  private async Task<Document> AddConfigurationMethodAsync(
    Document document,
    ClassDeclarationSyntax classDeclaration,
    string propertyName,
    Func<string, StatementSyntax> createStatement,
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

    var statement = createStatement(propertyName);
    var newBody = constructor.Body.AddStatements(statement);
    var newConstructor = constructor.WithBody(newBody);
    var finalClass = newClass.ReplaceNode(constructor, newConstructor);

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
    var constructor = SyntaxFactory.ConstructorDeclaration(classDeclaration.Identifier.Text)
      .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
      .WithParameterList(SyntaxFactory.ParameterList())
      .WithBody(SyntaxFactory.Block())
      .WithLeadingTrivia(SyntaxFactory.Whitespace(ConstructorIndentation))
      .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));

    // Insert constructor after first method, or at the end if no methods exist
    var firstMethod = classDeclaration.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault();
    return firstMethod is not null
      ? classDeclaration.WithMembers(classDeclaration.Members.Insert(classDeclaration.Members.IndexOf(firstMethod) + 1, constructor))
      : classDeclaration.AddMembers(constructor);
  }

  private StatementSyntax CreateMapMemberStatement(string propertyName)
  {
    // Create: MapMember(dest => dest.PropertyName, src => src.TODO)
    var template = $"{MapMemberMethodName}({DestinationParameterName} => {DestinationParameterName}.{propertyName}, {SourceParameterName} => {SourceParameterName}.{TodoPlaceholder});";
    var statement = SyntaxFactory.ParseStatement(template);

    // Find and annotate the TODO identifier for rename functionality
    var todoIdentifier = statement.DescendantNodes()
      .OfType<IdentifierNameSyntax>()
      .FirstOrDefault(id => id.Identifier.Text == TodoPlaceholder);

    if (todoIdentifier is not null)
    {
      var annotatedIdentifier = todoIdentifier.WithAdditionalAnnotations(RenameAnnotation.Create());
      statement = statement.ReplaceNode(todoIdentifier, annotatedIdentifier);
    }

    return statement.WithLeadingTrivia(SyntaxFactory.Whitespace(StatementIndentation));
  }

  private StatementSyntax CreateIgnoreMemberStatement(string propertyName)
  {
    // Create: IgnoreMember(dest => dest.PropertyName)
    var template = $"{IgnoreMemberMethodName}({DestinationParameterName} => {DestinationParameterName}.{propertyName});";
    var statement = SyntaxFactory.ParseStatement(template);

    return statement.WithLeadingTrivia(SyntaxFactory.Whitespace(StatementIndentation));
  }
}
