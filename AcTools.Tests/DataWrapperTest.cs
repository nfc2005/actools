using System.IO;
using System.Reflection;
using AcTools.DataFile;
using AcTools.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AcTools.Tests {
    [TestClass]
    public class DataWrapperTest {
        [TestMethod]
        public void TestPacked() {
            var testDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (!testDir.EndsWith("AcTools.Tests") && testDir.Length > 4) testDir = Path.GetDirectoryName(testDir);
            testDir = Path.Combine(testDir, "test");

            var file = DataWrapper.FromDirectory(Path.Combine(testDir, "data", "peugeot_504"));
            Assert.IsTrue(file.GetRawFile("mirrors.ini").Content.StartsWith("VALID_LUT_FILE"));
            Assert.IsTrue(file.GetRawFile("power.lut").Content.StartsWith("VALID_LUT_FILE"));

            file = DataWrapper.FromDirectory(Path.Combine(testDir, "data", "peugeot_504_unpacked"));
            Assert.IsTrue(file.GetRawFile("mirrors.ini").Content.StartsWith("VALID_LUT_FILE"));
            Assert.IsTrue(file.GetRawFile("power.lut").Content.StartsWith("VALID_LUT_FILE"));
        }
    }
}