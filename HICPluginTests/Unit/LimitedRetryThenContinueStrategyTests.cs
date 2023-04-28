using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;
using SCIStorePlugin.DataProvider.RetryStrategies;
using SCIStorePlugin.Repositories;

namespace SCIStorePluginTests.Unit;

public class LimitedRetryThenContinueStrategyTests
{
    [Test]
    public void Test()
    {
        var mockServer = new Moq.Mock<IRepositorySupportsDateRangeQueries<CombinedReportData>>().Object;

        var strategy = new LimitedRetryThenContinueStrategy(5,new List<int>(new int[]{3,1}), mockServer);

            
        Assert.AreEqual(4,strategy.RetryAfterCooldown(new TimeSpan(1,0,0,0), new ThrowImmediatelyDataLoadEventListener(), 5, new Exception()));
        Assert.AreEqual(3, strategy.RetryAfterCooldown(new TimeSpan(1, 0, 0, 0), new ThrowImmediatelyDataLoadEventListener(), 4, new Exception()));
        Assert.AreEqual(2, strategy.RetryAfterCooldown(new TimeSpan(1, 0, 0, 0), new ThrowImmediatelyDataLoadEventListener(), 3, new Exception()));
        Assert.AreEqual(1, strategy.RetryAfterCooldown(new TimeSpan(1, 0, 0, 0), new ThrowImmediatelyDataLoadEventListener(), 2, new Exception()));
        Assert.AreEqual(0, strategy.RetryAfterCooldown(new TimeSpan(1, 0, 0, 0), new ThrowImmediatelyDataLoadEventListener(), 1, new Exception()));

    }
}