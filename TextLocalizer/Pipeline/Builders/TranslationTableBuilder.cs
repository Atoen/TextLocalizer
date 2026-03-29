using System.Security;
using System.Text;
using TextLocalizer.Translations;
using static TextLocalizer.Pipeline.Builders.Snippets;

namespace TextLocalizer.Pipeline.Builders;

public static class TranslationTableBuilder
{
    public static string BuildTranslationTable(
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

                if (text.IsTemplated)
                {
                    builder
                        .Append(Tab4 + "public string ").Append(text.SourceKey).Append("(params ReadOnlySpan<object?> args)\n" + Tab4 + "{\n")
                        .Append(Tab5 + "var value = outer._provider[").Append(text.Key).Append("] ?? outer._defaultProvider[").Append(text.Key).Append("]!;\n")
                        .Append(Tab5 + "return string.Format(value, args);\n")
                        .Append(Tab4 + "}\n");

                }
                else
                {
                    builder
                        .Append(Tab4 + "public string ").Append(text.SourceKey).Append(" => outer.").Append(translationTable.Provider)
                        .Append("[").Append(text.Key).Append("] ?? outer.").Append(translationTable.DefaultProvider)
                        .Append("[").Append(text.Key).Append("]!;\n");
                }
            }

            builder.Append(Tab3 + CloseBrace + '\n');
        }

        builder.AppendIndexer(translationTable.Provider, translationTable.DefaultProvider);

        builder.Append(Tab2 + CloseBrace);

        builder
            .Append(Tab1 + CloseBrace)
            .Append(CloseBrace)
            .Append(NullableRestore);

        var result = builder.ToString();
        builder.Clear();

        return result;
    }

    private static void AddXml(StringBuilder builder, TranslationText<int> text, GeneratorSettings settings, CombinedTranslations<int> translations)
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
                        .Append(Tab4 + "///    <description>")
                        .Append(
                            SecurityElement.Escape(translatedText.Value)
                                .Replace("\r\n", "<br/>")
                                .Replace("\n", "<br/>")
                                .Replace("\r", "<br/>")
                        )
                        .Append("</description>\n")
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

    extension(StringBuilder builder)
    {
        private StringBuilder AppendIndexer(string currentProviderName, string defaultProviderName)
        {
            return builder
                .Append(Tab3 + "public string this[StringResourceId id]")
                .Append(" => outer.")
                .Append(currentProviderName)
                .Append("[id] ?? outer.")
                .Append(defaultProviderName)
                .Append("[id]!;\n");
        }

        private StringBuilder AppendTextTableFieldCtor(string tableName, string className)
        {
            builder
                .Append(Tab2 + "private __TextTable? _table;\n")
                .Append(Tab2 + "public __TextTable ")
                .Append(tableName)
                .Append(" => _table ??= new __TextTable(this);\n\n");

            builder
                .Append(Tab2 + "public class __TextTable(")
                .Append(className)
                .Append(" outer)\n");

            builder.Append(Tab2 + '{');

            return builder;
        }
    }
}
