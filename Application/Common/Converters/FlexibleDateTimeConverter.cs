using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SMU.Application.Common.Converters;

/// <summary>
/// Converter linh hoạt hỗ trợ parse DateTime từ nhiều định dạng:
/// - ISO 8601 (yyyy-MM-dd, yyyy-MM-ddTHH:mm:ssZ...)
/// - Định dạng ngày Việt Nam (dd/MM/yyyy, d/M/yyyy, dd/MM/yyyy HH:mm:ss...)
/// - Dấu gạch ngang (dd-MM-yyyy...)
/// - Dấu gạch chéo (yyyy/MM/dd...)
/// - Unix timestamp (seconds hoặc milliseconds)
/// </summary>
public class FlexibleDateTimeConverter : JsonConverter<DateTime>
{
    private static readonly string[] Formats =
    [
        "yyyy-MM-ddTHH:mm:ss.fffffffZ",
        "yyyy-MM-ddTHH:mm:ss.ffffffZ",
        "yyyy-MM-ddTHH:mm:ss.fffZ",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss.fffffff",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd",
        "dd/MM/yyyy HH:mm:ss",
        "dd/MM/yyyy HH:mm",
        "dd/MM/yyyy",
        "d/M/yyyy HH:mm:ss",
        "d/M/yyyy HH:mm",
        "d/M/yyyy",
        "dd-MM-yyyy HH:mm:ss",
        "dd-MM-yyyy",
        "d-M-yyyy",
        "yyyy/MM/dd HH:mm:ss",
        "yyyy/MM/dd",
        "yyyy/M/d"
    ];

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(str))
            {
                return default;
            }

            if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dtIso))
            {
                return dtIso;
            }

            if (DateTime.TryParseExact(str, Formats, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dtExact))
            {
                return dtExact;
            }

            if (DateTime.TryParse(str, new CultureInfo("vi-VN"), DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dtVi))
            {
                return dtVi;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            var val = reader.GetInt64();
            if (val > 100_000_000_000)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(val).UtcDateTime;
            }
            return DateTimeOffset.FromUnixTimeSeconds(val).UtcDateTime;
        }

        return reader.GetDateTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Converter linh hoạt hỗ trợ parse Nullable DateTime (DateTime?) từ nhiều định dạng khác nhau.
/// </summary>
public class FlexibleNullableDateTimeConverter : JsonConverter<DateTime?>
{
    private static readonly string[] Formats =
    [
        "yyyy-MM-ddTHH:mm:ss.fffffffZ",
        "yyyy-MM-ddTHH:mm:ss.ffffffZ",
        "yyyy-MM-ddTHH:mm:ss.fffZ",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss.fffffff",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd",
        "dd/MM/yyyy HH:mm:ss",
        "dd/MM/yyyy HH:mm",
        "dd/MM/yyyy",
        "d/M/yyyy HH:mm:ss",
        "d/M/yyyy HH:mm",
        "d/M/yyyy",
        "dd-MM-yyyy HH:mm:ss",
        "dd-MM-yyyy",
        "d-M-yyyy",
        "yyyy/MM/dd HH:mm:ss",
        "yyyy/MM/dd",
        "yyyy/M/d"
    ];

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }

            if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dtIso))
            {
                return dtIso;
            }

            if (DateTime.TryParseExact(str, Formats, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dtExact))
            {
                return dtExact;
            }

            if (DateTime.TryParse(str, new CultureInfo("vi-VN"), DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dtVi))
            {
                return dtVi;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            var val = reader.GetInt64();
            if (val > 100_000_000_000)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(val).UtcDateTime;
            }
            return DateTimeOffset.FromUnixTimeSeconds(val).UtcDateTime;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
