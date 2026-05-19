using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rdmp.Core.ReusableLibraryCode.Progress;
using SCIStorePlugin.Data;
using SCIStorePlugin.DataProvider.RetryStrategies;
using SCIStorePlugin.Repositories;
using NSubstitute;

namespace SCIStorePluginTests.Unit;

public class LimitedRetryThenContinueStrategyTests
{
    [Test]
    public void Test()
    {
        var mockServer = Substitute.For<IRepositorySupportsDateRangeQueries<CombinedReportData>>();

        var strategy = new LimitedRetryThenContinueStrategy(5,new List<int>(new int[]{3,1}), mockServer);

            
       Assert.That(4,Is.EqualTo(strategy.RetryAfterCooldown(new TimeSpan(1,0,0,0), ThrowImmediatelyDataLoadEventListener.Quiet, 5, new Exception())));
       Assert.That(3, Is.EqualTo(strategy.RetryAfterCooldown(new TimeSpan(1, 0, 0, 0), ThrowImmediatelyDataLoadEventListener.Quiet, 4, new Exception())));
       Assert.That(2, Is.EqualTo(strategy.RetryAfterCooldown(new TimeSpan(1, 0, 0, 0), ThrowImmediatelyDataLoadEventListener.Quiet, 3, new Exception())));
       Assert.That(1, Is.EqualTo(strategy.RetryAfterCooldown(new TimeSpan(1, 0, 0, 0), ThrowImmediatelyDataLoadEventListener.Quiet, 2, new Exception())));
       Assert.That(0, Is.EqualTo(strategy.RetryAfterCooldown(new TimeSpan(1, 0, 0, 0), ThrowImmediatelyDataLoadEventListener.Quiet, 1, new Exception())));

    }
}