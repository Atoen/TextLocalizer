using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TextLocalizer;

internal static class Extensions
{
    extension(AnalyzerConfigOptions analyzerConfigOptions)
    {
        public T GetValueOrDefault<T>(string optionName, T defaultValue)
        {
            if (!analyzerConfigOptions.TryGetValue(optionName, out var rawValue))
            {
                return defaultValue;
            }

            var targetType = typeof(T);

            try
            {
                if (targetType == typeof(string))
                {
                    return Unsafe.As<string, T>(ref rawValue);
                }

                if (targetType == typeof(bool))
                {
                    if (bool.TryParse(rawValue, out var b))
                    {
                        return Unsafe.As<bool, T>(ref b);
                    }

                    return defaultValue;
                }

                if (targetType == typeof(int))
                {
                    if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    {
                        return Unsafe.As<int, T>(ref i);
                    }

                    return defaultValue;
                }

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
