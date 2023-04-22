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
        private readonly TranslationString _noWorkingFolderText = new("No working directory");
        private readonly TranslationString _configureWorkingDirMenu = new("&Configure this menu");
        private readonly TranslationString _repositorySearchPlaceholder = new("Search repositories...");

        private static readonly object _excludeFromFilterMarker = new();

        private Func<GitUICommands>? _getUICommands;
        private RepositoryHistoryUIService _dont_use_me_repositoryHistoryUIService;

        private ToolStripMenuItem _tsmiCategorisedRepos = null!;
        private ToolStripMenuItem _tsmiOpenLocalRepository = null!;
        private ToolStripMenuItem _tsmiCloseRepo = null!;
        private ToolStripMenuItem _tsmiRecentReposSettings = null!;
        private readonly ToolStripTextBox _txtFilter = new();

        // NOTE: This is pretty bad, but we want to share the same look and feel of the menu items defined in the Start menu.
        private StartToolStripMenuItem _startToolStripMenuItem;
        private ToolStripMenuItem _closeToolStripMenuItem;

        public WorkingDirectoryToolStripSplitButton()
        {
            Name = nameof(WorkingDirectoryToolStripSplitButton);

            Image = Properties.Resources.RepoOpen;
            ImageAlign = ContentAlignment.MiddleLeft;
            ImageTransparentColor = Color.Magenta;
            TextAlign = ContentAlignment.MiddleLeft;

            _txtFilter.MaxLength = 200;
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
                if (DropDown.Items.Count <= defaultItemCount)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(filterTextbox.Text))
                {
                    foreach (ToolStripItem item in DropDown.Items)
                    {
                        item.Visible = true;
                    }

                    return;
                }

                foreach (ToolStripItem item in DropDown.Items)
                {
                    if (item is ToolStripSeparator || item.Tag == _excludeFromFilterMarker)
                    {
                        continue;
                    }

                    item.Visible = item.Text.Contains(filterTextbox.Text, StringComparison.CurrentCultureIgnoreCase);
                }
            };
        }

        /// <summary>
        ///  Gets the current instance of the git module.
        /// </summary>
        protected GitModule Module => UICommands.Module;

        /// <summary>
        ///  Gets the form that is displaying the menu item.
        /// </summary>
        protected static Form? OwnerForm
            => Form.ActiveForm ?? (Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null);

        /// <summary>
        ///  Gets the current instance of the <see cref="RepositoryHistoryUIService"/>.
        /// </summary>
        protected RepositoryHistoryUIService RepositoryHistoryUIService
            => _dont_use_me_repositoryHistoryUIService ?? throw new InvalidOperationException("The button is not initialized");

        /// <summary>
        ///  Gets the current instance of the UI commands.
        /// </summary>
        protected GitUICommands UICommands
            => (_getUICommands ?? throw new InvalidOperationException("The button is not initialized")).Invoke();

        /// <summary>
        ///  Initializes the menu item.
        /// </summary>
        /// <param name="getUICommands">The method that returns the current instance of UI commands.</param>
        public void Initialize(Func<GitUICommands> getUICommands, RepositoryHistoryUIService repositoryHistoryUIService,
                               StartToolStripMenuItem startToolStripMenuItem, ToolStripMenuItem closeToolStripMenuItem)
        {
            Translator.Translate(this, AppSettings.CurrentTranslation);

            _getUICommands = getUICommands;
            _dont_use_me_repositoryHistoryUIService = repositoryHistoryUIService;
            _startToolStripMenuItem = startToolStripMenuItem;
            _closeToolStripMenuItem = closeToolStripMenuItem;

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

                RefreshContent();
            };
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

            _txtFilter.Text = string.Empty;

            DropDownItems.Add(_txtFilter);
            DropDownItems.Add(new ToolStripSeparator());

            RepositoryHistoryUIService.PopulateFavouriteRepositoriesMenu(_tsmiCategorisedRepos);
            if (_tsmiCategorisedRepos.DropDownItems.Count > 0)
            {
                DropDownItems.Add(_tsmiCategorisedRepos);
            }

            RepositoryHistoryUIService.PopulateRecentRepositoriesMenu(this);

            DropDownItems.Add(new ToolStripSeparator());
            DropDownItems.Add(_tsmiOpenLocalRepository);
            DropDownItems.Add(_tsmiCloseRepo);
            DropDownItems.Add(new ToolStripSeparator());
            DropDownItems.Add(_tsmiRecentReposSettings);

            DropDown.ResumeLayout();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButtons.Right)
            {
                _startToolStripMenuItem.OpenRepositoryMenuItem.PerformClick();
            }
        }

        /// <summary>Updates the text shown on the combo button itself.</summary>
        public void RefreshContent()
        {
            string path = Module.WorkingDir;

            // it appears at times Module.WorkingDir path is an empty string, this caused issues like #4874
            if (string.IsNullOrWhiteSpace(path))
            {
                Text = _noWorkingFolderText.Text;
                return;
            }

            IList<Repository> recentRepositoryHistory = ThreadHelper.JoinableTaskFactory.Run(
                () => RepositoryHistoryManager.Locals.AddAsMostRecentAsync(path));

            List<RecentRepoInfo> pinnedRepos = new();
            using Graphics graphics = OwnerForm.CreateGraphics();
            RecentRepoSplitter splitter = new()
            {
                MeasureFont = Font,
                Graphics = graphics
            };

            splitter.SplitRecentRepos(recentRepositoryHistory, pinnedRepos, pinnedRepos);

            RecentRepoInfo ri = pinnedRepos.Find(e => e.Repo.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase));

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
