using System.Reflection;
using System.Reflection.Emit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

internal sealed class TestWorkspace : IDisposable
{
    public TestWorkspace(string? category = null)
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "SightAdapt.Tests",
            string.IsNullOrWhiteSpace(category)
                ? "workspace"
                : category.Trim(),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string File(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return System.IO.Path.Combine(Path, fileName);
    }

    public SettingsCoordinator CreateSettingsCoordinator(
        string fileName = "settings.json")
    {
        return new SettingsCoordinator(
            new SettingsStore(File(fileName)));
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}

internal static class StaTest
{
    public static void Run(
        Action scenario,
        string? timeoutMessage = null,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                scenario();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.IsTrue(
            thread.Join(timeout ?? TimeSpan.FromSeconds(10)),
            timeoutMessage ?? "The STA test did not finish in time.");
        if (failure is not null)
        {
            Assert.Fail(failure.ToString());
        }
    }
}

internal static class RepositoryLayout
{
    private static readonly Lazy<string> RootValue =
        new(FindRepositoryRoot);

    public static string Root => RootValue.Value;

    public static string SourceDirectory =>
        System.IO.Path.Combine(Root, "src", "SightAdapt");

    public static string TestsDirectory =>
        System.IO.Path.Combine(Root, "tests", "SightAdapt.Tests");

    public static string ReadSource(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return File.ReadAllText(
            System.IO.Path.Combine(SourceDirectory, fileName));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(System.IO.Path.Combine(
                    directory.FullName,
                    "src",
                    "SightAdapt")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "The SightAdapt repository root could not be located.");
    }
}

internal static class IlCallInspector
{
    private static readonly IReadOnlyDictionary<ushort, OpCode> OpCodesByValue =
        typeof(OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode))
            .Select(field => (OpCode)field.GetValue(null)!)
            .GroupBy(opCode => unchecked((ushort)opCode.Value))
            .ToDictionary(
                group => group.Key,
                group => group.First());

    public static IReadOnlyList<MethodCall> ReadCalls(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var calls = new List<MethodCall>();
        foreach (var caller in assembly.GetTypes().SelectMany(GetMethods))
        {
            var body = caller.GetMethodBody();
            var bytes = body?.GetILAsByteArray();
            if (bytes is null)
            {
                continue;
            }

            ReadMethodCalls(caller, bytes, calls);
        }

        return calls;
    }

    private static IEnumerable<MethodBase> GetMethods(Type type)
    {
        const BindingFlags flags =
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Static |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly;
        return type.GetMethods(flags).Cast<MethodBase>()
            .Concat(type.GetConstructors(flags));
    }

    private static void ReadMethodCalls(
        MethodBase caller,
        byte[] bytes,
        ICollection<MethodCall> calls)
    {
        var offset = 0;
        while (offset < bytes.Length)
        {
            var opCodeValue = (ushort)bytes[offset++];
            if (opCodeValue == 0xfe)
            {
                opCodeValue = (ushort)(0xfe00 | bytes[offset++]);
            }

            if (!OpCodesByValue.TryGetValue(
                    opCodeValue,
                    out var opCode))
            {
                throw new InvalidOperationException(
                    $"Unknown IL opcode 0x{opCodeValue:x4} in {caller}.");
            }

            if (opCode.OperandType == OperandType.InlineMethod)
            {
                var token = BitConverter.ToInt32(bytes, offset);
                var target = ResolveMethod(caller, token);
                if (target is not null)
                {
                    calls.Add(new MethodCall(caller, target));
                }
            }

            offset += OperandSize(opCode.OperandType, bytes, offset);
        }
    }

    private static MethodBase? ResolveMethod(
        MethodBase caller,
        int token)
    {
        try
        {
            return caller.Module.ResolveMethod(
                token,
                caller.DeclaringType?.GetGenericArguments(),
                caller is MethodInfo { IsGenericMethod: true } method
                    ? method.GetGenericArguments()
                    : null);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static int OperandSize(
        OperandType operandType,
        byte[] bytes,
        int offset)
    {
        return operandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or
            OperandType.ShortInlineI or
            OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineBrTarget or
            OperandType.InlineField or
            OperandType.InlineI or
            OperandType.InlineMethod or
            OperandType.InlineSig or
            OperandType.InlineString or
            OperandType.InlineTok or
            OperandType.InlineType or
            OperandType.ShortInlineR => 4,
            OperandType.InlineI8 or
            OperandType.InlineR => 8,
            OperandType.InlineSwitch =>
                4 + BitConverter.ToInt32(bytes, offset) * 4,
            _ => throw new ArgumentOutOfRangeException(
                nameof(operandType),
                operandType,
                "Unsupported IL operand type."),
        };
    }
}

internal sealed record MethodCall(
    MethodBase Caller,
    MethodBase Target);
