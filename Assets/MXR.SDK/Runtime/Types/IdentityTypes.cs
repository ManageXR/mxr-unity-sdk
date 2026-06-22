using Newtonsoft.Json;

using System;

namespace MXR.SDK {
    [Serializable]
    public class UserIdentityRequest {
        public UserIdentifierType identifierType;
        public string emailDomain;      // EMAIL only
        public string customLabel;      // CUSTOM only
        public string currentUserId;
        public string targetPackage;
    }

    [Serializable]
    public class UserIdentityResponse {
        public string userId;
        public string intentId;
    }

    [JsonConverter(typeof(TolerantStringEnumConverter))]
    public enum UserIdentifierType {
        NAME,
        EMAIL,
        NUMBER,
        CUSTOM
    }
}
