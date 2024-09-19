using System.Diagnostics;
using System.Text;
using GitUI.Editor.Diff;
using ICSharpCode.TextEditor.Document;

namespace GitUI.Models;

public interface IOutputHistoryModel
{
    /// <summary>
    ///  Is invoked when something was added to the output history.
    /// </summary>
    event EventHandler HistoryChanged;

    /// <summary>
    ///  Gets whether the output history is enabled.
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    ///  Gets the current output history formatted as string.
    /// </summary>
    string History { get; }

    /// <summary>
    ///  Clears the recorded history.
    /// </summary>
    void ClearHistory();

    /// <summary>
    ///  Records the output after a process has run.
    /// </summary>
    void RecordHistory(in RunProcessInfo runProcess);

    /// <summary>
    ///  Records an exception.
    /// </summary>
    void RecordHistory(in Exception exception);

    /// <summary>
    ///  Records a string message.
    /// </summary>
    void RecordHistory(in string message);
}

internal sealed class OutputHistoryModel : IOutputHistoryModel
{
    private const string _endMark = "###";
    private const string _noExecutable = "---";

    private List<StringBuilder> _outputHistory;

    public event EventHandler HistoryChanged;

    public OutputHistoryModel(in int historyDepth)
    {
        _outputHistory = new List<StringBuilder>(capacity: historyDepth);
    }

    public bool Enabled => _outputHistory.Capacity > 0;

    public string History
    {
        get
        {
            StringBuilder sb = new();
            lock (_outputHistory)
            {
                foreach (StringBuilder entry in _outputHistory)
                {
                    sb.Append(entry);
                }
            }

            sb.AppendLine(_endMark);

            return sb.ToString();
        }
    }

    public void ClearHistory()
    {
        lock (_outputHistory)
        {
            _outputHistory.Clear();
        }

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RecordHistory(in string message)
    {
        string time = DateTime.Now.ToShortTimeString();
        Add(new StringBuilder(time, capacity: time.Length + 1 + message.Length).Append(' ').AppendLine(message));
    }

    public void RecordHistory(in Exception exception)
    {
        RecordHistory(exception.ToStringDemystified());
    }

    public void RecordHistory(in RunProcessInfo runProcess)
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

            List<TextMarker> textMarkers = [];
            AnsiEscapeUtilities.ParseEscape(runProcess.Output.Trim(), sb, textMarkers, traceErrors: false);

            return sb.AppendLine().AppendLine();
        }
    }

    private void Add(in StringBuilder entry)
    {
        if (!Enabled)
        {
            return;
        }

        lock (_outputHistory)
        {
            if (_outputHistory.Count == _outputHistory.Capacity)
            {
                _outputHistory.RemoveAt(0);
            }

            _outputHistory.Add(entry);
        }

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }
}
