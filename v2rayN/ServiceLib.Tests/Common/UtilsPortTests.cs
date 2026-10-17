namespace ServiceLib.Tests.Common;

public class UtilsPortTests
{
    [Test]
    public async Task GetFreePort_WithZeroOrNegative_ShouldReturnValidPort()
    {
        var portZero = Utils.GetFreePort(0);
        await portZero.Should().BeGreaterThan(0);
        await portZero.Should().BeLessThan(Global.MaxPort);

        var portNegative = Utils.GetFreePort(-1);
        await portNegative.Should().BeGreaterThan(0);
        await portNegative.Should().BeLessThan(Global.MaxPort);
    }

    [Test]
    public async Task GetFreePort_WithAvailablePort_ShouldReturnPortWithinRange()
    {
        var port = Utils.GetFreePort(12345);
        await port.Should().BeGreaterThan(0);
        await port.Should().BeLessThan(Global.MaxPort);
    }
}
