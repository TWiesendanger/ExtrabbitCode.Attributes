using ExtrabbitCode.Inventor.Attributes.Models;
using ExtrabbitCode.Inventor.Attributes.Services;
using System;

namespace ExtrabbitCode.Inventor.Attributes;

public static class Globals
{
    private static Application? _invApp;
    private static ApplicationAddInSite? _invApplicationAddInSite;
    private static Theme? _activeTheme;

    public static SettingsService SettingsService { get; set; } = null!;
    public static AttributeService AttributeService { get; set; } = null!;
    public static AttributeLibraryService AttributeLibraryService { get; set; } = null!;
    public static ITelemetryService TelemetryService { get; set; } = null!;
    public static IAddAttributeWorkflowService AddAttributeWorkflowService { get; set; } = null!;
    public static Action<AddAttributeWorkflowResult>? OnAttributeAdded { get; set; }

    public static UserNotificationService UserNotificationService { get; set; } = null!;

    public const string AddInClientId = "2b75e9c2-a980-4760-8687-67966594c181";

    /// <summary>Holds a reference to the Inventor application instance.</summary>
    public static Application InvApp {
        get => _invApp ?? throw new InvalidOperationException("Inventor application not initialized.");
        internal set => _invApp = value;
    }

    /// <summary>Holds a reference to the add‑in site object.</summary>
    public static ApplicationAddInSite InvApplicationAddInSite {
        get => _invApplicationAddInSite ?? throw new InvalidOperationException("Add‑in site not initialized.");
        internal set => _invApplicationAddInSite = value;
    }

    /// <summary>Holds a reference to the active Inventor theme (light or dark).</summary>
    public static Theme ActiveTheme {
        get => _activeTheme ?? throw new InvalidOperationException("Active theme not initialized.");
        internal set => _activeTheme = value;
    }
}