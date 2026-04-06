using System.ComponentModel;
using GitCommands;
using GitUI.ScriptsEngine;

namespace GitExtensions.UITests.ScriptEngine;

[TestFixture]
public class ScriptManagerTests
{
    [Test]
    public async Task Can_save_settings()
    {
        string? originalScripts = AppSettings.OwnScripts.Value;

        try
        {
            ScriptsManager scriptsManager = new();

            AppSettings.OwnScripts.Value = "<ArrayOfScriptInfo />";

            BindingList<ScriptInfo> scripts = scriptsManager.GetScripts();

            scripts.Add(new ScriptInfo()
            {
                Name = "name",
                Command = "cmd",
                Arguments = "args"
            });

            string? xml = scriptsManager.SerializeIntoXml();

            // Verify as a string, as the xml verifier ignores line breaks.
            await Verifier.VerifyXml(xml);
        }
        finally
        {
            AppSettings.OwnScripts.Value = originalScripts;
        }
    }
}
