using System;
using System.Collections.Generic;

namespace MXR.SDK.Protocol.Subscriptions {
    /// <summary>
    /// Known state channel names on the V2 wire.
    /// </summary>
    public static class StateChannelNames {
        /// <summary>
        /// Current device status snapshot channel.
        /// </summary>
        public const string DeviceStatus = "deviceStatus";

        /// <summary>
        /// Runtime settings summary snapshot channel.
        /// </summary>
        public const string RuntimeSettings = "runtimeSettings";

        /// <summary>
        /// Device data snapshot channel.
        /// </summary>
        public const string DeviceData = "deviceData";

        static readonly IReadOnlyList<string> Names = Array.AsReadOnly(new[] {
            DeviceStatus,
            RuntimeSettings,
            DeviceData
        });

        /// <summary>
        /// Default bootstrap channels for a new connection. Read-only.
        /// </summary>
        public static IReadOnlyList<string> AsArray => Names;
    }
}
