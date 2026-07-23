using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class NativeCallTests
{
    [TestMethod]
    public void CriticalFailureIncludesOperationAndNativeError()
    {
        var exception =
            Assert.ThrowsException<Win32Exception>(() =>
                NativeCall.RequireSuccess(
                    succeeded: false,
                    "Apply magnifier filter",
                    () => 5));

        Assert.AreEqual(5, exception.NativeErrorCode);
        StringAssert.Contains(
            exception.Message,
            "Apply magnifier filter");
        StringAssert.Contains(
            exception.Message,
            "Win32 error 5");
    }

    [TestMethod]
    public void RequiredHandleRejectsZeroHandle()
    {
        var exception =
            Assert.ThrowsException<Win32Exception>(() =>
                NativeCall.RequireHandle(
                    nint.Zero,
                    "Create magnifier control",
                    () => 87));

        Assert.AreEqual(87, exception.NativeErrorCode);
        StringAssert.Contains(
            exception.Message,
            "Create magnifier control");
    }

    [TestMethod]
    public void TransientFailureIsReportedWithoutThrowing()
    {
        var messages = new List<string>();

        var succeeded = NativeCall.TryTransient(
            succeeded: false,
            "Position overlay",
            () => 1400,
            messages.Add);

        Assert.IsFalse(succeeded);
        Assert.AreEqual(1, messages.Count);
        StringAssert.Contains(
            messages[0],
            "Position overlay");
        StringAssert.Contains(
            messages[0],
            "Win32 error 1400");
    }

    [TestMethod]
    public void BestEffortFailureIsReported()
    {
        var messages = new List<string>();

        NativeCall.BestEffort(
            succeeded: false,
            "Destroy magnifier control",
            () => 6,
            messages.Add);

        Assert.AreEqual(1, messages.Count);
        StringAssert.Contains(
            messages[0],
            "Destroy magnifier control");
    }

    [TestMethod]
    public void SuccessfulCallsDoNotReadOrReportAnError()
    {
        var errorReads = 0;
        var messages = new List<string>();

        NativeCall.RequireSuccess(
            succeeded: true,
            "Initialize magnifier",
            () =>
            {
                errorReads++;
                return 1;
            });
        var transient = NativeCall.TryTransient(
            succeeded: true,
            "Position overlay",
            () =>
            {
                errorReads++;
                return 1;
            },
            messages.Add);
        NativeCall.BestEffort(
            succeeded: true,
            "Destroy magnifier control",
            () =>
            {
                errorReads++;
                return 1;
            },
            messages.Add);

        Assert.IsTrue(transient);
        Assert.AreEqual(0, errorReads);
        Assert.AreEqual(0, messages.Count);
    }
}
