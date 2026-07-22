Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Normalize-Newlines([string]$Value) {
    return $Value.Replace("`r`n", "`n")
}

function Replace-Exact(
    [string]$Path,
    [string]$Old,
    [string]$New,
    [int]$ExpectedCount = 1) {
    $content = Normalize-Newlines (Get-Content -Raw $Path)
    $oldValue = Normalize-Newlines $Old
    $newValue = Normalize-Newlines $New
    $count = [regex]::Matches(
        $content,
        [regex]::Escape($oldValue)).Count
    if ($count -ne $ExpectedCount) {
        throw "Expected $ExpectedCount occurrence(s) in '$Path', found $count."
    }

    $content = $content.Replace($oldValue, $newValue)
    [System.IO.File]::WriteAllText($Path, $content, $Utf8NoBom)
}

Replace-Exact 'src/SightAdapt/ApplicationProfile.cs' @'
    [JsonPropertyName("overlayScope")]
    public string OverlayScopeId
    {
        get => _overlayScopeId;
        set
        {
            OverlayScopePolicy.TryParseId(value, out var scope);
            _overlayScopeId = OverlayScopePolicy.ToId(scope);
        }
    }
'@ @'
    [JsonPropertyName("overlayScope")]
    public string OverlayScopeId
    {
        get => _overlayScopeId;
        set => _overlayScopeId = value ?? string.Empty;
    }
'@

Replace-Exact 'src/SightAdapt/SettingsStore.cs' @'
        CanonicalizeBuiltInProfiles(context);
        NormalizeCustomProfiles(context);
        NormalizeApplications(context);
        RepairProfileReferences(context);
'@ @'
        CanonicalizeBuiltInProfiles(context);
        NormalizeCustomProfiles(context);
        NormalizeApplicationOverlayScopes(context);
        NormalizeApplications(context);
        RepairProfileReferences(context);
'@

Replace-Exact 'src/SightAdapt/SettingsStore.cs' @'
    private static void NormalizeApplications(
        SettingsNormalizationContext context)
'@ @'
    private static void NormalizeApplicationOverlayScopes(
        SettingsNormalizationContext context)
    {
        foreach (var application in
                 context.SourceApplications)
        {
            var persistedId =
                application.OverlayScopeId ??
                string.Empty;
            OverlayScopePolicy.TryParseId(
                persistedId,
                out var scope);
            var canonicalId =
                OverlayScopePolicy.ToId(scope);

            if (string.Equals(
                    persistedId,
                    canonicalId,
                    StringComparison.Ordinal))
            {
                continue;
            }

            application.OverlayScopeId = canonicalId;
            context.MarkChanged();
        }
    }

    private static void NormalizeApplications(
        SettingsNormalizationContext context)
'@

Replace-Exact 'tests/SightAdapt.Tests/OverlayScopeTests.cs' @'
    [TestMethod]
    public void InvalidPersistedScopeRecoversToDefault()
    {
        var profile = new ApplicationProfile
        {
            OverlayScopeId = "unknown-scope",
        };

        Assert.AreEqual(OverlayScope.ClientArea, profile.OverlayScope);
        Assert.AreEqual("client-area", profile.OverlayScopeId);
    }
'@ @'
    [TestMethod]
    public void InvalidPersistedScopeIsRecoveredBySettingsNormalization()
    {
        var profile = new ApplicationProfile
        {
            DisplayName = "Reader",
            ExecutableName = "reader.exe",
            ExecutablePath = @"C:\Apps\reader.exe",
            OverlayScopeId = "unknown-scope",
        };
        var settings = new SightAdaptSettings
        {
            Applications = [profile],
        };

        Assert.AreEqual("unknown-scope", profile.OverlayScopeId);
        Assert.ThrowsException<ArgumentException>(() =>
        {
            _ = profile.OverlayScope;
        });

        Assert.IsTrue(SettingsStore.Normalize(settings));
        Assert.AreEqual(OverlayScope.ClientArea, profile.OverlayScope);
        Assert.AreEqual("client-area", profile.OverlayScopeId);
        Assert.IsFalse(SettingsStore.Normalize(settings));
    }
'@

Replace-Exact 'tests/SightAdapt.Tests/SettingsStoreTests.cs' @'
    [TestMethod]
    public void NormalizeRemovesDuplicateApplicationPaths()
'@ @'
    [TestMethod]
    public void LoadKeepsCanonicalOverlayScopeWithoutMigration()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        File.WriteAllText(
            settingsPath,
            """
            {
              "schemaVersion": 4,
              "applications": [
                {
                  "displayName": "Reader",
                  "executableName": "reader.exe",
                  "executablePath": "C:\\Apps\\reader.exe",
                  "visualProfileId": "default-soft-invert",
                  "overlayScope": "screen"
                }
              ]
            }
            """);

        var store = new SettingsStore(settingsPath);
        var settings = store.Load();

        Assert.IsFalse(store.SettingsWereMigrated);
        Assert.AreEqual("screen", settings.Applications[0].OverlayScopeId);
        Assert.AreEqual(OverlayScope.Screen, settings.Applications[0].OverlayScope);
    }

    [TestMethod]
    public void LoadCanonicalizesOverlayScopeAliasAndReportsMigration()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        File.WriteAllText(
            settingsPath,
            """
            {
              "schemaVersion": 4,
              "applications": [
                {
                  "displayName": "Reader",
                  "executableName": "reader.exe",
                  "executablePath": "C:\\Apps\\reader.exe",
                  "visualProfileId": "default-soft-invert",
                  "overlayScope": "full-window"
                }
              ]
            }
            """);

        var store = new SettingsStore(settingsPath);
        var settings = store.Load();

        Assert.IsTrue(store.SettingsWereMigrated);
        Assert.AreEqual("window", settings.Applications[0].OverlayScopeId);
        Assert.AreEqual(OverlayScope.Window, settings.Applications[0].OverlayScope);

        store.Save(settings);
        StringAssert.Contains(
            File.ReadAllText(settingsPath),
            "\"overlayScope\": \"window\"");
    }

    [TestMethod]
    public void LoadRepairsInvalidOverlayScopeAndReportsMigration()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        File.WriteAllText(
            settingsPath,
            """
            {
              "schemaVersion": 4,
              "applications": [
                {
                  "displayName": "Reader",
                  "executableName": "reader.exe",
                  "executablePath": "C:\\Apps\\reader.exe",
                  "visualProfileId": "default-soft-invert",
                  "overlayScope": "unknown-scope"
                }
              ]
            }
            """);

        var store = new SettingsStore(settingsPath);
        var settings = store.Load();

        Assert.IsTrue(store.SettingsWereMigrated);
        Assert.AreEqual("client-area", settings.Applications[0].OverlayScopeId);
        Assert.AreEqual(OverlayScope.ClientArea, settings.Applications[0].OverlayScope);

        store.Save(settings);
        StringAssert.Contains(
            File.ReadAllText(settingsPath),
            "\"overlayScope\": \"client-area\"");
    }

    [TestMethod]
    public void NormalizeRemovesDuplicateApplicationPaths()
'@

Replace-Exact 'tests/SightAdapt.Tests/ArchitectureComplianceTests.cs' @'
        StringAssert.Contains(source, "NormalizeCustomProfiles(context);");
        StringAssert.Contains(source, "NormalizeApplications(context);");
        StringAssert.Contains(source, "RepairProfileReferences(context);");
'@ @'
        StringAssert.Contains(source, "NormalizeCustomProfiles(context);");
        StringAssert.Contains(source, "NormalizeApplicationOverlayScopes(context);");
        StringAssert.Contains(source, "NormalizeApplications(context);");
        StringAssert.Contains(source, "RepairProfileReferences(context);");
'@

Replace-Exact 'docs/ARCHITECTURE.md' `
    '| Migration, normalization, recovery, and reference repair | `SettingsStore.Normalize` |' `
    '| Migration, scope canonicalization, normalization, recovery, and reference repair | `SettingsStore.Normalize` |'
