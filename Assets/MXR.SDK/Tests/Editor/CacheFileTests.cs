using System.IO;

using NUnit.Framework;

namespace MXR.SDK.Tests {
    public class CacheFileTests {
        private const string Status = "{\"batteryLevel\":80}";

        private string _dir;
        private string _path;

        [SetUp]
        public void SetUp() {
            _dir = Path.Combine(Path.GetTempPath(), "mxr-cachefile-" + Path.GetRandomFileName());
            Directory.CreateDirectory(_dir);
            _path = Path.Combine(_dir, "deviceStatus.json");
        }

        [TearDown]
        public void TearDown() {
            if (Directory.Exists(_dir)) {
                Directory.Delete(_dir, true);
            }
        }

        [Test]
        public void Differs_MissingFile_IsTrue() {
            Assert.IsTrue(CacheFile.Differs(_path, Status));
        }

        [Test]
        public void Differs_LiteralNull_IsTrue() {
            File.WriteAllText(_path, "NULL");

            Assert.IsTrue(CacheFile.Differs(_path, Status));
        }

        [Test]
        public void Differs_EmptyFile_IsTrue() {
            File.WriteAllText(_path, string.Empty);

            Assert.IsTrue(CacheFile.Differs(_path, Status));
        }

        [Test]
        public void Differs_OlderValidContent_IsTrue() {
            File.WriteAllText(_path, "{\"batteryLevel\":79}");

            Assert.IsTrue(CacheFile.Differs(_path, Status));
        }

        [Test]
        public void Differs_SameContent_IsFalse() {
            File.WriteAllText(_path, Status);

            Assert.IsFalse(CacheFile.Differs(_path, Status));
        }

        [Test]
        public void Differs_PathIsDirectory_IsTrue() {
            Directory.CreateDirectory(_path);

            Assert.IsTrue(CacheFile.Differs(_path, Status));
        }
    }
}
