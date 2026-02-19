using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TextLocalizer;

internal record GeneratorSettings
{
    public static class Defaults
    {
        public const string TranslationsDir = "Translations";
        public const bool EnableLogging = false;
        public const string LanguageName = "english";
        public const bool StrictMode = false;
        public const bool GenerateXmlDocs = true;
        public const bool GenerateIdClass = true;
        public const string IdClassName = "R";
    }

    public static class PropertyNames
    {
        private const string BuildPropertyPrefix = "build_property.TextLocalizer";

        internal const string TranslationsDir = BuildPropertyPrefix + nameof(TranslationsDir);
        internal const string EnableLogging   = BuildPropertyPrefix + nameof(EnableLogging);
        internal const string DefaultLanguage = BuildPropertyPrefix + nameof(DefaultLanguage);
        internal const string StrictMode      = BuildPropertyPrefix + nameof(StrictMode);
        internal const string GenerateXmlDocs = BuildPropertyPrefix + nameof(GenerateXmlDocs);
        internal const string GenerateIdClass = BuildPropertyPrefix + nameof(GenerateIdClass);
        internal const string IdClassName     = BuildPropertyPrefix + nameof(IdClassName);
    }

    public static bool IsSupportedFileType(AdditionalText additionalText)
    {
        var path = additionalText.Path;

        return path.EndsWith(".json") || path.EndsWith(".yml") || path.EndsWith(".yaml") || path.EndsWith(".xml");
    }

    public GeneratorSettings(
        string translationsDir,
        bool enableLogging,
        string defaultLanguage,
        bool strictMode,
        bool generateXmlDocs,
        bool generateIdClass,
        string idClassName)
    {
        TranslationsDir = translationsDir;
        EnableLogging = enableLogging;
        DefaultLanguage = defaultLanguage;
        StrictMode = strictMode;
        GenerateXmlDocs = generateXmlDocs;
        GenerateIdClass = generateIdClass;
        IdClassName = idClassName;
    }

    public string TranslationsDir { get; }
    public bool EnableLogging { get; }
    public string DefaultLanguage { get; }
    public bool StrictMode { get; }
    public bool GenerateXmlDocs { get; }
    public bool GenerateIdClass { get; }
    public string IdClassName { get; }
}

internal static class GeneratorSettingsHelper
{
    extension(GeneratorSettings)
    {
        public static GeneratorSettings ReadFromConfig(AnalyzerConfigOptions options)
        {
            var translationsDir = options.GetValueOrDefault(
                GeneratorSettings.PropertyNames.TranslationsDir,
                GeneratorSettings.Defaults.TranslationsDir);
            
            var enableLogging = options.GetValueOrDefault(
                GeneratorSettings.PropertyNames.EnableLogging,
                GeneratorSettings.Defaults.EnableLogging);

            var defaultLanguage = options.GetValueOrDefault(
                GeneratorSettings.PropertyNames.DefaultLanguage,
                GeneratorSettings.Defaults.LanguageName);

            var strictMode = options.GetValueOrDefault(
                GeneratorSettings.PropertyNames.StrictMode,
                GeneratorSettings.Defaults.StrictMode);

            var generateXmlDocs = options.GetValueOrDefault(
                GeneratorSettings.PropertyNames.GenerateXmlDocs,
                GeneratorSettings.Defaults.GenerateXmlDocs);

            var generateIdClass = options.GetValueOrDefault(
                GeneratorSettings.PropertyNames.GenerateIdClass,
                GeneratorSettings.Defaults.GenerateIdClass);

            var idClassName = options.GetValueOrDefault(
                GeneratorSettings.PropertyNames.IdClassName,
                GeneratorSettings.Defaults.IdClassName);

            return new GeneratorSettings(translationsDir, enableLogging, defaultLanguage, strictMode, generateXmlDocs, generateIdClass, idClassName);
        }
    }
}
