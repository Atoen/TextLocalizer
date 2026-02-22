using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using TextLocalizer.Translations;

namespace TextLocalizer;

internal static class DiagnosticsHelper
{
    extension(SourceProductionContext context)
    {
        
        public void ReportMissingModuleDiagnostic(string filename, string language, string moduleName)
        {
            var location = Location.Create(filename, new TextSpan(), new LinePositionSpan());

            var diagnostic = Diagnostic.Create(
                MissingModuleDescriptor,
                location,
                moduleName, language);

            context.ReportDiagnostic(diagnostic);
        }
        
        public void ReportMissingKeyDiagnostic(string filename, string module, string key)
        {
            var location = Location.Create(filename, new TextSpan(), new LinePositionSpan());

            var diagnostic = Diagnostic.Create(
                MissingKeyDescriptor,
                location,
                key, filename);

            context.ReportDiagnostic(diagnostic);
        }

        public void ReportUntranslatableKeyDiagnostic(string filename, string module, string key, int lineNumber)
        {
            var linePosition = new LinePosition(lineNumber, 0);
            var location = Location.Create(filename, new TextSpan(), new LinePositionSpan(linePosition, linePosition));

            var diagnostic = Diagnostic.Create(
                UntranslatableKeyDescriptor,
                location,
                module, key);

            context.ReportDiagnostic(diagnostic);
        }

        public void ReportExtraKeyDiagnostic(TranslationsProviderData providerData, string key, int lineNumber)
        {
            var linePosition = new LinePosition(lineNumber, 0);
            var location = Location.Create(providerData.File.Path, new TextSpan(), new LinePositionSpan(linePosition, linePosition));

            var diagnostic = Diagnostic.Create(
                ExtraKeyDescriptor,
                location,
                providerData.File.Name, key);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static readonly DiagnosticDescriptor MissingKeyDescriptor = new(
        id: "TL001",
        title: "Missing item in dictionary",
        messageFormat: "The key '{0}' is missing its translation in {1} file",
        category: "DictionaryComparison",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor ExtraKeyDescriptor = new(
        id: "TL002",
        title: "Extra item in dictionary",
        messageFormat: "File {0} contains key '{1}', which is not present in the main translations file",
        category: "DictionaryComparison",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor UntranslatableKeyDescriptor = new(
        id: "TL003",
        title: "Untranslatable key is localized",
        messageFormat: "File {0} contains key '{1}', which is marked as untranslatable",
        category: "DictionaryComparison",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
    
    private static readonly DiagnosticDescriptor MissingModuleDescriptor = new(
        id: "TL004",
        title: "Missing translation module in non-default language",
        messageFormat: "The module '{0}' is missing in {1} language directory",
        category: "TranslationModules",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
}
