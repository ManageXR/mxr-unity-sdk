using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using UnityEngine;
using UnityEngine.Scripting;

namespace MXR.SDK {
    /// <summary>
    /// A JSON converter for enums that gracefully handles unknown values instead of throwing.
    /// When an unrecognized string value is encountered during deserialization, it defaults to
    /// the enum's 0-value (typically UNKNOWN) and logs a warning.
    /// This provides forward-compatibility when the API introduces new enum values that
    /// older SDK builds don't have.
    /// </summary>
    [Preserve]
    public class TolerantStringEnumConverter : StringEnumConverter {
        [Preserve]
        public TolerantStringEnumConverter() { }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            var enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;

            try {
                return base.ReadJson(reader, objectType, existingValue, serializer);
            }
            catch (JsonSerializationException) {
                var fallback = Enum.ToObject(enumType, 0);
                Debug.LogWarning(
                    $"[MXR SDK] Unknown enum value '{reader.Value}' for type {enumType.Name}, defaulting to {fallback}");
                return fallback;
            }
        }
    }
}
