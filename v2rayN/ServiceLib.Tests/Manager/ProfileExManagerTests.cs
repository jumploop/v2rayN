namespace ServiceLib.Tests.Manager;

public class ProfileExManagerTests
{
    [Test]
    public async Task SetTestMessage_ConcurrentCalls_ShouldNotCorruptQueue()
    {
        SQLiteHelper.Instance.CreateTable<ProfileExItem>();

        const int count = 256;
        var tasks = Enumerable.Range(0, count).Select(i => Task.Run(() =>
        {
            ProfileExManager.Instance.SetTestMessage($"id-{i}", "busy");
        })).ToArray();

        await Task.WhenAll(tasks);
        var queueCount = GetQueueCount();
        await queueCount.Should().BeEqualTo(count);
        await ProfileExManager.Instance.SaveTo();
    }

    private static int GetQueueCount()
    {
        var field = typeof(ProfileExManager).GetField("_queIndexIds", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var queue = (Queue<string>)field.GetValue(ProfileExManager.Instance)!;
        return queue.Count;
    }
}
