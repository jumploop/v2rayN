namespace ServiceLib.Tests.Services;

public class SpeedtestServiceBatchTests
{
    [Test]
    [Arguments(0)]
    [Arguments(1000)]
    public async Task EmptySelection_ShouldReturnNoBatches(int pageSize)
    {
        await GetBatches([], pageSize).Count.Should().BeEqualTo(0);
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task NonPositivePageSize_ShouldKeepEveryEligibleProfile(int pageSize)
    {
        var profiles = CreateProfiles();
        var batches = GetBatches(profiles, pageSize);

        await batches.Count.Should().BeEqualTo(profiles.Count);
        await batches.SelectMany(batch => batch).Count().Should().BeEqualTo(profiles.Count);
        foreach (var profile in profiles)
        {
            await batches.SelectMany(batch => batch).Count(item => ReferenceEquals(item, profile)).Should().BeEqualTo(1);
        }
    }

    [Test]
    public async Task NormalBatches_ShouldRespectPageSizeAndKeepCoreTypesSeparate()
    {
        var profiles = CreateProfiles();
        var batches = GetBatches(profiles, 2);

        await batches.Count.Should().BeEqualTo(3);
        await batches.SelectMany(batch => batch).Count().Should().BeEqualTo(profiles.Count);
        foreach (var batch in batches)
        {
            await (batch.Count > 0 && batch.Count <= 2).Should().BeTrue();
            await batch.Select(item => item.CoreType).Distinct().Count().Should().BeEqualTo(1);
        }
    }

    [Test]
    public async Task RunMixedTestAsync_WhenCoreFails_ShouldMarkItemAsFailedAndNotStuck()
    {
        List<SpeedTestResult> results = [];
        var service = new SpeedtestService(new Config { SpeedTestItem = new() }, result =>
        {
            results.Add(result);
            return Task.CompletedTask;
        });

        var completedIds = new ConcurrentDictionary<string, byte>();
        var testItems = new List<ServerTestItem>
        {
            new() { IndexId = "test-fail-1", AllowTest = true }
        };

        var method = typeof(SpeedtestService).GetMethod("RunMixedTestAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await (Task)method.Invoke(service, [testItems, completedIds, 1, false, CancellationToken.None])!;

        await completedIds.ContainsKey("test-fail-1").Should().BeTrue();
        await results.Any(r => r.IndexId == "test-fail-1" && r.Delay == "-1").Should().BeTrue();
    }

    private static List<ServerTestItem> CreateProfiles() =>
    [
        new() { CoreType = ECoreType.Xray },
        new() { CoreType = ECoreType.Xray },
        new() { CoreType = ECoreType.Xray },
        new() { CoreType = ECoreType.sing_box },
        new() { CoreType = ECoreType.sing_box }
    ];

    private static List<List<ServerTestItem>> GetBatches(List<ServerTestItem> profiles, int pageSize)
    {
        var service = new SpeedtestService(new Config { SpeedTestItem = new() }, _ => Task.CompletedTask);
        var method = typeof(SpeedtestService).GetMethod("GetTestBatchItem", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (List<List<ServerTestItem>>)method.Invoke(service, [profiles, pageSize])!;
    }
}
