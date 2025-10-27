using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FastTreeDataGrid.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FastTreeDataGridColumnAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FTDG0001";

    private static readonly LocalizableString Title = "FastTreeDataGridColumn requires a value binding";
    private static readonly LocalizableString MessageFormat = "{0} should specify ValueKey or provide a CellTemplate/WidgetFactory to render cell content";
    private static readonly LocalizableString Description = "FastTreeDataGrid columns rely on a ValueKey, CellTemplate, CellTemplateSelector, or WidgetFactory to materialize cell content. Missing configuration results in empty cells at runtime.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ObjectCreationExpressionSyntax creation)
        {
            return;
        }

        var typeSymbol = context.SemanticModel.GetSymbolInfo(creation.Type, context.CancellationToken).Symbol as ITypeSymbol;
        if (typeSymbol is null || !IsColumnType(typeSymbol))
        {
            return;
        }

        var hasValueKey = false;
        var hasTemplate = false;
        var hasFactory = false;

        if (creation.Initializer is { } initializer)
        {
            foreach (var expression in initializer.Expressions)
            {
                if (expression is AssignmentExpressionSyntax assignment)
                {
                    var propertyName = ExtractPropertyName(assignment.Left, context.SemanticModel, context.CancellationToken);
                    if (string.IsNullOrEmpty(propertyName))
                    {
                        continue;
                    }

                    switch (propertyName)
                    {
                        case "ValueKey":
                            hasValueKey = true;
                            break;
                        case "CellTemplate":
                        case "CellTemplateSelector":
                            hasTemplate = true;
                            break;
                        case "WidgetFactory":
                            hasFactory = true;
                            break;
                    }
                }
            }
        }

        if (!hasValueKey && !hasTemplate && !hasFactory)
        {
            var diagnostic = Diagnostic.Create(Rule, creation.Type.GetLocation(), typeSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static string? ExtractPropertyName(ExpressionSyntax expression, SemanticModel model, System.Threading.CancellationToken cancellationToken)
    {
        return expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            _ => null,
        };
    }

    private static bool IsColumnType(ITypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::FastTreeDataGrid.Control.Models.FastTreeDataGridColumn")
            {
                return true;
            }
        }

        return false;
    }
}
