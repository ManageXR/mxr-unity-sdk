using System;

namespace MXR.SDK {
    [Serializable]
    public class UserIdentityRequest {
        public string identifierType;   // NAME | EMAIL | NUMBER | CUSTOM
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
}
