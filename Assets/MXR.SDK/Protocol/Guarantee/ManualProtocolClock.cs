using System;

namespace MXR.SDK.Protocol.Guarantee {
    /// <summary>
    /// Test double that advances time explicitly.
    /// </summary>
    public sealed class ManualProtocolClock : IProtocolClock {
        /// <inheritdoc />
        public DateTime UtcNow { get; private set; }

        /// <summary>
        /// Creates a clock starting at the given UTC instant.
        /// </summary>
        public ManualProtocolClock(DateTime utcNow) {
            UtcNow = utcNow;
        }

        /// <summary>
        /// Advances the clock by the given duration.
        /// </summary>
        public void Advance(TimeSpan duration) {
            UtcNow = UtcNow.Add(duration);
        }
    }
}
