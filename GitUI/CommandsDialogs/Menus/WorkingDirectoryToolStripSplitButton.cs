#nullable enable

using GitCommands;
using GitCommands.UserRepositoryHistory;
using GitUI.CommandsDialogs.BrowseDialog;
using ResourceManager;

namespace GitUI.CommandsDialogs.Menus
{
    /// <summary>
    ///  Represents a split button that contains the recent repositories.
    /// </summary>
    internal class WorkingDirectoryToolStripSplitButton : ToolStripSplitButton, ITranslate
    {
        private static readonly TranslationString _noWorkingFolderText = new("No working directory");
        private static readonly TranslationString _configureWorkingDirMenu = new("&Configure this menu");
        private static readonly TranslationString _repositorySearchPlaceholder = new("Search repositories...");

        private class Implementation
        {
            // This is used as Tag in order to mark controls which are to be excluded from the filtering considerations.
            private static readonly object _excludeFromFilterMarker = new();

            /// <summary>
            ///  Gets the current instance of the git module.
            /// </summary>
            private GitModule Module => UICommands.Module;

            /// <summary>
            ///  Gets the active or the first open form.
            /// </summary>
            private Form? ActiveOrOpenForm
                => Form.ActiveForm ?? (Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null);

            /// <summary>
            ///  Gets the current instance of the UI commands.
            /// </summary>
            private GitUICommands UICommands => _getUICommands();

            private readonly Func<GitUICommands> _getUICommands;

            /// <summary>
            ///  The current instance of the <see cref="RepositoryHistoryUIService"/>.
            /// </summary>
            private readonly RepositoryHistoryUIService _repositoryHistoryUIService;

            private readonly ToolStripMenuItem _tsmiCategorisedRepos;
            private readonly ToolStripMenuItem _tsmiOpenLocalRepository;
            private readonly ToolStripMenuItem _tsmiCloseRepo;
            private readonly ToolStripMenuItem _tsmiRecentReposSettings;
            private readonly ToolStripTextBox _txtFilter = new();

            // NOTE: This is pretty bad, but we want to share the same look and feel of the menu items defined in the Start menu.
            private readonly StartToolStripMenuItem _startToolStripMenuItem;
            private readonly ToolStripMenuItem _closeToolStripMenuItem;

            internal Implementation(
                ToolStripSplitButton button,
                Func<GitUICommands> getUICommands,
                RepositoryHistoryUIService repositoryHistoryUIService,
                StartToolStripMenuItem startToolStripMenuItem,
                ToolStripMenuItem closeToolStripMenuItem,
                Action refreshContent)
            {
                button.ButtonClick += (s, e) => button.ShowDropDown();
                button.DropDownOpening += (s, e) => FillDropDown(button);
                button.MouseUp += MouseUpHandler;

                _getUICommands = getUICommands;
                _repositoryHistoryUIService = repositoryHistoryUIService;
                _startToolStripMenuItem = startToolStripMenuItem;
                _closeToolStripMenuItem = closeToolStripMenuItem;

                // Even 20 char filter is excessive, but we'll set it at this.
                // Show a compelling use case to increase.
                _txtFilter.MaxLength = 20;

                _txtFilter.Size = new Size(250, 23);
                _txtFilter.Tag = _excludeFromFilterMarker;

                TextBox filterTextbox = _txtFilter.TextBox;
                filterTextbox.PlaceholderText = _repositorySearchPlaceholder.Text;
                filterTextbox.TextChanged += (s, e) =>
                {
                    if (_txtFilter.GetCurrentParent() is null)
                    {
                        // We are clearing the textbox while opening the dropdown
                        return;
                    }

                    // Default items include:
                    //  1. filter
                    //  2. separator
                    //  3. favourite items
                    //      ... recent items
                    //  4. "Open repo..."
                    //  5. "Close repo..."
                    //  6. separator
                    //  7. "Configure menu"
                    const int defaultItemCount = 7;
                    if (button.DropDown.Items.Count <= defaultItemCount)
                    {
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(filterTextbox.Text))
                    {
                        foreach (ToolStripItem item in button.DropDown.Items)
                        {
                            item.Visible = true;
                        }

                        return;
                    }

                    foreach (ToolStripItem item in button.DropDown.Items)
                    {
                        if (item is ToolStripSeparator || item.Tag == _excludeFromFilterMarker)
                        {
                            continue;
                        }

                        item.Visible = item.Text.Contains(filterTextbox.Text, StringComparison.CurrentCultureIgnoreCase);
                    }
                };

                // Initilize toolstip menu items
                // ----------------------------------------
                _tsmiCategorisedRepos = new(_startToolStripMenuItem.FavouriteRepositoriesMenuItem.Text, _startToolStripMenuItem.FavouriteRepositoriesMenuItem.Image)
                {
                    Tag = _excludeFromFilterMarker
                };

                _tsmiOpenLocalRepository = new(_startToolStripMenuItem.OpenRepositoryMenuItem.Text, _startToolStripMenuItem.OpenRepositoryMenuItem.Image)
                {
                    ShortcutKeys = _startToolStripMenuItem.OpenRepositoryMenuItem.ShortcutKeys,
                    Tag = _excludeFromFilterMarker
                };
                _tsmiOpenLocalRepository.Click += (s, e) => _startToolStripMenuItem.OpenRepositoryMenuItem.PerformClick();

                _tsmiCloseRepo = new(_closeToolStripMenuItem.Text, _closeToolStripMenuItem.Image)
                {
                    Tag = _excludeFromFilterMarker
                };
                _tsmiCloseRepo.Click += (hs, he) => _closeToolStripMenuItem.PerformClick();

                _tsmiRecentReposSettings = new(_configureWorkingDirMenu.Text)
                {
                    Tag = _excludeFromFilterMarker
                };
                _tsmiRecentReposSettings.Click += (hs, he) =>
                {
                    using (FormRecentReposSettings frm = new())
                    {
                        frm.ShowDialog(ActiveOrOpenForm);
                    }

                    refreshContent();
                };
            }

            private void FillDropDown(ToolStripDropDownItem button)
            {
                button.DropDown.SuspendLayout();
                try
                {
                    button.DropDown.Items.Clear();

                    _txtFilter.Text = string.Empty;

                    button.DropDown.Items.Add(_txtFilter);
                    button.DropDown.Items.Add(new ToolStripSeparator());

                    _repositoryHistoryUIService.PopulateFavouriteRepositoriesMenu(_tsmiCategorisedRepos);
                    if (_tsmiCategorisedRepos.DropDownItems.Count > 0)
                    {
                        button.DropDown.Items.Add(_tsmiCategorisedRepos);
                    }

                    _repositoryHistoryUIService.PopulateRecentRepositoriesMenu(button);

                    button.DropDown.Items.Add(new ToolStripSeparator());
                    button.DropDown.Items.Add(_tsmiOpenLocalRepository);
                    button.DropDown.Items.Add(_tsmiCloseRepo);
                    button.DropDown.Items.Add(new ToolStripSeparator());
                    button.DropDown.Items.Add(_tsmiRecentReposSettings);
                }
                finally
                {
                    button.DropDown.ResumeLayout();
                }
            }

            private void MouseUpHandler(object? sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Right)
                {
                    _startToolStripMenuItem.OpenRepositoryMenuItem.PerformClick();
                }
            }

            internal void RefreshContent(ToolStripSplitButton button)
            {
                Form? graphicsForm = ActiveOrOpenForm;
                if (graphicsForm is null)
                {
                    // The component is unparented, no point doing anything.
                    return;
                }

                string path = Module.WorkingDir;

                // It appears at times Module.WorkingDir path is an empty string,
                // this caused issues like https://github.com/gitextensions/gitextensions/issues/4874.
                if (string.IsNullOrWhiteSpace(path))
                {
                    button.Text = _noWorkingFolderText.Text;
                    return;
                }

                IList<Repository> recentRepositoryHistory = ThreadHelper.JoinableTaskFactory.Run(
                    () => RepositoryHistoryManager.Locals.AddAsMostRecentAsync(path));

                List<RecentRepoInfo> pinnedRepos = new();
                using Graphics graphics = graphicsForm.CreateGraphics();
                RecentRepoSplitter splitter = new()
                {
                    MeasureFont = button.Font,
                    Graphics = graphics
                };

                splitter.SplitRecentRepos(recentRepositoryHistory, pinnedRepos, pinnedRepos);

                RecentRepoInfo? ri = pinnedRepos.Find(e => e.Repo.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase));

                button.Text = PathUtil.GetDisplayPath(ri?.Caption ?? path);

                if (AppSettings.RecentReposComboMinWidth > 0)
                {
                    button.AutoSize = false;
                    float captionWidth = graphics.MeasureString(button.Text, button.Font).Width;
                    captionWidth = captionWidth + button.DropDownButtonWidth + 5;
                    button.Width = Math.Max(AppSettings.RecentReposComboMinWidth, (int)captionWidth);
                }
                else
                {
                    button.AutoSize = true;
                }
            }
        }

        private Implementation? _implementation;

        internal WorkingDirectoryToolStripSplitButton()
        {
            Name = nameof(WorkingDirectoryToolStripSplitButton);

            Image = Properties.Resources.RepoOpen;
            ImageAlign = ContentAlignment.MiddleLeft;
            ImageTransparentColor = Color.Magenta;
            TextAlign = ContentAlignment.MiddleLeft;
        }

        /// <summary>
        ///  Initializes the menu item.
        /// </summary>
        /// <param name="getUICommands">The method that returns the current instance of UI commands.</param>
        internal void Initialize(Func<GitUICommands> getUICommands, RepositoryHistoryUIService repositoryHistoryUIService,
                               StartToolStripMenuItem startToolStripMenuItem, ToolStripMenuItem closeToolStripMenuItem)
        {
            Translator.Translate(this, AppSettings.CurrentTranslation);

            _implementation = new Implementation(this, getUICommands, repositoryHistoryUIService, startToolStripMenuItem, closeToolStripMenuItem, RefreshContent);
        }

        /// <summary>Updates the text shown on the combo button itself.</summary>
        internal void RefreshContent()
        {
            if (_implementation is null)
            {
                // The component is not initialized, no point doing anything.
                return;
            }

            _implementation.RefreshContent(this);
        }

        void ITranslate.AddTranslationItems(ITranslation translation)
        {
            TranslationUtils.AddTranslationItemsFromFields("FormBrowse", this, translation);
        }

        void ITranslate.TranslateItems(ITranslation translation)
        {
            TranslationUtils.TranslateItemsFromFields("FormBrowse", this, translation);
        }
    }
}
