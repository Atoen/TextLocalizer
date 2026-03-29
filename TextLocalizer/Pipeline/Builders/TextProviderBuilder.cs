using System.Text;
using TextLocalizer.Translations;
using static TextLocalizer.Pipeline.Builders.Snippets;

namespace TextLocalizer.Pipeline.Builders;

public static class TextProviderBuilder
{
    private const string Indexer = "public string? this[int key] => _texts[key];\n";
    private const string ArrayDeclaration = "private readonly string?[] _texts =\n";

    public static string BuildProvider(
        StringBuilder builder,
        int highestId,
        Translation<int> translation)
    {
        var provider = translation.Provider;

        builder
            .Append(NullableEnable)
            .Append(UsingTypes)
            .AppendNamespace(provider.Namespace)
            .Append(OpenBrace)
            .AppendProviderClassName(provider.ClassName)
            .Append(Tab1 + OpenBrace)
            .Append(Tab2 + Indexer)
            .Append('\n' + Tab2 + ArrayDeclaration)
            .Append(Tab2 + '{');

        var array = new string?[highestId + 1];

        foreach (var text in translation.Modules.SelectMany(x => x.Texts))
        {
            array[text.Key] = text.Value;
        }

        foreach (var text in array)
        {
            builder.Append('\n' + Tab3);

            if (text is null)
            {
                builder.Append("null,");
            }
            else
            {
                builder.Append("@\"\"\"").Append(text).Append("\"\"\",");
            }
        }

        builder.Append('\n' + Tab2 + "};\n");

        builder
            .Append(Tab1 + CloseBrace)
            .Append(CloseBrace);

        builder.Append(NullableRestore);


        var result = builder.ToString();
        builder.Clear();

        return result;
    }
}
