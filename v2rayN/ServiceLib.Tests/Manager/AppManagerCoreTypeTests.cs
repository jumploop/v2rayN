using ServiceLib.Tests.CoreConfig;
using ServiceLib.ViewModels;

namespace ServiceLib.Tests.Manager;

[NotInParallel]
public class AppManagerCoreTypeTests
{
    [Test]
    public async Task GetCoreType_SingboxOnlyProtocols_ShouldAlwaysReturnSingbox()
    {
        var config = new Config
        {
            CoreTypeItem =
            [
                new CoreTypeItem { ConfigType = EConfigType.TUIC, CoreType = ECoreType.Xray },
                new CoreTypeItem { ConfigType = EConfigType.Anytls, CoreType = ECoreType.Xray },
                new CoreTypeItem { ConfigType = EConfigType.Naive, CoreType = ECoreType.Xray },
            ]
        };

        CoreConfigTestFactory.BindAppManagerConfig(config);

        var tuicNode = new ProfileItem { ConfigType = EConfigType.TUIC, CoreType = ECoreType.Xray };
        var anytlsNode = new ProfileItem { ConfigType = EConfigType.Anytls, CoreType = ECoreType.Xray };
        var naiveNode = new ProfileItem { ConfigType = EConfigType.Naive, CoreType = ECoreType.Xray };

        await AppManager.Instance.GetCoreType(tuicNode, EConfigType.TUIC).Should().BeEqualTo(ECoreType.sing_box);
        await AppManager.Instance.GetCoreType(anytlsNode, EConfigType.Anytls).Should().BeEqualTo(ECoreType.sing_box);
        await AppManager.Instance.GetCoreType(naiveNode, EConfigType.Naive).Should().BeEqualTo(ECoreType.sing_box);

        await AppManager.Instance.GetCoreType(null, EConfigType.TUIC).Should().BeEqualTo(ECoreType.sing_box);
        await AppManager.Instance.GetCoreType(null, EConfigType.Anytls).Should().BeEqualTo(ECoreType.sing_box);
        await AppManager.Instance.GetCoreType(null, EConfigType.Naive).Should().BeEqualTo(ECoreType.sing_box);
    }

    [Test]
    public async Task GetCoreType_XrayProtocols_ShouldRespectConfiguration()
    {
        var config = new Config
        {
            CoreTypeItem =
            [
                new CoreTypeItem { ConfigType = EConfigType.VMess, CoreType = ECoreType.Xray },
                new CoreTypeItem { ConfigType = EConfigType.VLESS, CoreType = ECoreType.sing_box },
            ]
        };

        CoreConfigTestFactory.BindAppManagerConfig(config);

        var vmessNode = new ProfileItem { ConfigType = EConfigType.VMess };
        var vlessNode = new ProfileItem { ConfigType = EConfigType.VLESS };

        await AppManager.Instance.GetCoreType(vmessNode, EConfigType.VMess).Should().BeEqualTo(ECoreType.Xray);
        await AppManager.Instance.GetCoreType(vlessNode, EConfigType.VLESS).Should().BeEqualTo(ECoreType.sing_box);
    }

    [Test]
    public async Task InitCoreType_SingboxOnlyProtocols_ShouldMigrateToSingbox()
    {
        var config = new Config
        {
            Inbound = [new InItem { LocalPort = 10808 }],
            CoreBasicItem = new(),
            UiItem = new(),
            ConstItem = new(),
            SpeedTestItem = new(),
            SystemProxyItem = new(),
            TunModeItem = new(),
            GuiItem = new(),
            Mux4RayItem = new(),
            Mux4SboxItem = new(),
            HysteriaItem = new(),
            CoreTypeItem =
            [
                new CoreTypeItem { ConfigType = EConfigType.TUIC, CoreType = ECoreType.Xray },
                new CoreTypeItem { ConfigType = EConfigType.Anytls, CoreType = ECoreType.Xray },
                new CoreTypeItem { ConfigType = EConfigType.Naive, CoreType = ECoreType.Xray },
                new CoreTypeItem { ConfigType = EConfigType.VMess, CoreType = ECoreType.Xray },
            ]
        };

        CoreConfigTestFactory.BindAppManagerConfig(config);

        var vm = new OptionSettingViewModel();
        var initCoreTypeMethod = typeof(OptionSettingViewModel).GetMethod("InitCoreType", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await (Task)initCoreTypeMethod.Invoke(vm, null)!;

        var tuicItem = config.CoreTypeItem.FirstOrDefault(t => t.ConfigType == EConfigType.TUIC);
        var anytlsItem = config.CoreTypeItem.FirstOrDefault(t => t.ConfigType == EConfigType.Anytls);
        var naiveItem = config.CoreTypeItem.FirstOrDefault(t => t.ConfigType == EConfigType.Naive);
        var vmessItem = config.CoreTypeItem.FirstOrDefault(t => t.ConfigType == EConfigType.VMess);

        await tuicItem!.CoreType.Should().BeEqualTo(ECoreType.sing_box);
        await anytlsItem!.CoreType.Should().BeEqualTo(ECoreType.sing_box);
        await naiveItem!.CoreType.Should().BeEqualTo(ECoreType.sing_box);
        await vmessItem!.CoreType.Should().BeEqualTo(ECoreType.Xray);
    }
}
