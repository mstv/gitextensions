#nullable enable

using System;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace GitUI
{
    public static class TaskDialogExtensions
    {
        public static void AddButton(this TaskDialog taskDialog, string text, Action? action = null, string? name = null)
        {
            TaskDialogCommandLink taskDialogCommandLink = new(name ?? text, text);
            taskDialogCommandLink.Click += (s, e) =>
            {
                taskDialog.Close();
                action?.Invoke();
            };
            taskDialog.Controls.Add(taskDialogCommandLink);
        }
    }
}
