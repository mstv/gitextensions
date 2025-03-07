using GitExtensions.UITests;

[SetUpFixture]
public class GlobalSetupOnce
{
    [OneTimeSetUp]
    public async Task RunBeforeAnyTests()
    {
        await UITest.TraceAction(() =>
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            return Task.CompletedTask;
        });
    }

    [OneTimeTearDown]
    public Task RunAfterAnyTests()
        => UITest.TraceAction(() => Task.CompletedTask);
}
