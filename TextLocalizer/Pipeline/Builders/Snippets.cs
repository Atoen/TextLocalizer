using System.Security;
using System.Text;
using TextLocalizer.Translations;
using TextLocalizer.Types;

namespace TextLocalizer.Pipeline.Builders;

public static class Snippets
{
    public const string Tab1 = "    ";
    public const string Tab2 = Tab1 + Tab1;
    public const string Tab3 = Tab2 + Tab1;
    public const string Tab4 = Tab3 + Tab1;
    public const string Tab5 = Tab4 + Tab1;

    public const string OpenBrace = "{\n";
    public const string CloseBrace = "}\n";

    public const string NullableEnable = "#nullable enable\n\n";
    public const string NullableRestore = "\n#nullable restore\n";
    public const string UsingTypes = "using System;\n" +
                                     "using TextLocalizer.Types;\n\n";

    extension(StringBuilder builder)
    {
        public StringBuilder AppendNamespace(string @namespace)
        {
            return builder
                .Append("namespace ")
                .Append(@namespace)
                .Append('\n');
        }

        public StringBuilder AppendProviderClassName(string className)
        {
            return builder
                .Append(Tab1 + "public partial class ")
                .Append(className)
                .Append(" : " + nameof(ILocalizedTextProvider) + "\n");
        }

        public StringBuilder AppendTranslationTableClassName(string className)
        {
            return builder
                .Append(Tab1 + "public partial class ")
                .Append(className)
                .Append('\n');
        }
        
        public StringBuilder AddXmlDocs(TranslationText<int> text, CombinedTranslations<int> translations, int indentLevel = 3)
        {
            var indent = indentLevel == 3 ? Tab3 : Tab4;
            
            builder.Append('\n' + indent + "/// <summary>\n");

            if (text.IsTemplated || true)
            {
                // builder.Append(indent + "/// Templated\n");
            }

            if (text.IsUntranslatable)
            {
                builder
                    .Append(indent + "/// <i>Marked as untranslatable.</i><br/>\n")
                    .Append(indent + "/// ").Append(text.Value).Append('\n');
            }
            else
            {
                builder
                    .Append(indent + "///  <list type=\"table\">\n")
                    .Append(indent + "///   <listheader>\n")
                    .Append(indent + "///    <term>Language</term>\n")
                    .Append(indent + "///    <description>Translation</description>\n")
                    .Append(indent + "///   </listheader>\n");

                foreach (var translation in translations.Languages)
                {
                    if (translation.ContainsModule(text.Module) && translation[text.Module].ContainsEntry(text.Key))
                    {
                        var translatedText = translation[text.Module][text.Key];

                        builder
                            .Append(indent + "///   <item>\n")
                            .Append(indent + "///    <term>").Append(translation.Language).Append("</term>\n")
                            .Append(indent + "///    <description>")
                            .Append(
                                SecurityElement.Escape(translatedText.Value)
                                    .Replace("\r\n", "<br/>")
                                    .Replace("\n", "<br/>")
                                    .Replace("\r", "<br/>")
                            )
                            .Append("</description>\n")
                            .Append(indent + "///   </item>\n");
                    }
                    else
                    {
                        builder
                            .Append(indent + "///   <item>\n")
                            .Append(indent + "///    <term>").Append(translation.Language).Append("</term>\n")
                            .Append(indent + "///    <description><i>Missing translation</i></description>\n")
                            .Append(indent + "///   </item>\n");
                    }
                }

                builder
                    .Append(indent + "///  </list>\n");
            }

            builder.Append(indent + "/// </summary>\n");
            
            return builder;
        }
    }
}
