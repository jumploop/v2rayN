using ServiceLib.Models.CoreConfigs;
using ServiceLib.Services.CoreConfig;
using ServiceLib.Tests.CoreConfig;

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
    public async Task Batches_WithOtherCoreTypes_ShouldBatchOnlyBatchableCores()
    {
        var profiles = new List<ServerTestItem>
        {
            new() { CoreType = ECoreType.Xray },
            new() { CoreType = ECoreType.sing_box },
            new() { CoreType = ECoreType.mihomo },
        };
        var batches = GetBatches(profiles, 10);
        // mihomo is handled separately via individual mixed tests, not batched into Xray/sing_box
        await batches.Count.Should().BeEqualTo(2);
        await batches.SelectMany(b => b).Count().Should().BeEqualTo(2);
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

    [Test]
    public async Task SingboxSpeedtestConfig_ShouldHaveDirectOutboundAndRouteFinal()
    {
        var config = CoreConfigTestFactory.CreateConfig(ECoreType.sing_box);
        CoreConfigTestFactory.BindAppManagerConfig(config);

        var node1 = new ProfileItem
        {
            IndexId = "node1",
            Remarks = "tuic-node1",
            ConfigType = EConfigType.TUIC,
            Address = "1.2.3.4",
            Port = 443,
            CoreType = ECoreType.sing_box,
            Password = "pass"
        };
        var context = CoreConfigTestFactory.CreateContext(config, node1, ECoreType.sing_box);
        context.AllProxiesMap[node1.IndexId] = node1;

        var selecteds = new List<ServerTestItem>
        {
            new()
            {
                IndexId = node1.IndexId,
                Address = node1.Address,
                Port = node1.Port,
                ConfigType = node1.ConfigType,
                Profile = node1,
                CoreType = ECoreType.sing_box
            }
        };

        var result = new CoreConfigSingboxService(context).GenerateClientSpeedtestConfig(selecteds);
        await result.Success.Should().BeTrue();

        var singboxConfig = JsonUtils.Deserialize<SingboxConfig>(result.Data!.ToString())!;
        await singboxConfig.outbounds.Should().Contain(o => o.tag == Global.DirectTag);
        await singboxConfig.route.final.Should().BeEqualTo(Global.DirectTag);
        await selecteds.First().Port.Should().BeGreaterThan(0);
        await selecteds.First().AllowTest.Should().BeTrue();
    }

    [Test]
    public async Task V2raySpeedtestConfig_ShouldAllocateValidPorts()
    {
        var config = CoreConfigTestFactory.CreateConfig(ECoreType.Xray);
        CoreConfigTestFactory.BindAppManagerConfig(config);

        var node1 = new ProfileItem
        {
            IndexId = "node-vless",
            Remarks = "vless-node1",
            ConfigType = EConfigType.VLESS,
            Address = "1.2.3.4",
            Port = 443,
            CoreType = ECoreType.Xray,
            Password = "uuid"
        };
        var context = CoreConfigTestFactory.CreateContext(config, node1, ECoreType.Xray);
        context.AllProxiesMap[node1.IndexId] = node1;

        var selecteds = new List<ServerTestItem>
        {
            new()
            {
                IndexId = node1.IndexId,
                Address = node1.Address,
                Port = node1.Port,
                ConfigType = node1.ConfigType,
                Profile = node1,
                CoreType = ECoreType.Xray
            }
        };

        var result = new CoreConfigV2rayService(context).GenerateClientSpeedtestConfig(selecteds);
        await result.Success.Should().BeTrue();
        await selecteds.First().Port.Should().BeGreaterThan(0);
        await selecteds.First().AllowTest.Should().BeTrue();
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
