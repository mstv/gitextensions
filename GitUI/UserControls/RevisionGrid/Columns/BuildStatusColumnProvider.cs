using System.Diagnostics.CodeAnalysis;
using GitCommands;
using GitCommands.Settings;
using GitExtUtils.GitUI;
using GitExtUtils.GitUI.Theming;
using GitUIPluginInterfaces;
using GitUIPluginInterfaces.BuildServerIntegration;

namespace GitUI.UserControls.RevisionGrid.Columns
{
    internal sealed class BuildStatusColumnProvider : ColumnProvider
    {
        private const int IconColumnWidth = 16;
        private const int TextColumnWidth = 150;

        private readonly RevisionGridControl _grid;
        private readonly RevisionDataGridView _gridView;
        private readonly Func<GitModule> _module;

        // Increase contrast to selected rows
        private readonly Color _lightBlue = Color.FromArgb(130, 180, 240);

        public BuildStatusColumnProvider(RevisionGridControl grid, RevisionDataGridView gridView, Func<GitModule> module)
            : base("Build Status")
        {
            _grid = grid;
            _gridView = gridView;
            _module = module;

            Column = new DataGridViewTextBoxColumn
            {
                HeaderText = "Build Status",
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Width = DpiUtil.Scale(TextColumnWidth)
            };
        }

        public override void Refresh(int rowHeight, in VisibleRowRange range)
        {
            var showIcon = AppSettings.ShowBuildStatusIconColumn;
            var showText = AppSettings.ShowBuildStatusTextColumn;

            IBuildServerSettings buildServerSettings = _module().GetEffectiveSettings().GetBuildServerSettings();
            var columnVisible = buildServerSettings.IntegrationEnabledOrDefault && (showIcon || showText);

            Column.Visible = columnVisible;

            if (columnVisible)
            {
                UpdateWidth();
            }

            return;

            void UpdateWidth()
            {
                Column.Resizable = showText ? DataGridViewTriState.True : DataGridViewTriState.False;
                Column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

                var iconColumnWidth = DpiUtil.Scale(IconColumnWidth);

                if (showIcon && !showText)
                {
                    Column.Width = iconColumnWidth;
                }
                else if (showText && Column.Width == iconColumnWidth)
                {
                    Column.Width = DpiUtil.Scale(TextColumnWidth);
                }
            }
        }

        public override void OnCellPainting(DataGridViewCellPaintingEventArgs e, GitRevision revision, int rowHeight, in CellStyle style)
        {
            if (revision.BuildStatus is null)
            {
                return;
            }

            string text = (AppSettings.ShowBuildStatusIconColumn ? revision.BuildStatus.StatusIcon : string.Empty)
                + (AppSettings.ShowBuildStatusTextColumn ? (string)e.FormattedValue : string.Empty);

            _grid.DrawColumnText(e, text, style.NormalFont, GetColor(style.ForeColor), bounds: e.CellBounds);

            Color GetColor(Color foreColor)
            {
                var isSelected = _gridView.Rows[e.RowIndex].Selected;

                Color customColor;
                switch (revision.BuildStatus.Status)
                {
                    case BuildInfo.BuildStatus.Unknown:
                        return foreColor;

                    case BuildInfo.BuildStatus.Success:
                        customColor = isSelected ? Color.LightGreen : Color.DarkGreen;
                        break;
                    case BuildInfo.BuildStatus.Failure:
                        customColor = isSelected ? Color.Red : Color.DarkRed;
                        break;
                    case BuildInfo.BuildStatus.InProgress:
                        customColor = isSelected ? _lightBlue : Color.Blue;
                        break;
                    case BuildInfo.BuildStatus.Unstable:
                        customColor = Color.OrangeRed;
                        break;
                    case BuildInfo.BuildStatus.Stopped:
                    default:
                        customColor = isSelected ? Color.LightGray : Color.Gray;
                        break;
                }

                return customColor.AdaptTextColor();
            }
        }

        public override void OnCellFormatting(DataGridViewCellFormattingEventArgs e, GitRevision revision)
        {
            e.Value = !string.IsNullOrEmpty(revision.BuildStatus?.Description)
                ? revision.BuildStatus.Description
                : "";
            e.FormattingApplied = true;
        }

        public override bool TryGetToolTip(DataGridViewCellMouseEventArgs e, GitRevision revision, [NotNullWhen(returnValue: true)] out string? toolTip)
        {
            if (revision.BuildStatus is not null)
            {
                toolTip = revision.BuildStatus.Tooltip ?? revision.BuildStatus.Description;
                return toolTip is not null;
            }

            return base.TryGetToolTip(e, revision, out toolTip);
        }
    }
}
