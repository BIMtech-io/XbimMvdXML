using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.MvdXml;

namespace Tests
{
    [TestClass]
    [DeploymentItem(@"TestFiles\")]
    public class IoTests
    {
        [TestMethod]
        public void CanLoadSampleXmlFile()
        {
            const string fileName = "ReferenceView_V1-2.mvdxml";
            var x = mvdXML.LoadFromFile(fileName);

            Assert.AreEqual(2, x.Templates.Length, "Error reading templates");
            Assert.AreEqual(1, x.Views.Length, "Error reading views");

        }
    }
}
