using GitCommands;
using GitCommands.Settings;

namespace GitUI.Script
{
    public interface IScriptsManager
    {
        ScriptInfo? GetScript(int scriptId);

        IReadOnlyList<ScriptInfo> GetScripts();

        void Initialize(DistributedSettings settings);

        bool RunEventScripts(GitModuleForm form, ScriptEvent scriptEvent);

        CommandStatus RunScript(int scriptId, GitModuleForm form, RevisionGridControl? revisionGrid = null);
    }
}
