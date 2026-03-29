using Microsoft.CodeAnalysis;

namespace TextLocalizer.Pipeline;

internal static class SourceGenerationHelper
{
    private const string TranslationProviderAttributeName = "TranslationProviderAttribute";
    private const string LocalizationTableAttributeName = "LocalizationTableAttribute";

    public static TranslationProviderAttributeData? CreateTranslationProviderInfo(INamedTypeSymbol classSymbol)
    {
        var attributeData = GetAttributeData(classSymbol, TranslationProviderAttributeName);
        if (attributeData is null) return null;

        var @namespace = classSymbol.ContainingNamespace.ToString();
        var className = classSymbol.Name;

        return new TranslationProviderAttributeData(@namespace, className, attributeData);
    }

    public static TranslationTableAttributeData? CreateLocalizationTableData(INamedTypeSymbol classSymbol)
    {
        var attributeData = GetAttributeData(classSymbol, LocalizationTableAttributeName);
        if (attributeData is null) return null;

        var @namespace = classSymbol.ContainingNamespace.ToString();
        var className = classSymbol.Name;

        var data = new TranslationTableAttributeData(@namespace, className, attributeData);

        return data.IsValid() ? data : null;
    }

    private static AttributeData? GetAttributeData(INamedTypeSymbol classSymbol, string attributeName)
    {
        foreach (var attribute in classSymbol.GetAttributes())
        {
            if (attribute.AttributeClass?.Name == attributeName)
            {
                return attribute;
            }
        }

        return null;
    }
}
