using System.Text;
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
    }
}
