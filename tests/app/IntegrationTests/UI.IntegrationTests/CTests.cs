using GitExtensions.UITests;

namespace UITests
{
    [TestFixture]
    public sealed class CTests
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
        public Task TestC1() => UITest.DummyAction();

        [Test]
        public Task TestC2() => UITest.DummyAction();

        [Test]
        public Task TestC3() => UITest.DummyAction();
    }
}
