using NUnit.Framework;
using SCIStorePlugin.Data;

namespace SCIStorePluginTests.Unit
{
    public class PrimaryKeyRelatedTests
    {
        [Test]
        public void TestIdenticallity_Identical()
        {
            SciStoreResult r1 = new SciStoreResult();
            SciStoreResult r2 = new SciStoreResult();


            r1.LabNumber = "fish";
            r2.LabNumber = "fish";


            Assert.IsTrue(r1.IsIdenticalTo(r2));
        }
        [Test]
        public void TestIdenticallity_NotIdentical()
        {
            SciStoreResult r1 = new SciStoreResult();
            SciStoreResult r2 = new SciStoreResult();


            r1.LabNumber = "fish";
            r2.LabNumber = "fish";

            r1.ReadCodeValue = "234";
            r2.ReadCodeValue = "2asd";
            
            Assert.IsFalse(r1.IsIdenticalTo(r2));
        }
    }
}
