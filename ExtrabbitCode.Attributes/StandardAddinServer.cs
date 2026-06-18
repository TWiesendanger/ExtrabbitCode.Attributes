using ExtrabbitCode.Attributes.Addin;
using ExtrabbitCode.Attributes.Helper;
using ExtrabbitCode.Attributes.Models;
using ExtrabbitCode.Attributes.Services;
using ExtrabbitCode.Attributes.UI;
using log4net;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Wpf.Ui.Appearance;

namespace ExtrabbitCode.Attributes;

[ProgId("Inventor.Core.Template.StandardAddInServer")]
[Guid(Globals.AddInClientId)]
public class StandardAddInServer : IsolatedApplicationAddInServer
{
    private UserInterfaceEvents? _uiEvents;
    private readonly List<RibbonPanel> _ribbonPanels = [];
    private readonly List<RibbonTab> _ribbonTabs = [];
    private readonly List<CommandControl> _buttons = [];
    private readonly List<ButtonDefinition> _buttonDefinitions = [];

    public static ApplicationEvents? InvAppEvents { get; set; }

    private ButtonDefinition? _settingsButton;
    private ButtonDefinition? _openAttributeWindow;
    private ButtonDefinition? _addAttributeToObject;
    private ButtonDefinition? _info;
    private ButtonDefinition? _helpPage;

    private static readonly ILog Logger = LogManagerAddin.GetLogger(typeof(StandardAddInServer));

    public UserInterfaceEvents? UiEvents {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get => _uiEvents;

        [MethodImpl(MethodImplOptions.Synchronized)]
        set {
            if (_uiEvents != null)
            {
                _uiEvents.OnResetRibbonInterface -= UiEventsOnResetRibbonInterface;
            }

            _uiEvents = value;
            if (_uiEvents != null)
            {
                _uiEvents.OnResetRibbonInterface += UiEventsOnResetRibbonInterface;
            }
        }
    }

    // ReSharper disable once CA1725
#pragma warning disable CA1725 // Parameter names should match base declaration
    public override void OnActivate()
#pragma warning restore CA1725 // Parameter names should match base declaration
    {
        ArgumentNullException.ThrowIfNull(ApplicationAddInSite);

        try
        {
            Logger.Debug("Addin InventorTemplate Activated");

            Globals.InvApp = ApplicationAddInSite.Application;
            Globals.InvApplicationAddInSite = ApplicationAddInSite;
            Globals.SettingsService = new SettingsService(StoragePaths.SettingsFile);
            Globals.AttributeService = new AttributeService();
            Globals.AddAttributeWorkflowService = new AddAttributeWorkflowService(Globals.AttributeService);
            Globals.AttributeLibraryService = new AttributeLibraryService(StoragePaths.AttributeLibraryFile);
            Globals.UserNotificationService = new UserNotificationService();
            UiEvents = Globals.InvApp.UserInterfaceManager.UserInterfaceEvents;
            InvAppEvents = Globals.InvApp.ApplicationEvents;
            InvAppEvents.OnApplicationOptionChange += InvAppEvents_OnApplicationOptionChange;

            ThemeManager themeManager = Globals.InvApp.ThemeManager;
            Globals.ActiveTheme = themeManager.ActiveTheme;
            string themeName = Globals.ActiveTheme.Name;
            ThemeResourceHelper.ApplyInventorThemeResources();
            Logger.Debug("Inventor ThemeManager ActiveTheme: " + themeName);

            ApplicationTheme appTheme = themeName == InventorThemeConstants.LightTheme
                ? ApplicationTheme.Light
                : ApplicationTheme.Dark;

            // Force initialize Wpf.Ui before any dialog opens
            ApplicationThemeManager.Apply(appTheme);

            // ModernUi theme + font (window-scoped), derived from Inventor. Used by migrated dialogs.
            Globals.CurrentTheme = themeName == InventorThemeConstants.LightTheme
                ? ExtrabbitCode.Inventor.ModernUi.Theme.Light
                : ExtrabbitCode.Inventor.ModernUi.Theme.Dark;
            Globals.CurrentFont = ExtrabbitCode.Inventor.ModernUi.FontOptions.FromInventor(
                Globals.InvApp.GeneralOptions.TextAppearance, Globals.InvApp.GeneralOptions.TextSize);

            InitializeTelemetry();

            _info = UiDefinitionHelper.CreateButton("Info", "ExtrabbitCode.Attributes.Info", @"UI\ButtonResources\Info", themeName);
            _helpPage = UiDefinitionHelper.CreateButton("Help", "ExtrabbitCode.Attributes.Help", @"UI\ButtonResources\Help", themeName);
            _settingsButton = UiDefinitionHelper.CreateButton("Settings", "ExtrabbitCode.Attributes.SettingsButton", @"UI\ButtonResources\Settings", themeName);
            _openAttributeWindow = UiDefinitionHelper.CreateButton("Attribute Dialog", "ExtrabbitCode.Attributes.OpenAttributeWindow", @"UI\ButtonResources\AttributeWindow", themeName);
            _addAttributeToObject = UiDefinitionHelper.CreateButton("Add attribute", "ExtrabbitCode.Attributes.AddAttribute", @"UI\ButtonResources\AddAttribute", themeName);
            _buttonDefinitions.Add(_info);
            _buttonDefinitions.Add(_helpPage);
            _buttonDefinitions.Add(_settingsButton);
            _buttonDefinitions.Add(_openAttributeWindow);
            _buttonDefinitions.Add(_addAttributeToObject);

            if (FirstTime)
            {
                AddToUserInterface();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Unexpected failure during activation of the add-in 'InventorTemplate'.",
                ex
            );
        }
    }

    private static void InitializeTelemetry()
    {
        SettingsModel settings = Globals.SettingsService.GetCopy();
        bool telemetryEnabled = settings.TelemetryEnabled;
        string distinctId = settings.TelemetryId;

        Globals.TelemetryService = new PostHogTelemetryService(
            apiKey: "phc_nAWoVFjC6Fn6xWLTbK5S5qM6onNocCjgfPnXx2azfyZe",
            distinctId: distinctId,
            enabled: telemetryEnabled);

        Globals.TelemetryService.TrackEvent("addin_activated", new Dictionary<string, object>
        {
            ["inventor_version"] = Globals.InvApp.SoftwareVersion.DisplayVersion,
            ["addin_version"] = typeof(StandardAddInServer).Assembly.GetName().Version?.ToString() ?? "unknown",
            ["theme"] = Globals.ActiveTheme.Name
        });
    }

    public override void OnDeactivate()
    {
        try
        {
            Globals.TelemetryService.TrackEvent("addin_deactivated");

            Task.Run(async () =>
            {
                await Globals.TelemetryService.FlushAsync().ConfigureAwait(false);
            }).Wait(TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            Logger.Debug($"Telemetry shutdown failed: {ex.Message}", ex);
        }

        DeleteAttributeWindow();
        ReleaseButtons();
        ReleaseRibbonPanels();
        ReleaseRibbonTabs();
        ReleaseAppEvents();

        if (_uiEvents == null)
        {
            return;
        }

        _uiEvents.OnResetRibbonInterface -= UiEventsOnResetRibbonInterface;
        _uiEvents = null;
    }

    private static void DeleteAttributeWindow()
    {
        try
        {
            DockableWindow dw = Globals.InvApp.UserInterfaceManager
                .DockableWindows["ExtrabbitCode.Attributes.Window"];
            dw.Delete();
        }
        catch
        {
            // Window was never opened — nothing to delete
        }
    }

    private void ReleaseAppEvents()
    {
        if (InvAppEvents is null)
        {
            return;
        }

        try
        {
            InvAppEvents.OnApplicationOptionChange -= InvAppEvents_OnApplicationOptionChange;
        }
        catch (COMException ex)
        {
            Logger.Debug("COMException while releasing application events.", ex);
        }
        catch (InvalidComObjectException ex)
        {
            Logger.Debug("InvalidComObjectException while releasing application events.", ex);
        }
        finally
        {
            InvAppEvents = null;
        }
    }

    private void ReleaseRibbonTabs()
    {
        if (_ribbonTabs.Count == 0)
        {
            return;
        }

        foreach (RibbonTab tab in _ribbonTabs)
        {
            try
            {
                tab.Delete();
                Marshal.ReleaseComObject(tab);
            }
            catch (COMException ex)
            {
                Logger.Debug("COMException releasing RibbonTab.", ex);
            }
            catch (InvalidComObjectException ex)
            {
                Logger.Debug("InvalidComObjectException releasing RibbonTab.", ex);
            }
        }

        _ribbonTabs.Clear();
    }

    private void ReleaseRibbonPanels()
    {
        if (_ribbonPanels.Count == 0)
        {
            return;
        }

        foreach (RibbonPanel panel in _ribbonPanels)
        {
            try
            {
                panel.Delete();
                Marshal.ReleaseComObject(panel);
            }
            catch (COMException ex)
            {
                Logger.Debug("COMException releasing RibbonPanel.", ex);
            }
            catch (InvalidComObjectException ex)
            {
                Logger.Debug("InvalidComObjectException releasing RibbonPanel.", ex);
            }
        }

        _ribbonPanels.Clear();
    }

    private void ReleaseButtons()
    {
        if (_buttonDefinitions.Count == 0 && _buttons.Count == 0)
        {
            return;
        }

        try
        {
            // Delete definitions first — Inventor removes all associated controls automatically
            foreach (ButtonDefinition buttonDefinition in _buttonDefinitions)
            {
                try
                {
                    buttonDefinition.Delete();
                    Marshal.ReleaseComObject(buttonDefinition);
                }
                catch (COMException ex)
                {
                    Logger.Debug($"COMException deleting ButtonDefinition: {ex.Message}");
                }
                catch (InvalidComObjectException ex)
                {
                    Logger.Debug($"InvalidComObjectException deleting ButtonDefinition: {ex.Message}");
                }
            }

            // Controls were already removed by ButtonDefinition.Delete() — just release wrappers
            foreach (CommandControl commandControl in _buttons)
            {
                try
                {
                    Marshal.ReleaseComObject(commandControl);
                }
                catch (InvalidComObjectException ex)
                {
                    Logger.Debug("InvalidComObjectException releasing CommandControl.", ex);
                }
            }
        }
        finally
        {
            _buttonDefinitions.Clear();
            _buttons.Clear();
        }
    }

    private void AddToUserInterface()
    {
        Ribbon idwRibbon = Globals.InvApp.UserInterfaceManager.Ribbons["Drawing"];
        Ribbon iptRibbon = Globals.InvApp.UserInterfaceManager.Ribbons["Part"];
        Ribbon iamRibbon = Globals.InvApp.UserInterfaceManager.Ribbons["Assembly"];
        Ribbon ipnRibbon = Globals.InvApp.UserInterfaceManager.Ribbons["Presentation"];

        RibbonTab tabIdw = UiDefinitionHelper.SetupTab(Constants.AddinFolder,
            Constants.AddinFolder, idwRibbon);
        RibbonTab tabIpt = UiDefinitionHelper.SetupTab(Constants.AddinFolder,
            Constants.AddinFolder, iptRibbon);
        RibbonTab tabIam = UiDefinitionHelper.SetupTab(Constants.AddinFolder,
            Constants.AddinFolder, iamRibbon);
        RibbonTab tabIpn = UiDefinitionHelper.SetupTab(Constants.AddinFolder,
            Constants.AddinFolder, ipnRibbon);
        _ribbonTabs.Add(tabIdw);
        _ribbonTabs.Add(tabIpt);
        _ribbonTabs.Add(tabIam);
        _ribbonTabs.Add(tabIpn);

        RibbonPanel infoIdw = UiDefinitionHelper.SetupPanel("Info", "Info", tabIdw);
        RibbonPanel infoIpt = UiDefinitionHelper.SetupPanel("Info", "Info", tabIpt);
        RibbonPanel infoIam = UiDefinitionHelper.SetupPanel("Info", "Info", tabIam);
        RibbonPanel infoIpn = UiDefinitionHelper.SetupPanel("Info", "Info", tabIpn);
        _ribbonPanels.Add(infoIdw);
        _ribbonPanels.Add(infoIpt);
        _ribbonPanels.Add(infoIam);
        _ribbonPanels.Add(infoIpn);

        RibbonPanel addinPanelIdw = UiDefinitionHelper.SetupPanel("AddinCommands", "AddinCommands", tabIdw);
        RibbonPanel addinPanelIpt = UiDefinitionHelper.SetupPanel("AddinCommands", "AddinCommands", tabIpt);
        RibbonPanel addinPanelIam = UiDefinitionHelper.SetupPanel("AddinCommands", "AddinCommands", tabIam);
        RibbonPanel addinPanelIpn = UiDefinitionHelper.SetupPanel("AddinCommands", "AddinCommands", tabIpn);
        _ribbonPanels.Add(addinPanelIdw);
        _ribbonPanels.Add(addinPanelIpt);
        _ribbonPanels.Add(addinPanelIam);
        _ribbonPanels.Add(addinPanelIpn);

        if (_settingsButton != null)
        {
            CommandControl settingsButtonIdw = addinPanelIdw.CommandControls.AddButton(_settingsButton, true);
            CommandControl settingsButtonIpt = addinPanelIpt.CommandControls.AddButton(_settingsButton, true);
            CommandControl settingsButtonIam = addinPanelIam.CommandControls.AddButton(_settingsButton, true);
            CommandControl settingsButtonIpn = addinPanelIpn.CommandControls.AddButton(_settingsButton, true);
            _buttons.Add(settingsButtonIdw);
            _buttons.Add(settingsButtonIpt);
            _buttons.Add(settingsButtonIam);
            _buttons.Add(settingsButtonIpn);
        }

        if (_info != null)
        {
            CommandControl infoButtonIdw = infoIdw.CommandControls.AddButton(_info, true);
            CommandControl infoButtonIpt = infoIpt.CommandControls.AddButton(_info, true);
            CommandControl infoButtonIam = infoIam.CommandControls.AddButton(_info, true);
            CommandControl infoButtonIpn = infoIpn.CommandControls.AddButton(_info, true);
            _buttons.Add(infoButtonIdw);
            _buttons.Add(infoButtonIpt);
            _buttons.Add(infoButtonIam);
            _buttons.Add(infoButtonIpn);
        }

        if (_helpPage != null)
        {
            CommandControl helpButtonIdw = infoIdw.CommandControls.AddButton(_helpPage, true);
            CommandControl helpButtonIpt = infoIpt.CommandControls.AddButton(_helpPage, true);
            CommandControl helpButtonIam = infoIam.CommandControls.AddButton(_helpPage, true);
            CommandControl helpButtonIpn = infoIpn.CommandControls.AddButton(_helpPage, true);
            _buttons.Add(helpButtonIdw);
            _buttons.Add(helpButtonIpt);
            _buttons.Add(helpButtonIam);
            _buttons.Add(helpButtonIpn);
        }

        if (_openAttributeWindow != null)
        {
            CommandControl attributeWindowButtonIdw =
                addinPanelIdw.CommandControls.AddButton(_openAttributeWindow, true);
            CommandControl attributeWindowButtonIpt =
                addinPanelIpt.CommandControls.AddButton(_openAttributeWindow, true);
            CommandControl attributeWindowButtonIam =
                addinPanelIam.CommandControls.AddButton(_openAttributeWindow, true);
            CommandControl attributeWindowButtonIpn =
                addinPanelIpn.CommandControls.AddButton(_openAttributeWindow, true);
            _buttons.Add(attributeWindowButtonIdw);
            _buttons.Add(attributeWindowButtonIpt);
            _buttons.Add(attributeWindowButtonIam);
            _buttons.Add(attributeWindowButtonIpn);
        }

        if (_addAttributeToObject != null)
        {
            CommandControl addAttributeButtonIdw =
                addinPanelIdw.CommandControls.AddButton(_addAttributeToObject, true);
            CommandControl addAttributeButtonIpt =
                addinPanelIpt.CommandControls.AddButton(_addAttributeToObject, true);
            CommandControl addAttributeButtonIam =
                addinPanelIam.CommandControls.AddButton(_addAttributeToObject, true);
            CommandControl addAttributeButtonIpn =
                addinPanelIpn.CommandControls.AddButton(_addAttributeToObject, true);
            _buttons.Add(addAttributeButtonIdw);
            _buttons.Add(addAttributeButtonIpt);
            _buttons.Add(addAttributeButtonIam);
            _buttons.Add(addAttributeButtonIpn);
        }
    }

    private void UiEventsOnResetRibbonInterface(NameValueMap context)
    {
        AddToUserInterface();
    }

    private void InvAppEvents_OnApplicationOptionChange(EventTimingEnum beforeOrAfter, NameValueMap context, out HandlingCodeEnum handlingCode)
    {
        if (beforeOrAfter == EventTimingEnum.kAfter)
        {
            ThemeManager themeManager = Globals.InvApp.ThemeManager;
            Theme activeTheme = themeManager.ActiveTheme;
            string theme = activeTheme.Name;
            ThemeResourceHelper.ApplyInventorThemeResources();

            if (Globals.ActiveTheme.Name != theme) //check if theme has changed
            {
                Deactivate();
                OnActivate();
            }
        }

        handlingCode = HandlingCodeEnum.kEventNotHandled;
    }
}