using GitExtensions.UITests;

namespace UITests
{
    [TestFixture]
    public sealed class ATests
    {
        [OneTimeSetUp]
        public Task OnTimeSetUp() => UITest.DummyAction();

        [SetUp]
        public Task SetUp() => UITest.DummyAction();

        [TearDown]
        public Task TearDown() => UITest.DummyAction();

        [OneTimeTearDown]
        public Task OneTimeTearDown() => UITest.DummyAction();

        [Test]
        public Task TestA1() => UITest.DummyAction();

        [Test]
        public Task TestA2() => UITest.DummyAction();
    }
}
