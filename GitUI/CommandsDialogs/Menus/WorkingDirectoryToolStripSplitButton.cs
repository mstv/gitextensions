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

            private readonly Func<GitUICommands> _getUICommands;

            /// <summary>
            ///  Gets the current instance of the git module.
            /// </summary>
            internal GitModule Module => UICommands.Module;

            /// <summary>
            ///  Gets the form that is displaying the menu item.
            /// </summary>
            internal Form? OwnerForm => _txtFilter.Control.FindForm();

            /// <summary>
            ///  Gets the current instance of the UI commands.
            /// </summary>
            internal GitUICommands UICommands => _getUICommands();

            /// <summary>
            ///  Gets the current instance of the <see cref="RepositoryHistoryUIService"/>.
            /// </summary>
            internal readonly RepositoryHistoryUIService RepositoryHistoryUIService;

            internal readonly ToolStripMenuItem _tsmiCategorisedRepos;
            internal readonly ToolStripMenuItem _tsmiOpenLocalRepository;
            internal readonly ToolStripMenuItem _tsmiCloseRepo;
            internal readonly ToolStripMenuItem _tsmiRecentReposSettings;
            internal readonly ToolStripTextBox _txtFilter = new();

            // NOTE: This is pretty bad, but we want to share the same look and feel of the menu items defined in the Start menu.
            internal readonly StartToolStripMenuItem _startToolStripMenuItem;
            internal readonly ToolStripMenuItem _closeToolStripMenuItem;

            internal Implementation(
                ToolStripDropDown dropDown,
                Func<GitUICommands> getUICommands,
                RepositoryHistoryUIService repositoryHistoryUIService,
                StartToolStripMenuItem startToolStripMenuItem,
                ToolStripMenuItem closeToolStripMenuItem,
                Action refreshContent)
            {
                _getUICommands = getUICommands;
                RepositoryHistoryUIService = repositoryHistoryUIService;
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
                    if (dropDown.Items.Count <= defaultItemCount)
                    {
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(filterTextbox.Text))
                    {
                        foreach (ToolStripItem item in dropDown.Items)
                        {
                            item.Visible = true;
                        }

                        return;
                    }

                    foreach (ToolStripItem item in dropDown.Items)
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
                        frm.ShowDialog(OwnerForm);
                    }

                    refreshContent();
                };
            }
        }

        private Implementation? _use_property_instead_implementation;

        private Implementation _implementation
            => _use_property_instead_implementation ?? throw new InvalidOperationException("The button is not initialized");

        public WorkingDirectoryToolStripSplitButton()
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
        public void Initialize(Func<GitUICommands> getUICommands, RepositoryHistoryUIService repositoryHistoryUIService,
                               StartToolStripMenuItem startToolStripMenuItem, ToolStripMenuItem closeToolStripMenuItem)
        {
            Translator.Translate(this, AppSettings.CurrentTranslation);

            _use_property_instead_implementation = new Implementation(DropDown, getUICommands, repositoryHistoryUIService, startToolStripMenuItem, closeToolStripMenuItem, RefreshContent);
        }

        protected override void OnButtonClick(EventArgs e)
        {
            base.OnButtonClick(e);
            ShowDropDown();
        }

        protected override void OnDropDownShow(EventArgs e)
        {
            DropDown.SuspendLayout();
            DropDownItems.Clear();

            _implementation._txtFilter.Text = string.Empty;

            DropDownItems.Add(_implementation._txtFilter);
            DropDownItems.Add(new ToolStripSeparator());

            _implementation.RepositoryHistoryUIService.PopulateFavouriteRepositoriesMenu(_implementation._tsmiCategorisedRepos);
            if (_implementation._tsmiCategorisedRepos.DropDownItems.Count > 0)
            {
                DropDownItems.Add(_implementation._tsmiCategorisedRepos);
            }

            _implementation.RepositoryHistoryUIService.PopulateRecentRepositoriesMenu(this);

            DropDownItems.Add(new ToolStripSeparator());
            DropDownItems.Add(_implementation._tsmiOpenLocalRepository);
            DropDownItems.Add(_implementation._tsmiCloseRepo);
            DropDownItems.Add(new ToolStripSeparator());
            DropDownItems.Add(_implementation._tsmiRecentReposSettings);

            DropDown.ResumeLayout();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButtons.Right)
            {
                _implementation._startToolStripMenuItem.OpenRepositoryMenuItem.PerformClick();
            }
        }

        /// <summary>Updates the text shown on the combo button itself.</summary>
        public void RefreshContent()
        {
            Form? ownerForm = _implementation.OwnerForm;
            if (ownerForm is null)
            {
                // The component is unparented, no point doing anything.
                return;
            }

            string path = _implementation.Module.WorkingDir;

            // It appears at times Module.WorkingDir path is an empty string,
            // this caused issues like https://github.com/gitextensions/gitextensions/issues/4874.
            if (string.IsNullOrWhiteSpace(path))
            {
                Text = _noWorkingFolderText.Text;
                return;
            }

            IList<Repository> recentRepositoryHistory = ThreadHelper.JoinableTaskFactory.Run(
                () => RepositoryHistoryManager.Locals.AddAsMostRecentAsync(path));

            List<RecentRepoInfo> pinnedRepos = new();
            using Graphics graphics = ownerForm.CreateGraphics();
            RecentRepoSplitter splitter = new()
            {
                MeasureFont = Font,
                Graphics = graphics
            };

            splitter.SplitRecentRepos(recentRepositoryHistory, pinnedRepos, pinnedRepos);

            RecentRepoInfo? ri = pinnedRepos.Find(e => e.Repo.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase));

            Text = PathUtil.GetDisplayPath(ri?.Caption ?? path);

            if (AppSettings.RecentReposComboMinWidth > 0)
            {
                AutoSize = false;
                float captionWidth = graphics.MeasureString(Text, Font).Width;
                captionWidth = captionWidth + DropDownButtonWidth + 5;
                Width = Math.Max(AppSettings.RecentReposComboMinWidth, (int)captionWidth);
            }
            else
            {
                AutoSize = true;
            }
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
