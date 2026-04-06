using System;

using Newtonsoft.Json;

using NUnit.Framework;

namespace MXR.SDK.Tests {
    public class TolerantStringEnumConverterTests {

        // Test enum that uses the converter, mirroring the pattern in the SDK
        [JsonConverter(typeof(TolerantStringEnumConverter))]
        enum TestStatus {
            UNKNOWN,
            ACTIVE,
            INACTIVE
        }

        class TestObject {
            public TestStatus status;
            public string name;
        }

        class TestObjectWithNullable {
            [JsonConverter(typeof(TolerantStringEnumConverter))]
            public TestStatus? status;
        }

        [Test]
        public void DeserializesKnownValues() {
            var json = "{\"status\":\"ACTIVE\",\"name\":\"test\"}";
            var obj = JsonConvert.DeserializeObject<TestObject>(json);
            Assert.AreEqual(TestStatus.ACTIVE, obj.status);
            Assert.AreEqual("test", obj.name);
        }

        [Test]
        public void UnknownValueDefaultsToZero() {
            var json = "{\"status\":\"BRAND_NEW_STATUS\",\"name\":\"test\"}";
            var obj = JsonConvert.DeserializeObject<TestObject>(json);
            Assert.AreEqual(TestStatus.UNKNOWN, obj.status);
            Assert.AreEqual("test", obj.name);
        }

        [Test]
        public void UnknownValueDoesNotThrow() {
            var json = "{\"status\":\"SOME_FUTURE_VALUE\"}";
            Assert.DoesNotThrow(() => JsonConvert.DeserializeObject<TestObject>(json));
        }

        [Test]
        public void NullableEnumWithKnownValue() {
            var json = "{\"status\":\"ACTIVE\"}";
            var obj = JsonConvert.DeserializeObject<TestObjectWithNullable>(json);
            Assert.AreEqual(TestStatus.ACTIVE, obj.status);
        }

        [Test]
        public void NullableEnumWithNullValue() {
            var json = "{\"status\":null}";
            var obj = JsonConvert.DeserializeObject<TestObjectWithNullable>(json);
            Assert.IsNull(obj.status);
        }

        [Test]
        public void NullableEnumWithUnknownValue() {
            var json = "{\"status\":\"FUTURE_STATUS\"}";
            var obj = JsonConvert.DeserializeObject<TestObjectWithNullable>(json);
            Assert.AreEqual(TestStatus.UNKNOWN, obj.status);
        }

        [Test]
        public void SerializesKnownValues() {
            var obj = new TestObject { status = TestStatus.ACTIVE, name = "test" };
            var json = JsonConvert.SerializeObject(obj);
            Assert.That(json, Does.Contain("\"ACTIVE\""));
        }

        [Test]
        public void DeserializesManagedFileTypeWithKnownValue() {
            var json = "{\"managedFileType\":\"FILE\",\"status\":\"COMPLETE\",\"id\":\"123\"}";
            var obj = JsonConvert.DeserializeObject<FileInstallStatus>(json);
            Assert.AreEqual(FileInstallStatus.ManagedFileType.FILE, obj.managedFileType);
        }

        [Test]
        public void DeserializesManagedFileTypeWithUnknownValue() {
            var json = "{\"managedFileType\":\"HOLOGRAM\",\"status\":\"COMPLETE\",\"id\":\"123\"}";
            var obj = JsonConvert.DeserializeObject<FileInstallStatus>(json);
            Assert.AreEqual(FileInstallStatus.ManagedFileType.UNKNOWN, obj.managedFileType);
        }

        [Test]
        public void FullDeviceStatusWithUnknownEnumValues() {
            var json = @"{
                ""serial"": ""ABC123"",
                ""fileStatuses"": {
                    ""file1"": {
                        ""status"": ""COMPLETE"",
                        ""managedFileType"": ""FUTURE_TYPE"",
                        ""id"": ""file1""
                    }
                },
                ""appStatuses"": {}
            }";
            var obj = JsonConvert.DeserializeObject<DeviceStatus>(json);
            Assert.AreEqual("ABC123", obj.serial);
            Assert.AreEqual(FileInstallStatus.ManagedFileType.UNKNOWN, obj.fileStatuses["file1"].managedFileType);
            Assert.AreEqual(FileInstallStatus.Status.COMPLETE, obj.fileStatuses["file1"].status);
        }

        [Test]
        public void MultipleUnknownEnumsInSameObject() {
            var json = "{\"managedFileType\":\"NEW_TYPE\",\"status\":\"NEW_STATUS\",\"id\":\"123\"}";
            var obj = JsonConvert.DeserializeObject<FileInstallStatus>(json);
            Assert.AreEqual(FileInstallStatus.ManagedFileType.UNKNOWN, obj.managedFileType);
            Assert.AreEqual(FileInstallStatus.Status.UNKNOWN, obj.status);
        }
    }
}
