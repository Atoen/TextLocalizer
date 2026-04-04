using System.Text;
using TextLocalizer.Translations;
using static TextLocalizer.Pipeline.Builders.Snippets;

namespace TextLocalizer.Pipeline.Builders;

public static class IdClassBuilder
{
    public static string BuildIdClass(
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
            .Append("    public static class ").Append(settings.IdClassName).Append('\n')
            .Append("    {\n");

        if (!settings.GenerateXmlDocs)
        {
            builder.Append('\n');
        }

        foreach (var module in mainTranslation.Modules)
        {
            builder.Append(Tab2 + "public static class ").Append(module.Name).Append("\n")
                .Append(Tab2 + OpenBrace);

            foreach (var text in module.Texts)
            {
                if (settings.GenerateXmlDocs)
                {
                    builder.AddXmlDocs(text, translations);
                }
                
                builder.Append(Tab3 + "public static readonly StringResourceId ").Append(text.SourceKey)
                    .Append(" = new(").Append(text.Key).Append(");\n");
            }

            builder.Append(Tab2 + CloseBrace + '\n');
        }

        builder
            .Append("    }\n")
            .Append("}\n\n")
            .Append("#nullable restore");

        var result = builder.ToString();
        builder.Clear();

        return result;
    }
}
