using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
[DoNotParallelize]
public sealed class DiagnosticsTests
{
    [TestMethod]
    public void TransientNativeFailureUsesStructuredDiagnostic()
    {
        var sink = new RecordingDiagnosticSink();
        using var scope = Diagnostics.UseSink(sink);

        Assert.IsFalse(
            NativeCall.TryTransient(
                succeeded: false,
                "Position overlay"));

        var diagnostic = sink.Events.Single();
        Assert.AreEqual(
            DiagnosticSeverity.Warning,
            diagnostic.Severity);
        Assert.AreEqual(
            DiagnosticFailurePolicy.Transient,
            diagnostic.FailurePolicy);
        Assert.AreEqual(
            "Position overlay",
            diagnostic.Operation);
    }

    [TestMethod]
    public void CriticalNativeFailureUsesStructuredDiagnostic()
    {
        var sink = new RecordingDiagnosticSink();
        using var scope = Diagnostics.UseSink(sink);

        Assert.ThrowsException<Win32Exception>(() =>
            NativeCall.RequireSuccess(
                succeeded: false,
                "Apply effect",
                () => 5));

        var diagnostic = sink.Events.Single();
        Assert.AreEqual(
            DiagnosticSeverity.Error,
            diagnostic.Severity);
        Assert.AreEqual(
            DiagnosticFailurePolicy.Critical,
            diagnostic.FailurePolicy);
        Assert.AreEqual(5, diagnostic.NativeErrorCode);
    }

    [TestMethod]
    public void DiagnosticSinkScopeRestoresPreviousSink()
    {
        var outer = new RecordingDiagnosticSink();
        var inner = new RecordingDiagnosticSink();

        using (Diagnostics.UseSink(outer))
        {
            using (Diagnostics.UseSink(inner))
            {
                Diagnostics.Report(
                    "Test",
                    "Inner",
                    DiagnosticSeverity.Information,
                    DiagnosticFailurePolicy.None,
                    "inner");
            }

            Diagnostics.Report(
                "Test",
                "Outer",
                DiagnosticSeverity.Information,
                DiagnosticFailurePolicy.None,
                "outer");
        }

        Assert.AreEqual(1, inner.Events.Count);
        Assert.AreEqual(1, outer.Events.Count);
        Assert.AreEqual("Outer", outer.Events[0].Operation);
    }
}
