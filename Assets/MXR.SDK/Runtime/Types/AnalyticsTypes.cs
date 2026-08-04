using System.Collections.Generic;

using Newtonsoft.Json;

namespace MXR.SDK {
    /// <summary>
    /// Generic analytics event envelope sent from Home Screen to Admin App.
    /// Serializes to <c>{ "name": "...", "properties": { ... } }</c>, plus
    /// <c>"eventId"</c> when one is set.
    /// </summary>
    public class AnalyticsEventPayload {
        public string name;
        public Dictionary<string, object> properties;

        /// <summary>
        /// Optional idempotency key. Admin App adopts a well-formed UUID as the event's
        /// eventUUID, so replays dedupe. One id per emission, not per logical event.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string eventId;

        /// <summary>
        /// Serializes this payload to the JSON wire format expected by
        /// <see cref="IMXRSystem.SendAnalyticsEvent(string)"/>.
        /// </summary>
        public string ToJson() => JsonConvert.SerializeObject(this);
    }
}
