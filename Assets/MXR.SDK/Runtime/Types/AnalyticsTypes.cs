using System.Collections.Generic;

using Newtonsoft.Json;

namespace MXR.SDK {
    /// <summary>
    /// Generic analytics event envelope sent from Home Screen to Admin App.
    /// Serializes to <c>{ "name": "...", "properties": { ... } }</c>.
    /// </summary>
    public class AnalyticsEventPayload {
        public string name;
        public Dictionary<string, object> properties;

        /// <summary>
        /// Serializes this payload to the JSON wire format expected by
        /// <see cref="IMXRSystem.SendAnalyticsEvent(string)"/>.
        /// </summary>
        public string ToJson() => JsonConvert.SerializeObject(this);
    }
}
