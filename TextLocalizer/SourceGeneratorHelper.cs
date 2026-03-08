using System.Text;
using Microsoft.CodeAnalysis;
using TextLocalizer.Translations;

namespace TextLocalizer;

using AllTranslationsData = Dictionary<string, Dictionary<string, string>>;

internal static partial class SourceGenerationHelper
{
    private const string TranslationProviderAttributeName = "TranslationProviderAttribute";
    private const string LocalizationTableAttributeName = "LocalizationTableAttribute";

    public static TranslationDictionary CreateIndexedLocalizedTextDictionary(Dictionary<string, LocalizedText> parsedLocalizedTexts)
    {
        var dictionary = new TranslationDictionary();
        var index = 1;

        foreach (var pair in parsedLocalizedTexts)
        {
            var (key, parsed) = (pair.Key, pair.Value);
            var indexedText = new IndexedLocalizedText(
                parsed.Text,
                index,
                parsed.IsUntranslatable
            );

            dictionary[key] = indexedText;
            index++;
        }

        return dictionary;
    }

    public static string GenerateProvider(
        StringBuilder builder,
        GeneratorSettings settings,
        ProviderData translationProvider,
        Dictionary<string, Dictionary<int, TranslationText>> dictionary)
    {
        if (!translationProvider.IsDefault)
        {
            builder.Append(NullableEnable);
        }

        builder
            .Append(UsingTypes)
            .AppendNamespace(translationProvider.Namespace)
            .Append(OpenBrace)
            .AppendProviderClassName(translationProvider.ClassName)
            .Append(Tab1 + OpenBrace)
            .Append(Tab2 + DictionaryDeclaration)
            .Append(Tab2 + OpenBrace)
            .AppendDictionaryValues(dictionary)
            .Append(Tab2 + "};\n\n")
            .AppendAccessor(translationProvider.IsDefault);

        builder
            .Append(Tab1 + CloseBrace)
            .Append(CloseBrace);

        if (!translationProvider.IsDefault)
        {
           builder.Append(NullableRestore);
        }

        var result = builder.ToString();
        builder.Clear();

        return result;
    }

    public static string GenerateTranslationTable(
        StringBuilder builder,
        GeneratorSettings settings,
        CombinedTranslations<int> translations)
    {
        if (translations is not { MainTranslation: { } mainTranslation, Table: { } translationTable })
        {
            return "// Failed to generate Translation table\n";
        }

        builder
            .Append(NullableEnable + UsingTypes)
            .AppendNamespace(translationTable.Namespace)
            .Append(OpenBrace)
            .AppendTranslationTableClassName(translationTable.ClassName)
            .Append(Tab1 + OpenBrace)
            .AppendTextTableFieldCtor(translationTable.Name, translationTable.ClassName);

        // if (!settings.GenerateXmlDocs)
        // {
            builder.Append('\n');
        // }

        foreach (var module in mainTranslation.Modules)
        {
            builder.Append(Tab3 + "public __").Append(module.Name).Append(' ').Append(module.Name).Append(" = new(outer);\n");

            builder.Append(Tab3 + "public class __").Append(module.Name)
                .Append('(').Append(translationTable.ClassName).Append(' ').Append("outer").Append(")\n");

            builder.Append(Tab3 + OpenBrace);

            foreach (var text in module.Texts)
            {
                if (settings.GenerateXmlDocs)
                {
                    AddXml(builder, text, settings, translations);
                }

                builder.Append(Tab4 + "public string ").Append(text.SourceKey)
                    .Append(" => outer.Provider[").Append(text.Key)
                    .Append("] ?? outer.DefaultProvider[").Append(text.Key).Append("]!;\n");
            }

            builder.Append(Tab3 + CloseBrace + '\n');
        }

        // foreach (var moduleKvp in mainTranslation.Modules)
        // {
        //     var moduleName = moduleKvp.Key;
        //     builder.Append(Tab3 + "public __").Append(moduleName).Append(' ').Append(moduleName)
        //         .Append(" = new(outer);\n");
        //
        //     builder.Append(Tab3 + "public class __").Append(moduleName)
        //         .Append('(').Append(translationTable.ClassName).Append(' ').Append("outer").Append(")\n");
        //     builder.Append(Tab3 + OpenBrace);
        //
        //     foreach (var textKvp in moduleKvp.Value.Texts)
        //     {
        //         var (key, value) = (textKvp.Key, textKvp.Value);
        //         builder.Append(Tab4 + "public string ").Append(key)
        //             .Append(" => outer.Provider[").Append(key).Append("];\n");
        //     }
        //
        //     builder.Append(Tab3 + CloseBrace + '\n');
        // }

        // foreach (var pair in mainTranslation)
        // {
        //     var (key, localizedText) = (pair.Key, pair.Value);
        // AppendTextProp(builder, settings, key, localizedText, translationTable, allTranslations);
        // }

        builder.AppendIndexer("Provider", "DefaultProvider");

        builder.Append(Tab2 + CloseBrace);

        builder
            .Append(Tab1 + CloseBrace)
            .Append(CloseBrace)
            .Append(NullableRestore);

        var result = builder.ToString();
        builder.Clear();

        return result;

        static void AddXml(StringBuilder builder,
            TranslationText2<int> text,
            GeneratorSettings settings,
            CombinedTranslations<int> translations)
        {
            builder.Append('\n' + Tab4 + "/// <summary>\n");

            if (text.IsTemplated || true)
            {
                // builder.Append(Tab4 + "/// Templated\n");
            }

            if (text.IsUntranslatable)
            {
                builder
                    .Append(Tab4 + "/// <i>Marked as untranslatable.</i><br/>\n")
                    .Append(Tab4 + "/// ").Append(text.Value).Append('\n');
            }
            else
            {
                builder
                    .Append(Tab4 + "///  <list type=\"table\">\n")
                    .Append(Tab4 + "///   <listheader>\n")
                    .Append(Tab4 + "///    <term>Language</term>\n")
                    .Append(Tab4 + "///    <description>Translation</description>\n")
                    .Append(Tab4 + "///   </listheader>\n");

                foreach (var translation in translations.Languages)
                {
                    if (translation.ContainsModule(text.Module) && translation[text.Module].ContainsEntry(text.Key))
                    {
                        var translatedText = translation[text.Module][text.Key];

                        builder
                            .Append(Tab4 + "///   <item>\n")
                            .Append(Tab4 + "///    <term>").Append(translation.Language).Append("</term>\n")
                            .Append(Tab4 + "///    <description>").Append(translatedText.Value).Append("</description>\n")
                            .Append(Tab4 + "///   </item>\n");
                    }
                    else
                    {
                        builder
                            .Append(Tab4 + "///   <item>\n")
                            .Append(Tab4 + "///    <term>").Append(translation.Language).Append("</term>\n")
                            .Append(Tab4 + "///    <description><i>Missing translation</i></description>\n")
                            .Append(Tab4 + "///   </item>\n");
                    }
                }

                builder
                    .Append(Tab4 + "///  </list>\n");
            }

            builder.Append(Tab4 + "/// </summary>\n");
        }
    }

    private static void AppendTextProp(
        StringBuilder builder,
        GeneratorSettings settings,
        string key,
        IndexedLocalizedText localizedText,
        TranslationTableAttributeData translationTable,
        AllTranslationsData allTranslations)
    {
        if (localizedText.IsUntranslatable)
        {
            if (settings.GenerateXmlDocs)
            {
                builder.Append('\n');
                builder.Append("            /// <summary>\n");
                builder.Append("            /// <i>Marked as untranslatable</i><br/>\n");
                builder.Append("            /// ").Append(localizedText.Text).Append('\n');
                builder.Append("            /// </summary>\n");
            }

            builder
                .Append("            public string ").Append(key)
                .Append(" => outer.").Append(translationTable.DefaultProviderAccessor)
                .Append("[").Append(localizedText.Index).Append("]!;\n");
        }
        else
        {
            if (settings.GenerateXmlDocs && allTranslations.TryGetValue(key, out var fileTranslations))
            {
                builder.Append('\n');
                builder.Append("            /// <summary>\n");
                builder.Append("            ///  <list type=\"table\">\n");
                builder.Append("            ///   <listheader>\n");
                builder.Append("            ///    <term>File</term>\n");
                builder.Append("            ///    <description>Translation</description>\n");
                builder.Append("            ///   </listheader>\n");

                foreach (var pair in fileTranslations)
                {
                    var (filename, translation) = (pair.Key, pair.Value);

                    builder.Append("            ///   <item>\n");
                    builder.Append("            ///    <term>").Append(filename).Append("</term>\n");
                    builder.Append("            ///    <description>").Append(translation).Append("</description>\n");
                    builder.Append("            ///   </item>\n");
                }

                builder.Append("            ///  </list>\n");
                builder.Append("            /// </summary>\n");
            }

            builder
                .Append("            public string ").Append(key)
                .Append(" => outer.").Append(translationTable.CurrentProviderAccessor)
                .Append("[").Append(localizedText.Index).Append("] ?? outer.")
                .Append(translationTable.DefaultProviderAccessor).Append("[").Append(localizedText.Index).Append("]!;\n");
        }
    }

    public static string GenerateIdClass(
        StringBuilder builder,
        GeneratorSettings settings,
        CombinedTranslations<int> translations)
    {
        if (translations is not { MainTranslation: { } mainTranslation, Table: { } translationTable })
        {
            return "// Failed to generate Id class\n";
        }

        builder
            .Append(NullableEnable + UsingTypes)
            .Append("namespace ").Append(translationTable.Namespace).Append("\n")
            .Append("{\n")
            .Append("    public static class ").Append("TODO").Append('\n')
            .Append("    {\n");

        if (!settings.GenerateXmlDocs)
        {
            builder.Append('\n');
        }

        foreach (var module in mainTranslation.Modules)
        {
            // builder.Append("// ").Append(module.Name).Append('\n');

            builder.Append(Tab2 + "public static class ").Append(module.Name).Append("\n")
                .Append(Tab2 + OpenBrace);

            foreach (var text in module.Texts)
            {
                builder.Append(Tab3 + "public static readonly StringResourceId ").Append(text.SourceKey)
                    .Append(" = new(").Append(text.Key).Append(");\n");
            }

            builder.Append(Tab2 + CloseBrace + '\n');
        }
        // foreach (var pair in defaultDictionary)
        // {
        //     var (key, localizedText) = (pair.Key, pair.Value);
        //     AppendIdProp(builder, settings, key, localizedText, allTranslations);
        // }

        builder
            .Append("    }\n")
            .Append("}\n\n")
            .Append("#nullable restore");

        var result = builder.ToString();
        builder.Clear();

        return result;
    }

    private static void AppendIdProp(
        StringBuilder builder,
        GeneratorSettings settings,
        string key,
        IndexedLocalizedText localizedText,
        AllTranslationsData allTranslations)
    {
        if (settings.GenerateXmlDocs)
        {
            if (localizedText.IsUntranslatable)
            {
                builder.Append('\n');
                builder.Append("        /// <summary>\n");
                builder.Append("        /// <i>Marked as untranslatable</i><br/>\n");
                builder.Append("        /// ").Append(localizedText.Text).Append('\n');
                builder.Append("        /// </summary>\n");
            }
            else if (allTranslations.TryGetValue(key, out var fileTranslations))
            {
                builder.Append('\n');
                builder.Append("        /// <summary>\n");
                builder.Append("        ///  <list type=\"table\">\n");
                builder.Append("        ///   <listheader>\n");
                builder.Append("        ///    <term>File</term>\n");
                builder.Append("        ///    <description>Translation</description>\n");
                builder.Append("        ///   </listheader>\n");

                foreach (var pair in fileTranslations)
                {
                    var (filename, translation) = (pair.Key, pair.Value);

                    builder.Append("        ///   <item>\n");
                    builder.Append("        ///    <term>").Append(filename).Append("</term>\n");
                    builder.Append("        ///    <description>").Append(translation).Append("</description>\n");
                    builder.Append("        ///   </item>\n");
                }

                builder.Append("        ///  </list>\n");
                builder.Append("        /// </summary>\n");
            }
        }

        builder.Append("        public static readonly int ").Append(key).Append(" = new(").Append(localizedText.Index).Append(");\n");
    }

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
