using System;

namespace MXR.SDK.Protocol.Guarantee {
    /// <summary>
    /// Injectable UTC clock for deadline tracking in tests and production.
    /// </summary>
    public interface IProtocolClock {
        /// <summary>
        /// Current UTC time.
        /// </summary>
        DateTime UtcNow { get; }
    }
}
