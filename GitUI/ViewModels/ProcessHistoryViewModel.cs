using System.ComponentModel.Design;
using System.Diagnostics;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GitCommands;
using GitUI.Models;

namespace GitUI.ViewModels;

public partial class ProcessHistoryViewModel : ObservableRecipient, IRecipient<RunProcessInfo>, IRecipient<Exception>
{
    private const string _endMark = "###";
    private const string _noExecutable = "---";

    private List<StringBuilder> _processHistory;

    public ProcessHistoryViewModel(in int historyDepth)
    {
        _processHistory = new List<StringBuilder>(capacity: historyDepth);
    }

    public string History
    {
        get
        {
            StringBuilder sb = new();
            foreach (StringBuilder entry in _processHistory)
            {
                sb.Append(entry);
            }

            sb.Append(_endMark);

            return sb.ToString();
        }
    }

    internal static void Register(IServiceContainer serviceContainer)
    {
        ProcessHistoryViewModel model = new(AppSettings.GitOutputHistoryDepth.Value);
        serviceContainer.AddService(model);
        serviceContainer.GetRequiredService<IMessenger>().RegisterAll(model);
    }

    [RelayCommand]
    public void Clear()
    {
        _processHistory.Clear();

        OnPropertyChanged(nameof(History));
    }

    public void Receive(RunProcessInfo runProcess)
    {
        Add(Format(runProcess));

        return;

        static StringBuilder Format(in RunProcessInfo runProcess)
        {
            StringBuilder sb = new();
            sb.Append(runProcess.FinishTime.ToShortTimeString()).Append(' ');
            if (string.IsNullOrWhiteSpace(runProcess.Executable))
            {
                sb.AppendLine(_noExecutable);
            }
            else
            {
                sb.Append(runProcess.Executable).Append(' ').AppendLine(runProcess.Arguments);
            }

            return sb.AppendLine(runProcess.Output);
        }
    }

    public void Receive(Exception exception)
    {
        Add(new StringBuilder().Append(DateTime.Now.ToShortTimeString()).Append(' ').AppendLine(exception.ToStringDemystified()));
    }

    private void Add(StringBuilder entry)
    {
        if (_processHistory.Count == _processHistory.Capacity)
        {
            _processHistory.RemoveAt(0);
        }

        _processHistory.Add(entry);

        OnPropertyChanged(nameof(History));
    }
}
