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
            .AppendTextTableFieldCtor(translationTable.Name, translationTable.ClassName)
            .Append('\n');

        foreach (var module in mainTranslation.Modules)
        {
            builder.Append(Tab3 + "public __").Append(module.Name).Append(' ').Append(module.Name).Append(" = new(outer);\n");

            builder.Append(Tab3 + "public class __").Append(module.Name)
                .Append('(').Append(translationTable.ClassName).Append(' ').Append("outer").Append(")\n");

            builder.Append(Tab3 + '{');

            foreach (var text in module.Texts)
            {
                if (settings.GenerateXmlDocs)
                {
                    builder.AddXmlDocs(text, translations, 4);
                }

                if (text.IsTemplated)
                {
                    builder
                        .Append(Tab4 + "public string ").Append(text.SourceKey).Append("(params ReadOnlySpan<object?> args)\n" + Tab4 + "{\n")
                        .Append(Tab5 + "var value = ")
                        .AppendProviderAccess(translationTable, text).Append('\n')
                        .Append(Tab5 + "return string.Format(value, args);\n")
                        .Append(Tab4 + "}\n");
                }
                else
                {
                    builder
                        .Append(Tab4 + "public string ").Append(text.SourceKey).Append(" => ")
                        .AppendProviderAccess(translationTable, text).Append('\n');
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

    extension(StringBuilder builder)
    {
        public StringBuilder AppendProviderAccess(TranslationTable table, TranslationText<int> text)
        {
            if (text.IsUntranslatable)
            {
                builder
                    .Append("outer.").Append(table.DefaultProvider)
                    .Append("[").Append(text.Key).Append("]!;");
            }
            else
            {
                builder
                    .Append("outer.").Append(table.Provider)
                    .Append("[").Append(text.Key).Append("] ?? outer.").Append(table.DefaultProvider)
                    .Append("[").Append(text.Key).Append("]!;");
            }
            
            return builder;
        }
        
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
                .Append(Tab2 + "private __TextTable? __table;\n")
                .Append(Tab2 + "public __TextTable ")
                .Append(tableName)
                .Append(" => __table ??= new __TextTable(this);\n\n");

            builder
                .Append(Tab2 + "public class __TextTable(")
                .Append(className)
                .Append(" outer)\n");

            builder.Append(Tab2 + '{');

            return builder;
        }
    }
}
