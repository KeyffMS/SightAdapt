using System.Diagnostics;

namespace SightAdapt;

internal enum DiagnosticSeverity
{
    Information,
    Warning,
    Error,
}

internal enum DiagnosticFailurePolicy
{
    None,
    Critical,
    Transient,
    BestEffort,
    Recovered,
}

internal sealed record DiagnosticEvent(
    string Component,
    string Operation,
    DiagnosticSeverity Severity,
    DiagnosticFailurePolicy FailurePolicy,
    string Message,
    Exception? Exception = null,
    int? NativeErrorCode = null);

internal interface IDiagnosticSink
{
    void Write(DiagnosticEvent diagnosticEvent);
}

internal sealed class DebugDiagnosticSink : IDiagnosticSink
{
    public void Write(DiagnosticEvent diagnosticEvent)
    {
        ArgumentNullException.ThrowIfNull(diagnosticEvent);
        Debug.WriteLine(
            $"SightAdapt [{diagnosticEvent.Severity}] " +
            $"{diagnosticEvent.Component}/" +
            $"{diagnosticEvent.Operation} " +
            $"({diagnosticEvent.FailurePolicy}): " +
            diagnosticEvent.Message);
    }
}

internal static class Diagnostics
{
    private static readonly object Sync = new();
    private static IDiagnosticSink _sink =
        new DebugDiagnosticSink();

    public static void Report(
        string component,
        string operation,
        DiagnosticSeverity severity,
        DiagnosticFailurePolicy failurePolicy,
        string message,
        Exception? exception = null,
        int? nativeErrorCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(component);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        IDiagnosticSink sink;
        lock (Sync)
        {
            sink = _sink;
        }

        sink.Write(new DiagnosticEvent(
            component.Trim(),
            operation.Trim(),
            severity,
            failurePolicy,
            message.Trim(),
            exception,
            nativeErrorCode));
    }

    internal static IDisposable UseSink(
        IDiagnosticSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        lock (Sync)
        {
            var previous = _sink;
            _sink = sink;
            return new SinkScope(previous);
        }
    }

    private sealed class SinkScope(
        IDiagnosticSink previous) : IDisposable
    {
        private IDiagnosticSink? _previous = previous;

        public void Dispose()
        {
            lock (Sync)
            {
                if (_previous is null)
                {
                    return;
                }

                _sink = _previous;
                _previous = null;
            }
        }
    }
}
