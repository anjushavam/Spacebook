using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceBook.Application.Common.JsonConverters;

public class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    private const string TimeFormat = "HH:mm:ss";

    public override TimeOnly Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return default;
            }

            if (TryParseTimeOnly(value, out var time))
            {
                return time;
            }
        }

        throw new JsonException($"Unable to convert \"{reader.GetString()}\" to TimeOnly.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        TimeOnly value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(TimeFormat, CultureInfo.InvariantCulture));
    }

    internal static bool TryParseTimeOnly(string value, out TimeOnly result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();

        // 1. Direct TimeOnly formats: "09:00", "09:00:00", "9:00", "9:00 AM", etc.
        if (TimeOnly.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            return true;
        }

        // 2. If ISO timestamp with 'T' (e.g. 2026-08-26T09:00:00.000Z), extract time portion directly
        if (trimmed.Contains('T'))
        {
            var timePart = trimmed[(trimmed.IndexOf('T') + 1)..].TrimEnd('Z');
            if (timePart.Contains('+'))
            {
                timePart = timePart[..timePart.IndexOf('+')];
            }
            else if (timePart.Contains('-'))
            {
                timePart = timePart[..timePart.IndexOf('-')];
            }

            if (TimeOnly.TryParse(timePart, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return true;
            }
        }

        // 3. Fallback to DateTime / DateTimeOffset
        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDateTime))
        {
            result = TimeOnly.FromDateTime(parsedDateTime);
            return true;
        }

        return false;
    }
}

public class NullableTimeOnlyJsonConverter : JsonConverter<TimeOnly?>
{
    private const string TimeFormat = "HH:mm:ss";

    public override TimeOnly? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (TimeOnlyJsonConverter.TryParseTimeOnly(value, out var time))
            {
                return time;
            }
        }

        throw new JsonException($"Unable to convert \"{reader.GetString()}\" to Nullable<TimeOnly>.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        TimeOnly? value,
        JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString(TimeFormat, CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
