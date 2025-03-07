using System.Diagnostics;
using GitCommands;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace CommonTestUtils
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class TestAppSettingsAttribute : Attribute, ITestAction
    {
        public ActionTargets Targets => ActionTargets.Suite;

        public void BeforeTest(ITest test)
        {
            Trace.WriteLine($"enter {nameof(TestAppSettingsAttribute)}.{nameof(BeforeTest)} {AppSettings.SettingsContainer.SettingsCache.SettingsFilePath}");

            File.Delete(AppSettings.SettingsContainer.SettingsCache.SettingsFilePath);

            AppSettings.CheckForUpdates = false;
            AppSettings.ShowAvailableDiffTools = false;

            Trace.WriteLine($"exit  {nameof(TestAppSettingsAttribute)}.{nameof(BeforeTest)}");
        }

        public void AfterTest(ITest test)
        {
            Trace.WriteLine($"exit  {nameof(TestAppSettingsAttribute)}.{nameof(AfterTest)}");
        }
    }
}
