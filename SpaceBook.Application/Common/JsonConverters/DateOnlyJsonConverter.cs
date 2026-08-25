using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceBook.Application.Common.JsonConverters;

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string DateFormat = "yyyy-MM-dd";

    public override DateOnly Read(
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

            if (TryParseDateOnly(value, out var date))
            {
                return date;
            }
        }

        throw new JsonException($"Unable to convert \"{reader.GetString()}\" to DateOnly.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateOnly value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(DateFormat, CultureInfo.InvariantCulture));
    }

    internal static bool TryParseDateOnly(string value, out DateOnly result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();

        // 1. If ISO timestamp with 'T' (e.g. 2026-08-26T00:00:00.000Z), extract date portion
        if (trimmed.Contains('T'))
        {
            var datePart = trimmed[..trimmed.IndexOf('T')];
            if (DateOnly.TryParseExact(datePart, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return true;
            }
            if (DateOnly.TryParse(datePart, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return true;
            }
        }

        // 2. Exact DateOnly format
        if (DateOnly.TryParseExact(trimmed, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            return true;
        }

        // 3. Flexible DateOnly parsing
        if (DateOnly.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            return true;
        }

        // 4. Fallback for DateTime / DateTimeOffset
        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDateTime))
        {
            result = DateOnly.FromDateTime(parsedDateTime);
            return true;
        }

        if (DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedOffset))
        {
            result = DateOnly.FromDateTime(parsedOffset.DateTime);
            return true;
        }

        return false;
    }
}

public class NullableDateOnlyJsonConverter : JsonConverter<DateOnly?>
{
    private const string DateFormat = "yyyy-MM-dd";

    public override DateOnly? Read(
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

            if (DateOnlyJsonConverter.TryParseDateOnly(value, out var date))
            {
                return date;
            }
        }

        throw new JsonException($"Unable to convert \"{reader.GetString()}\" to Nullable<DateOnly>.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateOnly? value,
        JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString(DateFormat, CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
