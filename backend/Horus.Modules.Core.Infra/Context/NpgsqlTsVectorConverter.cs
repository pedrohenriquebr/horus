using Newtonsoft.Json;
using NpgsqlTypes;

namespace Horus.Modules.Core.Infra.Context;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;

public class NpgsqlTsVectorConverter : JsonConverter<NpgsqlTsVector>
{
    public override void WriteJson(JsonWriter writer, NpgsqlTsVector value, JsonSerializer serializer)
    {
        writer.WriteValue(value?.ToString());
    }

    public override NpgsqlTsVector ReadJson(JsonReader reader, Type objectType, NpgsqlTsVector existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        
        if (reader.TokenType == JsonToken.StartArray) 
        {
            var lexemes = JArray.Load(reader);
            var tsVectorText = string.Join(" ", lexemes.Select(l => 
                $"{l["Text"]}:{l["Count"]}"));
            return NpgsqlTsVector.Parse(tsVectorText);
        }
        
        if (reader.TokenType == JsonToken.Null) return null;

        if (reader.TokenType == JsonToken.String)
        {
            return NpgsqlTsVector.Parse((string)reader.Value);
        }

        if (reader.TokenType == JsonToken.StartObject)
        {
            var jObject = JObject.Load(reader);
            string tsVectorString = jObject["value"]?.ToString() ?? jObject["$values"]?.ToString();
            if (tsVectorString != null)
            {
                return NpgsqlTsVector.Parse(tsVectorString);
            }
            throw new JsonSerializationException("No valid string representation found in NpgsqlTsVector JSON object.");
        }

        throw new JsonSerializationException($"Unexpected token: {reader.TokenType}");
    }
}