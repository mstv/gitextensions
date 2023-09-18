using System.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;

namespace GitExtUtils;

/// <summary>
///   Functions for tracing otherwise ignored exceptions.
/// </summary>
public interface IExceptionTracer
{
    /// <summary>
    ///  Executes the <paramref name="action"/> and handles all exceptions.
    /// </summary>
    /// <param name="action">The action whose exceptions shall be handled.</param>
    void HandleExceptions(Action action);

    /// <summary>
    ///  Executes the <paramref name="action"/> and handles all exceptions of the specified type.
    /// </summary>
    /// <typeparam name="TException">The type of exceptions to be handled.</typeparam>
    /// <param name="action">The action whose exceptions shall be handled.</param>
    void HandleExceptions<TException>(Action action) where TException : Exception;

    /// <summary>
    ///  Traces the <paramref name="exception"/>.
    /// </summary>
    void TraceException(Exception exception);
}

public class ExceptionTracer : IExceptionTracer
{
    private readonly IMessenger _messenger;

    public ExceptionTracer(IMessenger messenger)
    {
        _messenger = messenger;
    }

    public void HandleExceptions(Action action) => HandleExceptions<Exception>(action);

    public void HandleExceptions<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            TraceException(exception);
        }
    }

    public void TraceException(Exception exception)
    {
        Trace.WriteLine(exception);
        try
        {
            _messenger.Send(exception);
        }
        catch (Exception sendException)
        {
            Trace.WriteLine(sendException);
        }
    }
}
