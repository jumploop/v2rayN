namespace ServiceLib.Tests.Manager;

[NotInParallel]
public class ProfileExManagerTests
{
    [Test]
    public async Task SetTestMessage_ConcurrentCalls_ShouldNotCorruptQueue()
    {
        SQLiteHelper.Instance.CreateTable<ProfileExItem>();
        await ProfileExManager.Instance.ClearAll();

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

    [Test]
    public async Task SaveTo_ConcurrentCalls_ShouldNotThrowQueueEmpty()
    {
        SQLiteHelper.Instance.CreateTable<ProfileExItem>();
        await ProfileExManager.Instance.ClearAll();

        for (int i = 0; i < 50; i++)
        {
            ProfileExManager.Instance.SetTestMessage($"concurrent-save-{i}", "ok");
        }

        var saveTasks = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            await ProfileExManager.Instance.SaveTo();
        })).ToArray();

        await Task.WhenAll(saveTasks);
        var queueCount = GetQueueCount();
        await queueCount.Should().BeEqualTo(0);
    }

    [Test]
    public async Task ClearAll_ShouldClearQueue()
    {
        SQLiteHelper.Instance.CreateTable<ProfileExItem>();
        ProfileExManager.Instance.SetTestMessage("clear-test-1", "msg");
        await GetQueueCount().Should().BeGreaterThan(0);

        await ProfileExManager.Instance.ClearAll();
        await GetQueueCount().Should().BeEqualTo(0);
    }

    private static int GetQueueCount()
    {
        var field = typeof(ProfileExManager).GetField("_queIndexIds", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var queue = (Queue<string>)field.GetValue(ProfileExManager.Instance)!;
        return queue.Count;
    }
}
