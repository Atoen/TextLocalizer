using System.Text;
using TextLocalizer.Types;

namespace TextLocalizer;

internal static partial class SourceGenerationHelper
{
    public const string ProviderAttribute =
        """
        namespace TextLocalizer
        {
            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class TranslationProviderAttribute : System.Attribute
            {
                public required string Language { get; set; }
            }
        }
        """;

    public const string LocalizationTableAttribute =
        """
        #nullable enable

        namespace TextLocalizer
        {
            [System.AttributeUsage(System.AttributeTargets.Class)]
            public class LocalizationTableAttribute : System.Attribute
            {
                public required string CurrentProviderAccessor { get; set; }
                
                public required string DefaultProviderAccessor { get; set; }
                
                public string TableName { get; set; } = "Table";
            }
        }

        #nullable restore
        """;

    private const string Tab1 = "    ";
    private const string Tab2 = Tab1 + Tab1;
    private const string Tab3 = Tab2 + Tab1;

    private const string OpenBrace = "{\n";
    private const string CloseBrace = "}\n";

    private const string NullableEnable = "#nullable enable\n\n";
    private const string NullableRestore = "\n#nullable restore\n";
    private const string UsingTypes = "using TextLocalizer.Types;\n\n";

    private const string DictionaryDeclaration = "private readonly Dictionary<StringResourceId, string> _dictionary = new()\n";

    extension(StringBuilder builder)
    {
        public StringBuilder AppendNamespace(string @namespace)
        {
            return builder.Append("namespace ").Append(@namespace).Append('\n');
        }

        public StringBuilder AppendProviderClassName(string className)
        {
            return builder.Append(Tab1 + "public partial class ")
                .Append(className)
                .Append(" : " + nameof(ILocalizedTextProvider) + "\n");
        }

        public StringBuilder AppendTranslationTableClassName(string className)
        {
            return builder.Append(Tab1 + "public partial class ")
                .Append(className)
                .Append('\n');
        }

        public StringBuilder AppendDictionaryValues(TranslationDictionary2 dictionary)
        {
            foreach (var kvp in dictionary)
            {
                // builder.Append("public static class ").Append(kvp.Key).Append(" {\n");

                builder.Append(Tab3 + "// ").Append(kvp.Key).Append("\n");

                foreach (var kvp2 in kvp.Value)
                {
                    builder.Append(Tab3 + "{ \"").Append(kvp2.Key).Append("\", \"").Append(kvp2.Value.Value);
                    builder.Append("\" },\n");
                }

                // builder.Append("}\n");
            }
            //
            // foreach (var translation in dictionary.Values)
            // {
            //     // builder.Append(Tab3 + "{ ").Append(translation.Index).Append(", \"").Append(translation.Text);
            //     // builder.Append(translation.IsUntranslatable ? "\" }, // Untranslatable \n" : "\" },\n");
            // }

            return builder;
        }

        public StringBuilder AppendAccessor(bool isDefaultProvider)
        {
            const string @default = "public string this[StringResourceId key] => _dictionary[key];\n";
            const string nonDefault = "public string? this[StringResourceId key] => _dictionary.GetValueOrDefault(key);\n";

            return isDefaultProvider ? builder.Append(Tab2 + @default) : builder.Append(Tab2 + nonDefault);
        }

        public StringBuilder AppendTextTableFieldCtor(string tableName, string className)
        {
            builder.Append(Tab2 + "private TextTable? _table;\n");

            builder
                .Append(Tab2 + "public TextTable ")
                .Append(tableName)
                .Append(" => _table ??= new TextTable(this);\n\n");

            builder
                .Append(Tab2 + "public class TextTable(")
                .Append(className)
                .Append(" outer)\n");

            builder.Append(Tab2 + '{');

            return builder;
        }

        public StringBuilder AppendIndexer(string currentProviderName, string defaultProviderName)
        {
            return builder
                .Append('\n' + Tab3 + "public string this[StringResourceId id]")
                .Append(" => outer.")
                .Append(currentProviderName)
                .Append("[id] ?? outer.")
                .Append(defaultProviderName)
                .Append("[id]!;\n");
        }
    }
}
