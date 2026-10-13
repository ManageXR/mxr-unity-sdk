using Newtonsoft.Json;

using NUnit.Framework;

namespace MXR.SDK.Tests {
    /// <summary>
    /// Covers deserialization of <see cref="RuntimeApp.appVersionType"/> from the
    /// Admin App's runtimeSettingsSummary.json. The three EXTERNAL fixtures mirror
    /// the states observed on a device: never installed (placeholder id, versionCode -1),
    /// uninstalled after install (real id, versionCode -1) and installed (versionCode 1).
    /// </summary>
    public class RuntimeAppTests {

        [Test]
        public void RuntimeApp_WhenJsonHasExternalWithPlaceholderPrefix_ShouldDeserializeAsExternal() {
            var app = Deserialize(AppJson("EXTERNAL", "placeholder:6yZcFYoHstc7u2RCeXVS", -1, isPlaceholder: true));

            Assert.AreEqual(RuntimeAppType.EXTERNAL, app.appVersionType);
            Assert.AreEqual("placeholder:6yZcFYoHstc7u2RCeXVS", app.id);
            Assert.AreEqual(-1, app.versionCode);
        }

        [Test]
        public void RuntimeApp_WhenJsonHasExternalWithRealIdVersionMinusOne_ShouldDeserializeAsExternal() {
            var app = Deserialize(AppJson("EXTERNAL", "6yZcFYoHstc7u2RCeXVS", -1));

            Assert.AreEqual(RuntimeAppType.EXTERNAL, app.appVersionType);
            Assert.AreEqual("6yZcFYoHstc7u2RCeXVS", app.id);
            Assert.AreEqual(-1, app.versionCode);
        }

        [Test]
        public void RuntimeApp_WhenJsonHasExternalWithVersionCode1_ShouldDeserializeAsExternal() {
            var app = Deserialize(AppJson("EXTERNAL", "6yZcFYoHstc7u2RCeXVS", 1));

            Assert.AreEqual(RuntimeAppType.EXTERNAL, app.appVersionType);
            Assert.AreEqual(1, app.versionCode);
        }

        [Test]
        public void RuntimeApp_WhenJsonHasManaged_ShouldDeserializeAsManaged() {
            var app = Deserialize(AppJson("MANAGED", "Imh0OKqIoYQLxTBizO8a", 12));

            Assert.AreEqual(RuntimeAppType.MANAGED, app.appVersionType);
        }

        [Test]
        public void RuntimeApp_WhenJsonHasMetaHorizonStore_ShouldDeserializeAsMetaHorizonStore() {
            var app = Deserialize(AppJson("META_HORIZON_STORE", "Imh0OKqIoYQLxTBizO8a", 3));

            Assert.AreEqual(RuntimeAppType.META_HORIZON_STORE, app.appVersionType);
        }

        [Test]
        public void RuntimeApp_WhenJsonMissingAppVersionType_ShouldDefaultToUnknown() {
            // Admin App builds that pre-date the field omit the key entirely.
            var json = AppJson(null, "Imh0OKqIoYQLxTBizO8a", 12);
            Assume.That(json, Does.Not.Contain("appVersionType"));

            RuntimeApp app = null;
            Assert.DoesNotThrow(() => app = Deserialize(json));

            Assert.AreEqual(RuntimeAppType.UNKNOWN, app.appVersionType);
            Assert.AreEqual("Imh0OKqIoYQLxTBizO8a", app.id);
        }

        [Test]
        public void RuntimeApp_WhenJsonHasGarbageAppVersionType_ShouldDefaultToUnknown() {
            // A future enum value the SDK does not know about must not throw.
            var json = AppJson("SOME_FUTURE_TYPE", "Imh0OKqIoYQLxTBizO8a", 12);

            RuntimeApp app = null;
            Assert.DoesNotThrow(() => app = Deserialize(json));

            Assert.AreEqual(RuntimeAppType.UNKNOWN, app.appVersionType);
            Assert.AreEqual(12, app.versionCode);
        }

        [Test]
        public void RuntimeSettingsSummary_WhenAppsMixTypes_ShouldDeserializeEachApp() {
            var json = "{\"apps\":{"
                + "\"a.a.a.a\":" + AppJson("EXTERNAL", "placeholder:6yZcFYoHstc7u2RCeXVS", -1, isPlaceholder: true) + ","
                + "\"com.mightyimmersion.demovr.a\":" + AppJson("MANAGED", "Imh0OKqIoYQLxTBizO8a", 12) + ","
                + "\"com.legacy.app\":" + AppJson(null, "legacyId", 2)
                + "}}";

            var summary = JsonConvert.DeserializeObject<RuntimeSettingsSummary>(json);

            Assert.AreEqual(RuntimeAppType.EXTERNAL, summary.apps["a.a.a.a"].appVersionType);
            Assert.AreEqual(RuntimeAppType.MANAGED, summary.apps["com.mightyimmersion.demovr.a"].appVersionType);
            Assert.AreEqual(RuntimeAppType.UNKNOWN, summary.apps["com.legacy.app"].appVersionType);
        }

        private static RuntimeApp Deserialize(string json) =>
            JsonConvert.DeserializeObject<RuntimeApp>(json);

        /// <summary>
        /// Builds a single app entry shaped like the Admin App's runtimeSettingsSummary.json.
        /// Pass a null <paramref name="appVersionType"/> to omit the key.
        /// </summary>
        private static string AppJson(string appVersionType, string id, int versionCode, bool isPlaceholder = false) {
            var typeField = appVersionType == null ? "" : "\"appVersionType\":\"" + appVersionType + "\",";
            var placeholderField = isPlaceholder ? "\"isPlaceholder\":true," : "";
            return "{"
                + "\"apkSize\":0,"
                + typeField
                + "\"autoGrantPermissions\":true,"
                + "\"categories\":[],"
                + "\"expirationTimestamp\":0,"
                + "\"files\":{},"
                + "\"forceInstall\":false,"
                + "\"hasObb\":false,"
                + "\"hidden\":false,"
                + "\"id\":\"" + id + "\","
                + "\"launchParams\":{},"
                + "\"obbSize\":0,"
                + "\"organizationId\":\"testOrg\","
                + "\"originalAppId\":\"6yZcFYoHstc7u2RCeXVS\","
                + "\"sortOrder\":3,"
                + "\"sortPriority\":0,"
                + "\"title\":\"AAA\","
                + "\"versionCode\":" + versionCode + ","
                + "\"packageName\":\"a.a.a.a\","
                + placeholderField
                + "\"iconPath\":\"/MightyImmersion/icons/a.a.a.a\","
                + "\"isExpired\":false"
                + "}";
        }
    }
}
