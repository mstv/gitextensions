using System.IO;
using GitUIPluginInterfaces;
            // TODO produce a valid working directory
            var module = new GitModule(Path.GetTempPath());
                var fetchCmd = module.FetchCmd("origin", "some-branch", "local").Arguments;
                var fetchCmd = module.FetchCmd("origin", "some-branch", "local", true).Arguments;
                var fetchCmd = module.FetchCmd("https://host.com/repo", "some-branch", "local").Arguments;
                var fetchCmd = module.FetchCmd("https://host.com/repo", "some-branch", null).Arguments;
                var fetchCmd = module.FetchCmd("origin", "", "local").Arguments;
                var fetchCmd = module.PullCmd("origin", "some-branch", false).Arguments;
                var fetchCmd = module.PullCmd("https://host.com/repo", "some-branch", false).Arguments;
                var fetchCmd = module.PullCmd("origin", "some-branch", true).Arguments;
        public void TestGetDiffChangedFilesFromString()
            // TODO produce a valid working directory
            var module = new GitModule(Path.GetTempPath());
                var status = GitCommandHelpers.GetDiffChangedFilesFromString(module, statusString, "HEAD", GitRevision.IndexGuid, "HEAD");
                var status = GitCommandHelpers.GetDiffChangedFilesFromString(module, statusString, "HEAD", GitRevision.IndexGuid, "HEAD");
                var status = GitCommandHelpers.GetDiffChangedFilesFromString(module, statusString, "HEAD", GitRevision.IndexGuid, "HEAD");
                var status = GitCommandHelpers.GetDiffChangedFilesFromString(module, statusString, "HEAD", GitRevision.IndexGuid, "HEAD");
                // git diff -M -C -z --cached --name-status
                // Ignore unmerged (in conflict) if revision is work tree
                string statusString = "M  testfile.txt\0U  testfile.txt\0";
                var status = GitCommandHelpers.GetDiffChangedFilesFromString(module, statusString, GitRevision.IndexGuid, GitRevision.WorkTreeGuid, GitRevision.IndexGuid);
                Assert.IsTrue(status.Count == 1);
                Assert.IsTrue(status[0].Name == "testfile.txt");
                Assert.IsTrue(status[0].Staged == StagedStatus.WorkTree);
            }

            {
                // git diff -M -C -z --cached --name-status
                // Include unmerged (in conflict) if revision is index
                string statusString = "M  testfile.txt\0U  testfile2.txt\0";
                var status = GitCommandHelpers.GetDiffChangedFilesFromString(module, statusString, "HEAD", GitRevision.IndexGuid, "HEAD");
                Assert.IsTrue(status[0].Name == "testfile.txt");
                Assert.IsTrue(status[0].Staged == StagedStatus.Index);
            {
                // git diff -M -C -z --name-status 123 456
                // Check that the staged status is None if not Index/WorkTree
                string statusString = "M  testfile.txt\0U  testfile2.txt\0";
                var status = GitCommandHelpers.GetDiffChangedFilesFromString(module, statusString, GitRevision.IndexGuid, "456", "678");
                Assert.IsTrue(status.Count == 2);
                Assert.IsTrue(status[0].Name == "testfile.txt");
                Assert.IsTrue(status[0].Staged == StagedStatus.None);
            }
            {
                // git diff -M -C -z --name-status 123 456
                // Check that the staged status is None if not Index/WorkTree
                string statusString = "M  testfile.txt\0U  testfile2.txt\0";
                var status = GitCommandHelpers.GetDiffChangedFilesFromString(module, statusString, "123", "456", null);
                Assert.IsTrue(status.Count == 2);
                Assert.IsTrue(status[0].Name == "testfile.txt");
                Assert.IsTrue(status[0].Staged == StagedStatus.None);
            }
#if !DEBUG && false
            // This test is for documentation, but as the throw is in a called function, it will not test cleanly
            {
                // git diff -M -C -z --name-status 123 456
                // Check that the staged status is None if not Index/WorkTree
                // Assertion in Debug, throws in Release
                string statusString = "M  testfile.txt\0U  testfile2.txt\0";

                var status = GitCommandHelpers.GetDiffChangedFilesFromString(module, statusString, null, null, null);
                Assert.IsTrue(status.Count == 2);
                Assert.IsTrue(status[0].Name == "testfile.txt");
                Assert.IsTrue(status[0].Staged == StagedStatus.Unknown);
             }
#endif
        public void TestGetStatusChangedFilesFromString()
            // TODO produce a valid working directory
            var module = new GitModule(Path.GetTempPath());
            {
                // git status --porcelain=2 --untracked-files=no -z
                // porcelain v1: string statusString = "M  adfs.h\0M  dir.c\0";
                string statusString = "#Header\03 unknown info\01 .M S..U 160000 160000 160000 cbca134e29be13b35f21ca4553ba04f796324b1c cbca134e29be13b35f21ca4553ba04f796324b1c adfs.h\01 .M SCM. 160000 160000 160000 6bd3b036fc5718a51a0d27cde134c7019798c3ce 6bd3b036fc5718a51a0d27cde134c7019798c3ce dir.c\0\r\nwarning: LF will be replaced by CRLF in adfs.h.\nThe file will have its original line endings in your working directory.\nwarning: LF will be replaced by CRLF in dir.c.\nThe file will have its original line endings in your working directory.";
                var status = GitCommandHelpers.GetStatusChangedFilesFromString(module, statusString);
                Assert.IsTrue(status.Count == 2);
                Assert.IsTrue(status[0].Name == "adfs.h");
                Assert.IsTrue(status[1].Name == "dir.c");
            }
            {
                // git status --porcelain=2 --untracked-files -z
                // porcelain v1: string statusString = "M  adfs.h\0?? untracked_file\0";
                string statusString = "1 .M S..U 160000 160000 160000 cbca134e29be13b35f21ca4553ba04f796324b1c cbca134e29be13b35f21ca4553ba04f796324b1c adfs.h\0? untracked_file\0";
                var status = GitCommandHelpers.GetStatusChangedFilesFromString(module, statusString);
                Assert.IsTrue(status.Count == 2);
                Assert.IsTrue(status[0].Name == "adfs.h");
                Assert.IsTrue(status[1].Name == "untracked_file");
            }
            {
                // git status --porcelain=2 --ignored-files -z
                // porcelain v1: string statusString = ".M  adfs.h\0!! ignored_file\0";
                string statusString = "1 .M S..U 160000 160000 160000 cbca134e29be13b35f21ca4553ba04f796324b1c cbca134e29be13b35f21ca4553ba04f796324b1c adfs.h\0! ignored_file\0";
                var status = GitCommandHelpers.GetStatusChangedFilesFromString(module, statusString);
                Assert.IsTrue(status.Count == 2);
                Assert.IsTrue(status[0].Name == "adfs.h");
                Assert.IsTrue(status[1].Name == "ignored_file");
            }
            // TODO produce a valid working directory
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            // Create actual working directories so that Process.Start doesn't throw Win32Exception due to an invalid path
            Directory.CreateDirectory(Path.Combine(root, "Externals", "conemu-inside"));
            Directory.CreateDirectory(Path.Combine(root, "Externals", "conemu-inside-a"));
            Directory.CreateDirectory(Path.Combine(root, "Externals", "conemu-inside-b"));
            Directory.CreateDirectory(Path.Combine(root, "Assets", "Core", "Vehicle Physics core assets"));

            var testModule = new GitModule(root);
            var status = GitCommandHelpers.ParseSubmoduleStatus(text, testModule, fileName);
            Assert.AreEqual(ObjectId.Parse("b5a3d51777c85a9aeee534c382b5ccbb86b485d3"), status.Commit);
            Assert.AreEqual(fileName, status.Name);
            Assert.AreEqual(ObjectId.Parse("a17ea0c8ebe9d8cd7e634ba44559adffe633c11d"), status.OldCommit);
            Assert.AreEqual(fileName, status.OldName);
            status = GitCommandHelpers.ParseSubmoduleStatus(text, testModule, fileName);
            Assert.AreEqual(ObjectId.Parse("0cc457d030e92f804569407c7cd39893320f9740"), status.Commit);
            Assert.AreEqual(fileName, status.Name);
            Assert.AreEqual(ObjectId.Parse("2fb88514cfdc37a2708c24f71eca71c424b8d402"), status.OldCommit);
            Assert.AreEqual(fileName, status.OldName);
            status = GitCommandHelpers.ParseSubmoduleStatus(text, testModule, fileName);

            Assert.AreEqual(ObjectId.Parse("b5a3d51777c85a9aeee534c382b5ccbb86b485d3"), status.Commit);
            Assert.AreEqual(fileName, status.Name);
            Assert.AreEqual(ObjectId.Parse("a17ea0c8ebe9d8cd7e634ba44559adffe633c11d"), status.OldCommit);
            Assert.AreEqual("Externals/conemu-inside-a", status.OldName);

            text = "diff --git a/Externals/ICSharpCode.TextEditor b/Externals/ICSharpCode.TextEditor\r\nnew file mode 160000\r\nindex 000000000..05321769f\r\n--- /dev/null\r\n+++ b/Externals/ICSharpCode.TextEditor\r\n@@ -0,0 +1 @@\r\n+Subproject commit 05321769f039f39fa7f6748e8f30d5c8f157c7dc\r\n";
            fileName = "Externals/ICSharpCode.TextEditor";

            status = GitCommandHelpers.ParseSubmoduleStatus(text, testModule, fileName);
            Assert.AreEqual(ObjectId.Parse("05321769f039f39fa7f6748e8f30d5c8f157c7dc"), status.Commit);
            Assert.AreEqual(fileName, status.Name);
            Assert.IsNull(status.OldCommit);
            Assert.AreEqual("Externals/ICSharpCode.TextEditor", status.OldName);

            try
            {
                // Clean up temporary folders
                Directory.Delete(root, recursive: true);
            }
            catch
            {
                // Ignore
            }
            Assert.AreEqual("submodule sync \"foo\"", GitCommandHelpers.SubmoduleSyncCmd("foo").Arguments);
            Assert.AreEqual("submodule sync", GitCommandHelpers.SubmoduleSyncCmd("").Arguments);
            Assert.AreEqual("submodule sync", GitCommandHelpers.SubmoduleSyncCmd(null).Arguments);
                GitCommandHelpers.AddSubmoduleCmd("remotepath", "localpath", "branch", force: false).Arguments);
                GitCommandHelpers.AddSubmoduleCmd("remotepath", "localpath", branch: null, force: false).Arguments);
                GitCommandHelpers.AddSubmoduleCmd("remotepath", "localpath", "branch", force: true).Arguments);
                GitCommandHelpers.AddSubmoduleCmd("remote\\path", "local\\path", "branch", force: true).Arguments);
            var commitId = ObjectId.Random();

                $"revert {commitId}",
                GitCommandHelpers.RevertCmd(commitId, autoCommit: true, parentIndex: 0).Arguments);
                $"revert --no-commit {commitId}",
                GitCommandHelpers.RevertCmd(commitId, autoCommit: false, parentIndex: 0).Arguments);
                $"revert -m 1 {commitId}",
                GitCommandHelpers.RevertCmd(commitId, autoCommit: true, parentIndex: 1).Arguments);
                GitCommandHelpers.CloneCmd("from", "to").Arguments);
                GitCommandHelpers.CloneCmd("from\\path", "to\\path").Arguments);
                GitCommandHelpers.CloneCmd("from", "to", central: true).Arguments);
                GitCommandHelpers.CloneCmd("from", "to", initSubmodules: true).Arguments);
                GitCommandHelpers.CloneCmd("from", "to", initSubmodules: true).Arguments);
                GitCommandHelpers.CloneCmd("from", "to", depth: 2).Arguments);
                GitCommandHelpers.CloneCmd("from", "to", isSingleBranch: true).Arguments);
                GitCommandHelpers.CloneCmd("from", "to", isSingleBranch: false).Arguments);
                GitCommandHelpers.CloneCmd("from", "to", branch: "branch").Arguments);
                GitCommandHelpers.CloneCmd("from", "to", branch: null).Arguments);
                GitCommandHelpers.CloneCmd("from", "to", lfs: true).Arguments);
                GitCommandHelpers.CheckoutCmd("branch", LocalChangesAction.DontChange).Arguments);
                GitCommandHelpers.CheckoutCmd("branch", LocalChangesAction.Merge).Arguments);
                GitCommandHelpers.CheckoutCmd("branch", LocalChangesAction.Reset).Arguments);
                GitCommandHelpers.CheckoutCmd("branch", LocalChangesAction.Stash).Arguments);
                GitCommandHelpers.RemoveCmd().Arguments);
                GitCommandHelpers.RemoveCmd(force: false).Arguments);
                GitCommandHelpers.RemoveCmd(isRecursive: false).Arguments);
                GitCommandHelpers.RemoveCmd(files: new[] { "a", "b", "c" }).Arguments);
                GitCommandHelpers.BranchCmd("branch", "revision", checkout: true).Arguments);
                GitCommandHelpers.BranchCmd("branch", "revision", checkout: false).Arguments);
                GitCommandHelpers.BranchCmd("branch", null, checkout: true).Arguments);
                GitCommandHelpers.BranchCmd("branch", "", checkout: true).Arguments);
                GitCommandHelpers.BranchCmd("branch", "  ", checkout: true).Arguments);
            Assert.True(GitVersion.Current.PushCanAskForProgress);
                GitCommandHelpers.PushTagCmd("path", "tag", all: false).Arguments);
                GitCommandHelpers.PushTagCmd("path", " tag ", all: false).Arguments);
                GitCommandHelpers.PushTagCmd("path\\path", " tag ", all: false).Arguments);
                GitCommandHelpers.PushTagCmd("path", "tag", all: true).Arguments);
                GitCommandHelpers.PushTagCmd("path", "tag", all: true, force: ForcePushOptions.Force).Arguments);
                GitCommandHelpers.PushTagCmd("path", "tag", all: true, force: ForcePushOptions.ForceWithLease).Arguments);
                GitCommandHelpers.PushTagCmd("path", "", all: false).Arguments);
            Assert.True(GitVersion.Current.StashUntrackedFilesSupported);
                GitCommandHelpers.StashSaveCmd(untracked: false, keepIndex: false, null, Array.Empty<string>()).Arguments);
                GitCommandHelpers.StashSaveCmd(untracked: false, keepIndex: false, null, null).Arguments);
                GitCommandHelpers.StashSaveCmd(untracked: true, keepIndex: false, null, null).Arguments);
                GitCommandHelpers.StashSaveCmd(untracked: false, keepIndex: true, null, null).Arguments);
                GitCommandHelpers.StashSaveCmd(untracked: false, keepIndex: true, null, null).Arguments);
                GitCommandHelpers.StashSaveCmd(untracked: false, keepIndex: false, "message", null).Arguments);
                GitCommandHelpers.StashSaveCmd(untracked: false, keepIndex: false, null, new[] { "a", "b" }).Arguments);
            var id1 = ObjectId.Random();
            var id2 = ObjectId.Random();

                GitCommandHelpers.ContinueBisectCmd(GitBisectOption.Good).Arguments);
                GitCommandHelpers.ContinueBisectCmd(GitBisectOption.Bad).Arguments);
                GitCommandHelpers.ContinueBisectCmd(GitBisectOption.Skip).Arguments);
                $"bisect good {id1} {id2}",
                GitCommandHelpers.ContinueBisectCmd(GitBisectOption.Good, id1, id2).Arguments);
                GitCommandHelpers.RebaseCmd("branch", interactive: false, preserveMerges: false, autosquash: false, autoStash: false).Arguments);
                GitCommandHelpers.RebaseCmd("branch", interactive: true, preserveMerges: false, autosquash: false, autoStash: false).Arguments);
                GitCommandHelpers.RebaseCmd("branch", interactive: false, preserveMerges: true, autosquash: false, autoStash: false).Arguments);
                GitCommandHelpers.RebaseCmd("branch", interactive: false, preserveMerges: false, autosquash: true, autoStash: false).Arguments);
                GitCommandHelpers.RebaseCmd("branch", interactive: false, preserveMerges: false, autosquash: false, autoStash: true).Arguments);
                GitCommandHelpers.RebaseCmd("branch", interactive: true, preserveMerges: false, autosquash: true, autoStash: false).Arguments);
                GitCommandHelpers.RebaseCmd("branch", interactive: true, preserveMerges: true, autosquash: true, autoStash: true).Arguments);
                GitCommandHelpers.RebaseCmd("branch", interactive: false, preserveMerges: false, autosquash: false, autoStash: false, "from", "onto").Arguments);
                GitCommandHelpers.CleanUpCmd(dryRun: false, directories: false, nonIgnored: true, ignored: false).Arguments);
                GitCommandHelpers.CleanUpCmd(dryRun: true, directories: false, nonIgnored: true, ignored: false).Arguments);
                GitCommandHelpers.CleanUpCmd(dryRun: false, directories: true, nonIgnored: true, ignored: false).Arguments);
                GitCommandHelpers.CleanUpCmd(dryRun: false, directories: false, nonIgnored: false, ignored: false).Arguments);
                GitCommandHelpers.CleanUpCmd(dryRun: false, directories: false, nonIgnored: true, ignored: true).Arguments);
                GitCommandHelpers.CleanUpCmd(dryRun: false, directories: false, nonIgnored: false, ignored: true).Arguments);
                GitCommandHelpers.CleanUpCmd(dryRun: false, directories: false, nonIgnored: true, ignored: false, "paths").Arguments);
                "status --porcelain=2 -z --untracked-files --ignore-submodules",
                GitCommandHelpers.GetAllChangedFilesCmd(excludeIgnoredFiles: true, UntrackedFilesMode.Default, IgnoreSubmodulesMode.Default).Arguments);
                "status --porcelain=2 -z --untracked-files --ignore-submodules --ignored",
                GitCommandHelpers.GetAllChangedFilesCmd(excludeIgnoredFiles: false, UntrackedFilesMode.Default, IgnoreSubmodulesMode.Default).Arguments);
                "status --porcelain=2 -z --untracked-files=no --ignore-submodules",
                GitCommandHelpers.GetAllChangedFilesCmd(excludeIgnoredFiles: true, UntrackedFilesMode.No, IgnoreSubmodulesMode.Default).Arguments);
                "status --porcelain=2 -z --untracked-files=normal --ignore-submodules",
                GitCommandHelpers.GetAllChangedFilesCmd(excludeIgnoredFiles: true, UntrackedFilesMode.Normal, IgnoreSubmodulesMode.Default).Arguments);
                "status --porcelain=2 -z --untracked-files=all --ignore-submodules",
                GitCommandHelpers.GetAllChangedFilesCmd(excludeIgnoredFiles: true, UntrackedFilesMode.All, IgnoreSubmodulesMode.Default).Arguments);
                "status --porcelain=2 -z --untracked-files --ignore-submodules=none",
                GitCommandHelpers.GetAllChangedFilesCmd(excludeIgnoredFiles: true, UntrackedFilesMode.Default, IgnoreSubmodulesMode.None).Arguments);
                "status --porcelain=2 -z --untracked-files --ignore-submodules=none",
                GitCommandHelpers.GetAllChangedFilesCmd(excludeIgnoredFiles: true, UntrackedFilesMode.Default).Arguments);
                "status --porcelain=2 -z --untracked-files --ignore-submodules=untracked",
                GitCommandHelpers.GetAllChangedFilesCmd(excludeIgnoredFiles: true, UntrackedFilesMode.Default, IgnoreSubmodulesMode.Untracked).Arguments);
                "status --porcelain=2 -z --untracked-files --ignore-submodules=dirty",
                GitCommandHelpers.GetAllChangedFilesCmd(excludeIgnoredFiles: true, UntrackedFilesMode.Default, IgnoreSubmodulesMode.Dirty).Arguments);
                "status --porcelain=2 -z --untracked-files --ignore-submodules=all",
                GitCommandHelpers.GetAllChangedFilesCmd(excludeIgnoredFiles: true, UntrackedFilesMode.Default, IgnoreSubmodulesMode.All).Arguments);
                "--no-optional-locks status --porcelain=2 -z --untracked-files --ignore-submodules",
                GitCommandHelpers.GetAllChangedFilesCmd(excludeIgnoredFiles: true, UntrackedFilesMode.Default, IgnoreSubmodulesMode.Default, noLocks: true).Arguments);
                GitCommandHelpers.MergeBranchCmd("branch", allowFastForward: true, squash: false, noCommit: false, allowUnrelatedHistories: false, message: null, log: null, strategy: null).Arguments);
                GitCommandHelpers.MergeBranchCmd("branch", allowFastForward: false, squash: false, noCommit: false, allowUnrelatedHistories: false, message: null, log: null, strategy: null).Arguments);
                GitCommandHelpers.MergeBranchCmd("branch", allowFastForward: true, squash: true, noCommit: false, allowUnrelatedHistories: false, message: null, log: null, strategy: null).Arguments);
                GitCommandHelpers.MergeBranchCmd("branch", allowFastForward: true, squash: false, noCommit: true, allowUnrelatedHistories: false, message: null, log: null, strategy: null).Arguments);
                GitCommandHelpers.MergeBranchCmd("branch", allowFastForward: true, squash: false, noCommit: false, allowUnrelatedHistories: true, message: null, log: null, strategy: null).Arguments);
                GitCommandHelpers.MergeBranchCmd("branch", allowFastForward: true, squash: false, noCommit: false, allowUnrelatedHistories: false, message: "message", log: null, strategy: null).Arguments);
                GitCommandHelpers.MergeBranchCmd("branch", allowFastForward: true, squash: false, noCommit: false, allowUnrelatedHistories: false, message: null, log: 0, strategy: null).Arguments);
                GitCommandHelpers.MergeBranchCmd("branch", allowFastForward: true, squash: false, noCommit: false, allowUnrelatedHistories: false, message: null, log: null, strategy: "strategy").Arguments);
                GitCommandHelpers.MergeBranchCmd("branch", allowFastForward: false, squash: true, noCommit: true, allowUnrelatedHistories: true, message: "message", log: 1, strategy: "strategy").Arguments);
                GitCommandHelpers.ApplyDiffPatchCmd(false, "hello\\world.patch").Arguments);
                GitCommandHelpers.ApplyDiffPatchCmd(true, "hello\\world.patch").Arguments);
                GitCommandHelpers.ApplyMailboxPatchCmd(false, "hello\\world.patch").Arguments);
                GitCommandHelpers.ApplyMailboxPatchCmd(true, "hello\\world.patch").Arguments);
                GitCommandHelpers.ApplyMailboxPatchCmd(true).Arguments);