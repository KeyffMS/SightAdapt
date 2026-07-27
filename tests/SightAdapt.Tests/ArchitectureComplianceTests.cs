using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ArchitectureComplianceTests
{
    private static readonly Assembly ProductionAssembly =
        typeof(ProductInfo).Assembly;

    private static readonly Lazy<IReadOnlyList<MethodCall>> Calls =
        new(() => IlCallInspector.ReadCalls(ProductionAssembly));

    [TestMethod]
    public void ApplicationAssignmentWritesStayInDomainAuthorities()
    {
        var setters = new[]
        {
            nameof(ApplicationAssignment.DisplayName),
            nameof(ApplicationAssignment.ExecutableName),
            nameof(ApplicationAssignment.ExecutablePath),
            nameof(ApplicationAssignment.Enabled),
            nameof(ApplicationAssignment.VisualProfileId),
            nameof(ApplicationAssignment.MenuVisualProfileId),
            nameof(ApplicationAssignment.OverlayScopeId),
        }
        .Select(name => typeof(ApplicationAssignment)
            .GetProperty(name)!
            .SetMethod!)
        .ToArray();

        AssertCallsRestrictedTo(
            setters,
            typeof(ApplicationAssignment),
            typeof(ApplicationAssignmentService),
            typeof(PersistedSettingsMapper),
            typeof(ApplicationAssignmentNormalizationPass),
            typeof(ProfileReferenceNormalizationPass));
    }

    [TestMethod]
    public void AutomaticModeWritesStayInDomainAuthority()
    {
        AssertCallsRestrictedTo(
            [typeof(SightAdaptSettings)
                .GetProperty(nameof(SightAdaptSettings.AutomaticMode))!
                .SetMethod!],
            typeof(SightAdaptSettings),
            typeof(PersistedSettingsMapper),
            typeof(AutomaticModeManagementService));
    }

    [TestMethod]
    public void SettingsCollectionsHaveFocusedMutationAuthorities()
    {
        AssertListMutationRestrictedTo<ApplicationAssignment>(
            typeof(ApplicationAssignmentService),
            typeof(PersistedSettingsMapper),
            typeof(ApplicationAssignmentNormalizationPass),
            typeof(SettingsNormalizationContext));

        AssertListMutationRestrictedTo<VisualProfile>(
            typeof(VisualProfileManagementService),
            typeof(PersistedSettingsMapper),
            typeof(BuiltInVisualProfileNormalizationPass),
            typeof(UserVisualProfileNormalizationPass),
            typeof(SettingsNormalizationContext));
    }

    [TestMethod]
    public void ConfigurationFormsDelegateCommandsToUseCaseAuthorities()
    {
        CollectionAssert.Contains(
            FieldTypes(typeof(ConfigurationForm)),
            typeof(ConfigurationUseCases));
        CollectionAssert.Contains(
            FieldTypes(typeof(VisualProfileManagerForm)),
            typeof(VisualProfileUseCases));

        AssertNoCallsToConstructor<SettingsStore>(
            typeof(ConfigurationForm),
            typeof(VisualProfileManagerForm),
            typeof(RuntimeCoordinator),
            typeof(SightAdaptContext));
    }

    [TestMethod]
    public void RuntimeOverlayHasOneExplicitActivationContract()
    {
        var activateMethods = typeof(IRuntimeOverlay)
            .GetMethods()
            .Where(method => method.Name == nameof(IRuntimeOverlay.Activate))
            .ToArray();

        Assert.AreEqual(1, activateMethods.Length);
        CollectionAssert.AreEqual(
            new[] { typeof(OverlayActivationRequest) },
            activateMethods[0]
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray());
    }

    [TestMethod]
    public void RawNativeImportsStayInInteropBoundary()
    {
        var violations = ProductionAssembly
            .GetTypes()
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static |
                BindingFlags.Instance))
            .Where(method =>
                method.GetCustomAttribute<DllImportAttribute>() is not null)
            .Where(method => method.DeclaringType?.FullName?.StartsWith(
                "SightAdapt.NativeInterop+",
                StringComparison.Ordinal) != true)
            .Select(Describe)
            .OrderBy(name => name)
            .ToArray();

        Assert.AreEqual(
            0,
            violations.Length,
            "Raw DllImport declarations found outside NativeInterop: " +
            string.Join(", ", violations));
    }

    [TestMethod]
    public void DirectDebugWritesStayInDiagnosticSink()
    {
        var violations = Calls.Value
            .Where(call => call.Target.DeclaringType == typeof(Debug))
            .Where(call => OwnerType(call.Caller.DeclaringType) !=
                typeof(Diagnostics))
            .Select(call => Describe(call.Caller))
            .Distinct()
            .OrderBy(name => name)
            .ToArray();

        Assert.AreEqual(
            0,
            violations.Length,
            "Direct Debug calls found outside Diagnostics: " +
            string.Join(", ", violations));
    }

    [TestMethod]
    public void RemovedLegacyMutationServiceDoesNotReturn()
    {
        Assert.IsNull(ProductionAssembly.GetType(
            "SightAdapt.ApplicationAssignmentToggleService"));
    }

    private static void AssertCallsRestrictedTo(
        IEnumerable<MethodBase> targets,
        params Type[] allowedOwners)
    {
        var targetKeys = targets
            .Select(MethodKey.Create)
            .ToHashSet();
        var allowed = allowedOwners.ToHashSet();
        var violations = Calls.Value
            .Where(call => targetKeys.Contains(
                MethodKey.Create(call.Target)))
            .Where(call =>
                OwnerType(call.Caller.DeclaringType) is not { } owner ||
                !allowed.Contains(owner))
            .Select(call => Describe(call.Caller))
            .Distinct()
            .OrderBy(name => name)
            .ToArray();

        Assert.AreEqual(
            0,
            violations.Length,
            "Restricted mutation calls found in: " +
            string.Join(", ", violations));
    }

    private static void AssertListMutationRestrictedTo<T>(
        params Type[] allowedOwners)
    {
        var allowed = allowedOwners.ToHashSet();
        var violations = Calls.Value
            .Where(call => IsListMutation<T>(call.Target))
            .Where(call =>
                OwnerType(call.Caller.DeclaringType) is not { } owner ||
                !allowed.Contains(owner))
            .Select(call => Describe(call.Caller))
            .Distinct()
            .OrderBy(name => name)
            .ToArray();

        Assert.AreEqual(
            0,
            violations.Length,
            $"List<{typeof(T).Name}> mutation calls found outside " +
            "their authorities: " + string.Join(", ", violations));
    }

    private static bool IsListMutation<T>(MethodBase method)
    {
        if (method.Name is not ("Add" or "Remove") ||
            method.DeclaringType is not { IsGenericType: true } type ||
            type.GetGenericTypeDefinition() != typeof(List<>))
        {
            return false;
        }

        return type.GetGenericArguments()[0] == typeof(T);
    }

    private static void AssertNoCallsToConstructor<T>(
        params Type[] callerOwners)
    {
        var allowedCallers = callerOwners.ToHashSet();
        var violations = Calls.Value
            .Where(call => call.Target is ConstructorInfo &&
                call.Target.DeclaringType == typeof(T))
            .Where(call =>
                OwnerType(call.Caller.DeclaringType) is { } owner &&
                allowedCallers.Contains(owner))
            .Select(call => Describe(call.Caller))
            .Distinct()
            .OrderBy(name => name)
            .ToArray();

        Assert.AreEqual(
            0,
            violations.Length,
            $"{typeof(T).Name} is constructed directly by: " +
            string.Join(", ", violations));
    }

    private static Type[] FieldTypes(Type type)
    {
        return type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic)
            .Select(field => field.FieldType)
            .ToArray();
    }

    private static Type? OwnerType(Type? type)
    {
        while (type?.DeclaringType is not null)
        {
            type = type.DeclaringType;
        }

        return type;
    }

    private static string Describe(MethodBase method)
    {
        return $"{OwnerType(method.DeclaringType)?.FullName}.{method.Name}";
    }

    private readonly record struct MethodKey(
        Module Module,
        int MetadataToken)
    {
        public static MethodKey Create(MethodBase method)
        {
            return new MethodKey(method.Module, method.MetadataToken);
        }
    }
}
