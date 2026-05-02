using System.Text;
using TextLocalizer.Parsing;
using TextLocalizer.Translations;
using static TextLocalizer.Pipeline.Builders.Snippets;

namespace TextLocalizer.Pipeline.Builders;

public static class TextProviderBuilder
{
    private const string Indexer = "public override object? this[int key] => _texts[key];\n";
    private const string ArrayDeclaration = "private readonly object?[] _texts =\n";

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
            .Append(Tab2 + "public override string? Get(int key) => _texts[key] as string;\n")
            .Append(Tab2 + "public override string? Get(int key, int count)\n" + Tab2 + "{\n")
            .Append(Tab3 + "var category = GetPluralCategory(count);\n")
            .Append(Tab3 + "return (_texts[key] as PluralString)?.Get(category);\n")
            .Append(Tab2 + "}\n")
            .AppendIsDefaultProperty(translation.IsDefault)
            .Append('\n' + Tab2 + ArrayDeclaration)
            .Append(Tab2 + '{');

        var array = new object?[highestId + 1];

        foreach (var text in translation.Modules.SelectMany(x => x.Texts))
        {
            array[text.Key] = text.SupportsPluralization ? text.Plurals : text.Value;
            // array[text.Key] = "oro";
        }

        foreach (var text in array)
        {
            builder.Append('\n' + Tab3);

            if (text is null)
            {
                builder.Append("null,");
            }
            else if (text is string)
            {
                builder.Append("@\"").Append(text).Append("\",");
            }
            else if (text is Dictionary<PluralCategory, string> dictionary)
            {
                builder.Append("new PluralString(")
                    .Append(ToLiteral(dictionary.GetValueOrDefault(PluralCategory.Other))).Append(", ")
                    .Append(ToLiteral(dictionary.GetValueOrDefault(PluralCategory.Zero))).Append(", ")
                    .Append(ToLiteral(dictionary.GetValueOrDefault(PluralCategory.One))).Append(", ")
                    .Append(ToLiteral(dictionary.GetValueOrDefault(PluralCategory.Two))).Append(", ")
                    .Append(ToLiteral(dictionary.GetValueOrDefault(PluralCategory.Few))).Append(", ")
                    .Append(ToLiteral(dictionary.GetValueOrDefault(PluralCategory.Many)))
                    .Append("),");
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
    
    private static string ToLiteral(string? value)
    {
        if (value == null)
            return "null";

        return "@\"" + value.Replace("\"", "\"\"") + "\"";
    }

    extension(StringBuilder builder)
    {
        private StringBuilder AppendIsDefaultProperty(bool isDefault)
        {
            var value = isDefault ? "true" : "false";
            
            return builder
                .Append('\n' + Tab2 + "public override bool IsDefault => ")
                .Append(value).Append(";\n");
        }
    }

    extension<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
    {
        private TValue? GetValueOrDefault(TKey key)
        {
            dictionary.TryGetValue(key, out var value);
            return value ?? default;
        }
        
        private TValue GetOr(TKey key, TValue @default)
        {
            return dictionary.TryGetValue(key, out var value) ? value : @default;
        }
    }
}
