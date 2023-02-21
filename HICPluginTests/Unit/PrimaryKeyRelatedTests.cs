using NUnit.Framework;
using SCIStorePlugin.Data;

namespace SCIStorePluginTests.Unit
{
public class PrimaryKeyRelatedTests
{
    [Test]
    public void TestIdenticallity_Identical()
    {
        var r1 = new SciStoreResult
        {
            LabNumber = "fish"
        };
        var r2 = new SciStoreResult
        {
            LabNumber = "fish"
        };


        Assert.IsTrue(r1.IsIdenticalTo(r2));
    }
    [Test]
    public void TestIdenticallity_NotIdentical()
    {
        var r1 = new SciStoreResult
        {
            LabNumber = "fish",
            ReadCodeValue = "234"
        };
        var r2 = new SciStoreResult
        {
            LabNumber = "fish",
            ReadCodeValue = "2asd"
        };


        Assert.IsFalse(r1.IsIdenticalTo(r2));
    }
}