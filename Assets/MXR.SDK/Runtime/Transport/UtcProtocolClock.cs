using System;

using MXR.SDK.Protocol.Guarantee;

namespace MXR.SDK.Transport {
    /// <summary>
    /// Wall clock for ack and reply deadlines on a device.
    /// </summary>
    public sealed class UtcProtocolClock : IProtocolClock {
        /// <summary>
        /// Current UTC time.
        /// </summary>
        public DateTime UtcNow {
            get { return DateTime.UtcNow; }
        }
    }
}
