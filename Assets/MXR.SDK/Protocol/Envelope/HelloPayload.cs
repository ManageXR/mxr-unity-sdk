using System;
using System.Collections.Generic;

namespace MXR.SDK.Protocol {
    /// <summary>
    /// Payload for <see cref="ProtocolTypes.Hello"/>. Always JSON on the wire regardless
    /// of the payload format negotiated for subsequent envelopes.
    /// </summary>
    public sealed class HelloPayload {
        /// <summary>
        /// Protocol version this side speaks.
        /// </summary>
        public int Protocol { get; set; }

        /// <summary>
        /// Minimum protocol version this side accepts.
        /// </summary>
        public int MinProtocol { get; set; }

        /// <summary>
        /// Supported payload formats in preference order.
        /// </summary>
        public IReadOnlyList<string> Formats { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Implementation identity, e.g. <c>mxr-unity-sdk/2.0.0</c>.
        /// </summary>
        public string Impl { get; set; } = string.Empty;
    }
}
