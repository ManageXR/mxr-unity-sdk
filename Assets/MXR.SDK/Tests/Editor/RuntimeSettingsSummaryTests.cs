using Newtonsoft.Json;

using NUnit.Framework;

namespace MXR.SDK.Tests {
    public class RuntimeSettingsSummaryTests {

        [Test]
        public void PicoFastBoundarySetting_On_EnablesQuickBoundarySetup() {
            var summary = Deserialize("{\"picoFastBoundarySetting\":\"ON\"}");

            Assert.AreEqual(PicoSystemSwitch.ON, summary.picoFastBoundarySetting);
            Assert.IsTrue(summary.IsQuickBoundarySetupEnabled);
        }

        [Test]
        public void PicoFastBoundarySetting_Off_DoesNotEnableQuickBoundarySetup() {
            var summary = Deserialize("{\"picoFastBoundarySetting\":\"OFF\"}");

            Assert.AreEqual(PicoSystemSwitch.OFF, summary.picoFastBoundarySetting);
            Assert.IsFalse(summary.IsQuickBoundarySetupEnabled);
        }

        [Test]
        public void PicoFastBoundarySetting_Absent_IsUnmanaged() {
            var summary = Deserialize("{}");

            Assert.AreEqual(PicoSystemSwitch.UNMANAGED, summary.picoFastBoundarySetting);
            Assert.IsFalse(summary.IsQuickBoundarySetupEnabled);
        }

        [Test]
        public void PicoFastBoundarySetting_UnknownValue_DoesNotEnableQuickBoundarySetup() {
            var summary = Deserialize("{\"picoFastBoundarySetting\":\"SOMETHING_NEW\"}");

            Assert.AreEqual(PicoSystemSwitch.UNKNOWN, summary.picoFastBoundarySetting);
            Assert.IsFalse(summary.IsQuickBoundarySetupEnabled);
        }

        [Test]
        public void DataCollection_WifiNetworkNameFalse_ReadsAsFalse() {
            var summary = Deserialize(
                "{\"organization\":{\"dataCollection\":{\"wifiNetworkName\":false,\"location\":\"approximate\"}}}");

            Assert.AreEqual(false, summary.organization.dataCollection.wifiNetworkName);
            Assert.AreEqual("approximate", summary.organization.dataCollection.location);
        }

        [Test]
        public void DataCollection_Absent_IsNull() {
            var summary = Deserialize("{\"organization\":{\"id\":\"org\"}}");

            Assert.IsNull(summary.organization.dataCollection);
        }

        [Test]
        public void DataCollection_MissingKey_IsNull() {
            var summary = Deserialize("{\"organization\":{\"dataCollection\":{\"ipAddress\":false}}}");

            Assert.IsNull(summary.organization.dataCollection.wifiNetworkName);
        }

        private static RuntimeSettingsSummary Deserialize(string json) =>
            JsonConvert.DeserializeObject<RuntimeSettingsSummary>(json);
    }
}
