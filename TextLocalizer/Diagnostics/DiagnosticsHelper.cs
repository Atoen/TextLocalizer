using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace TextLocalizer.Diagnostics;

internal static class DiagnosticsHelper
{
    extension(SourceProductionContext context)
    {
        public void ReportError(TranslationError error)
        {
            var descriptor = GetDescriptor(error.Type);

            var location = CreateLocation(
                error.Module,
                error.Line
            );

            var diagnostic = Diagnostic.Create(
                descriptor,
                location,
                GetArguments(error)
            );

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static Location CreateLocation(string filename, int lineNumber)
    {
        if (lineNumber <= 0)
        {
            return Location.Create(filename, new TextSpan(), new LinePositionSpan());
        }

        var pos = new LinePosition(lineNumber, 0);
        return Location.Create(filename, new TextSpan(), new LinePositionSpan(pos, pos));
    }

    private static object[] GetArguments(TranslationError error)
    {
        return error.Type switch
        {
            TranslationErrorType.ModuleMissing => [error.Language, error.Module],
            _ => [error.Language, error.Key, error.Module]
        };
    }

    private static DiagnosticDescriptor GetDescriptor(TranslationErrorType type)
    {
        return type switch
        {
            TranslationErrorType.KeyMissing => MissingKeyDescriptor,
            TranslationErrorType.InvalidKey => InvalidKeyDescriptor,
            TranslationErrorType.UnexpectedKey => ExtraKeyDescriptor,
            TranslationErrorType.TranslatedUntranslatable => UntranslatableKeyDescriptor,
            TranslationErrorType.ModuleMissing => MissingModuleDescriptor,
            TranslationErrorType.KeyConflictsWithModule => KeyModuleConflictDescriptor,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private static readonly DiagnosticDescriptor MissingKeyDescriptor = new(
        "TL001",
        "Missing translation key",
        "Language '{0}' is missing translation for key '{1}' in module '{2}'",
        "Localization",
        DiagnosticSeverity.Warning,
        true
    );

    private static readonly DiagnosticDescriptor ExtraKeyDescriptor = new(
        "TL002",
        "Unexpected translation key",
        "Language '{0}' contains key '{1}' in module '{2}', but it does not exist in the default language",
        "Localization",
        DiagnosticSeverity.Warning,
        true
    );

    private static readonly DiagnosticDescriptor UntranslatableKeyDescriptor = new(
        "TL003",
        "Untranslatable key was translated",
        "Language '{0}' contains a translation for key '{1}' in module '{2}', but this key is marked as untranslatable",
        "Localization",
        DiagnosticSeverity.Warning,
        true
    );

    private static readonly DiagnosticDescriptor MissingModuleDescriptor = new(
        "TL004",
        "Missing translation module",
        "Language '{0}' is missing module '{1}'",
        "Localization",
        DiagnosticSeverity.Warning,
        true
    );

    private static readonly DiagnosticDescriptor InvalidKeyDescriptor = new(
        "TL005",
        "Invalid translation key",
        "Language '{0}' has invalid key '{1}' in module '{2}'",
        "Localization",
        DiagnosticSeverity.Warning,
        true
    );

    private static readonly DiagnosticDescriptor KeyModuleConflictDescriptor = new(
        "TL006",
        "Key conflicts with module name",
        "Language '{0}' has key '{1}' that conflicts with module '{2}'",
        "Localization",
        DiagnosticSeverity.Error,
        true
    );
}