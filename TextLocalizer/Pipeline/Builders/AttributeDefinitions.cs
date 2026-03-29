namespace TextLocalizer.Pipeline.Builders;

public static class AttributeDefinitions
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
}
