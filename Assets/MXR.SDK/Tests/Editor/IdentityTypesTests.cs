using Newtonsoft.Json;

using NUnit.Framework;

namespace MXR.SDK.Tests {
    public class IdentityTypesTests {

        [Test]
        public void Request_RoundTrips_AllFields() {
            var original = new UserIdentityRequest {
                identifierType = "EMAIL",
                emailDomain = "acme.com",
                customLabel = null,
                currentUserId = "jane@acme.com",
                targetPackage = "com.some.app"
            };

            var roundTripped = JsonConvert.DeserializeObject<UserIdentityRequest>(
                JsonConvert.SerializeObject(original));

            Assert.AreEqual(original.identifierType, roundTripped.identifierType);
            Assert.AreEqual(original.emailDomain, roundTripped.emailDomain);
            Assert.AreEqual(original.customLabel, roundTripped.customLabel);
            Assert.AreEqual(original.currentUserId, roundTripped.currentUserId);
            Assert.AreEqual(original.targetPackage, roundTripped.targetPackage);
        }

        [Test]
        public void Request_DeserializesFromAdminAppJson() {
            var json = "{\"identifierType\":\"NAME\",\"currentUserId\":null,\"targetPackage\":\"com.app\"}";

            var request = JsonConvert.DeserializeObject<UserIdentityRequest>(json);

            Assert.AreEqual("NAME", request.identifierType);
            Assert.AreEqual("com.app", request.targetPackage);
        }

        [Test]
        public void Request_NoIdentifiedUser_LeavesCurrentUserIdNull() {
            var json = "{\"identifierType\":\"NAME\",\"targetPackage\":\"com.app\"}";

            var request = JsonConvert.DeserializeObject<UserIdentityRequest>(json);

            Assert.IsNull(request.currentUserId);
        }

        [Test]
        public void Request_OptionalFields_DefaultToNullWhenAbsent() {
            var json = "{\"identifierType\":\"NUMBER\"}";

            var request = JsonConvert.DeserializeObject<UserIdentityRequest>(json);

            Assert.IsNull(request.emailDomain);
            Assert.IsNull(request.customLabel);
        }

        [Test]
        public void Response_RoundTrips_UserId() {
            var original = new UserIdentityResponse { userId = "jane@acme.com" };

            var roundTripped = JsonConvert.DeserializeObject<UserIdentityResponse>(
                JsonConvert.SerializeObject(original));

            Assert.AreEqual(original.userId, roundTripped.userId);
        }

        [Test]
        public void Response_SerializesToUserIdField() {
            var json = JsonConvert.SerializeObject(new UserIdentityResponse { userId = "abc" });

            Assert.AreEqual("{\"userId\":\"abc\"}", json);
        }
    }
}
