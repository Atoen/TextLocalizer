using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TextLocalizer;

internal record struct AttributePropertyValue<T>
{
    public string Name { get; }
    public T DefaultValue { get; }

    public bool Read { get; private set; }
    public T Value => _value ?? DefaultValue;

    private T? _value;

    public AttributePropertyValue(string name, T defaultValue)
    {
        Name = name;
        DefaultValue = defaultValue;
    }

    public static implicit operator T(AttributePropertyValue<T> attributePropertyValue)
    {
        return attributePropertyValue.Value;
    }

    public void ReadIfEmpty(KeyValuePair<string, TypedConstant> property)
    {
        if (Read) return;

        if (property.Key == Name && property.Value.Value is T propertyValue)
        {
            _value = propertyValue;
            Read = true;
        }
    }
}

internal readonly record struct TranslationProviderAttributeData
{
    public readonly string Namespace;
    public readonly string ClassName;

    public readonly AttributePropertyValue<string> Language = new(nameof(Language), string.Empty);

    public TranslationProviderAttributeData(string @namespace, string className, AttributeData attributeData)
    {
        Namespace = @namespace;
        ClassName = className;

        foreach (var property in attributeData.NamedArguments)
        {
            Language.ReadIfEmpty(property);
        }
    }
}

internal readonly record struct TranslationTableAttributeData
{
    public readonly string Namespace;
    public readonly string ClassName;

    public readonly AttributePropertyValue<string> CurrentProviderAccessor = new(nameof(CurrentProviderAccessor), string.Empty);
    public readonly AttributePropertyValue<string> DefaultProviderAccessor = new(nameof(DefaultProviderAccessor), string.Empty);
    public readonly AttributePropertyValue<string> TableName = new(nameof(TableName), "Table");

    public TranslationTableAttributeData(string @namespace, string className, AttributeData attributeData)
    {
        Namespace = @namespace;
        ClassName = className;

        foreach (var property in attributeData.NamedArguments)
        {
            CurrentProviderAccessor.ReadIfEmpty(property);
            DefaultProviderAccessor.ReadIfEmpty(property);
            TableName.ReadIfEmpty(property);
        }
    }

    public bool IsValid()
    {
        return SyntaxFacts.IsValidIdentifier(CurrentProviderAccessor.Value) &&
               SyntaxFacts.IsValidIdentifier(DefaultProviderAccessor.Value) &&
               SyntaxFacts.IsValidIdentifier(TableName.Value);
    }
}
