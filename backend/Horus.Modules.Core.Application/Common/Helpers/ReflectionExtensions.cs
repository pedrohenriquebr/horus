using System.Reflection;
using System.Text.Json;
using Humanizer;

namespace Horus.Modules.Core.Application.Common.Helpers;

public class ReflectionExtensions
{
    
    
    public static  Type GetPropertyType(Type entityType, string propertyName)
    {
        return entityType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.PropertyType 
               ?? throw new InvalidOperationException($"Property {propertyName} not found");
    }

    public static void CopyProperties(dynamic source, object destination)
    {
        // Se source for um JsonElement
        if (source is JsonElement jsonElement)
        {
            // Converte para string JSON e depois para um objeto anônimo ou Dictionary
            string json = jsonElement.GetRawText();
            var dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        
            var destinationProperties = destination.GetType().GetProperties();
        
            foreach (var sourceProperty in dictionary)
            {
                var destinationProperty = destinationProperties
                    .FirstOrDefault(p => p.Name == sourceProperty.Key.Pascalize() && p.CanWrite);
                
                if (destinationProperty != null)
                {
                    var value = sourceProperty.Value;
            
                    if (value is JsonElement jsonEl)
                    {
                        // Converte o JsonElement para o tipo correto da propriedade de destino
                        var convertedValue = ConvertFromJsonElement(jsonEl, destinationProperty.PropertyType);
                        destinationProperty.SetValue(destination, convertedValue);
                    }
                    else
                    {
                        // Para outros tipos que não são JsonElement
                        destinationProperty.SetValue(destination, value);
                    }
                }
            }
        }
        else
        {
            // Sua implementação original para outros tipos dinâmicos
            CopyProperties(source, destination);
        }
    }
    

    public static void SetProperty(object obj, string propertyName, object value)
    {
        var property = obj.GetType().GetProperty(propertyName);
        if (property != null && property.CanWrite)
        {
            property.SetValue(obj, value);
        }
    }
    
    public static object ConvertFromJsonElement(JsonElement element, Type targetType)
{
    // Se o elemento for nulo ou indefinido
    if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
        return null;

    // Tratamento especial para Guid
    if (targetType == typeof(Guid) || targetType == typeof(Guid?))
    {
        if (element.ValueKind == JsonValueKind.String)
            return Guid.Parse(element.GetString());
    }
    // DateTime
    else if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
    {
        if (element.ValueKind == JsonValueKind.String)
            return DateTime.Parse(element.GetString());
    }
    // String
    else if (targetType == typeof(string))
    {
        return element.ToString();
    }
    // Tipos numéricos
    else if (targetType == typeof(int) || targetType == typeof(int?))
    {
        return element.GetInt32();
    }
    else if (targetType == typeof(long) || targetType == typeof(long?))
    {
        return element.GetInt64();
    }
    else if (targetType == typeof(double) || targetType == typeof(double?))
    {
        return element.GetDouble();
    }
    else if (targetType == typeof(decimal) || targetType == typeof(decimal?))
    {
        return element.GetDecimal();
    }
    // Boolean
    else if (targetType == typeof(bool) || targetType == typeof(bool?))
    {
        return element.GetBoolean();
    }
    // Para tipos enumerados
    else if (targetType.IsEnum)
    {
        if (element.ValueKind == JsonValueKind.Number)
            return Enum.ToObject(targetType, element.GetInt32());
        else if (element.ValueKind == JsonValueKind.String)
            return Enum.Parse(targetType, element.GetString());
    }
    // Para tipos complexos, considere usar desserialização
    else if (element.ValueKind == JsonValueKind.Object || element.ValueKind == JsonValueKind.Array)
    {
        return JsonSerializer.Deserialize(element.GetRawText(), targetType);
    }

    // Se não conseguir converter, retorne null
    return null;
}
}