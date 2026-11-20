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
    }
}
