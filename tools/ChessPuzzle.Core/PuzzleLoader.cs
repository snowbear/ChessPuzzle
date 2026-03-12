using System.Text.Json;
using System.Text.Json.Serialization;
using ChessPuzzle.Core.Models;

namespace ChessPuzzle.Core;

public static class PuzzleLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new HintScopeConverter(),
            new CastleValueConverter()
        }
    };

    public static Puzzle FromJson(string json)
    {
        return JsonSerializer.Deserialize<Puzzle>(json, Options)
            ?? throw new JsonException("Failed to deserialize puzzle JSON.");
    }

    public static Puzzle FromFile(string path)
    {
        var json = File.ReadAllText(path);
        return FromJson(json);
    }
}

/// <summary>
/// Custom converter for HintScope which can be a string ("any", "final")
/// or an object with halfMove or halfMoveRange.
/// </summary>
internal class HintScopeConverter : JsonConverter<HintScope>
{
    public override HintScope Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            return value switch
            {
                "any" => new HintScope { IsAny = true },
                "final" => new HintScope { IsFinal = true },
                _ => throw new JsonException($"Unknown HintScope string value: '{value}'")
            };
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var scope = new HintScope();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return scope;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "halfMove":
                            scope.HalfMove = reader.GetInt32();
                            break;
                        case "halfMoveRange":
                            var range = new List<int>();
                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                                {
                                    range.Add(reader.GetInt32());
                                }
                            }
                            scope.HalfMoveRange = range.ToArray();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
            return scope;
        }

        throw new JsonException($"Unexpected token type for HintScope: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, HintScope value, JsonSerializerOptions options)
    {
        if (value.IsAny)
        {
            writer.WriteStringValue("any");
        }
        else if (value.IsFinal)
        {
            writer.WriteStringValue("final");
        }
        else if (value.HalfMoveRange != null)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("halfMoveRange");
            writer.WriteStartArray();
            foreach (var v in value.HalfMoveRange)
                writer.WriteNumberValue(v);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        else if (value.HalfMove.HasValue)
        {
            writer.WriteStartObject();
            writer.WriteNumber("halfMove", value.HalfMove.Value);
            writer.WriteEndObject();
        }
        else
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }
}

/// <summary>
/// Custom converter for CastleValue which can be a bool or a string.
/// </summary>
internal class CastleValueConverter : JsonConverter<CastleValue>
{
    public override CastleValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => new CastleValue(true),
            JsonTokenType.False => new CastleValue(false),
            JsonTokenType.String => new CastleValue(reader.GetString()!),
            _ => throw new JsonException($"Unexpected token type for CastleValue: {reader.TokenType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, CastleValue value, JsonSerializerOptions options)
    {
        if (value.IsBool)
            writer.WriteBooleanValue(value.BoolValue);
        else
            writer.WriteStringValue(value.StringValue);
    }
}
