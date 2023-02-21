using System;
using System.Collections.Generic;
using NUnit.Framework;
using ReusableLibraryCode.Progress;
using Rhino.Mocks;
using SCIStorePlugin.Data;
using SCIStorePlugin.DataProvider.RetryStrategies;
using SCIStorePlugin.Repositories;

namespace SCIStorePluginTests.Unit
{
public class LimitedRetryThenContinueStrategyTests
{
    [Test]
    public void Test()
    {
        var mockServer = MockRepository.Mock<IRepositorySupportsDateRangeQueries<CombinedReportData>>();

        var strat = new LimitedRetryThenContinueStrategy(5,new List<int>(new int[]{3,1}), mockServer);

            
        Assert.AreEqual(4,strat.RetryAfterCooldown(new TimeSpan(1,0,0,0), new ThrowImmediatelyDataLoadEventListener(), 5, new Exception()));
        Assert.AreEqual(3, strat.RetryAfterCooldown(new TimeSpan(1, 0, 0, 0), new ThrowImmediatelyDataLoadEventListener(), 4, new Exception()));
        Assert.AreEqual(2, strat.RetryAfterCooldown(new TimeSpan(1, 0, 0, 0), new ThrowImmediatelyDataLoadEventListener(), 3, new Exception()));
        Assert.AreEqual(1, strat.RetryAfterCooldown(new TimeSpan(1, 0, 0, 0), new ThrowImmediatelyDataLoadEventListener(), 2, new Exception()));
        Assert.AreEqual(0, strat.RetryAfterCooldown(new TimeSpan(1, 0, 0, 0), new ThrowImmediatelyDataLoadEventListener(), 1, new Exception()));

    }
}