using ExtrabbitCode.Inventor.Attributes.Helper;
using ExtrabbitCode.Inventor.Attributes.Models;
using ExtrabbitCode.Inventor.Attributes.UI;
using log4net;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Wpf.Ui.Appearance;

namespace ExtrabbitCode.Inventor.Attributes
{
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

        private ButtonDefinition? _defaultButton;
        private ButtonDefinition? _info;

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
                UiEvents = Globals.InvApp.UserInterfaceManager.UserInterfaceEvents;
                InvAppEvents = Globals.InvApp.ApplicationEvents;
                InvAppEvents.OnApplicationOptionChange += InvAppEvents_OnApplicationOptionChange;

                ThemeManager themeManager = Globals.InvApp.ThemeManager;
                Globals.ActiveTheme = themeManager.ActiveTheme;
                string themeName = Globals.ActiveTheme.Name;
                Logger.Debug("Inventor ThemeManager ActiveTheme: " + themeName);

                ApplicationTheme appTheme = themeName == InventorThemeConstants.LightTheme
                    ? ApplicationTheme.Light
                    : ApplicationTheme.Dark;

                // Force initialize Wpf.Ui before any dialog opens
                ApplicationThemeManager.Apply(appTheme);

                _info = UiDefinitionHelper.CreateButton("Info", "ExtrabbitCode.Inventor.Attributes.Info", @"UI\ButtonResources\Info", themeName);
                _defaultButton = UiDefinitionHelper.CreateButton("DefaultButton", "ExtrabbitCode.Inventor.Attributes.DefaultButton", @"UI\ButtonResources\DefaultButton", themeName);
                _buttonDefinitions.Add(_info);
                _buttonDefinitions.Add(_defaultButton);

                if (FirstTime)
                {
                    AddToUserInterface();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    @"Unexpected failure during activation of the add-in 'InventorTemplate'.",
                    ex
                );
            }
        }

        public override void OnDeactivate()
        {
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
                foreach (ButtonDefinition buttonDefinition in _buttonDefinitions)
                {
                    try
                    {
                        buttonDefinition.Delete();
                        Marshal.ReleaseComObject(buttonDefinition);
                    }
                    catch (COMException ex)
                    {
                        Logger.Debug("COMException releasing ButtonDefinition.", ex);
                    }
                    catch (InvalidComObjectException ex)
                    {
                        Logger.Debug("InvalidComObjectException releasing ButtonDefinition.", ex);
                    }
                }

                foreach (CommandControl commandControl in _buttons)
                {
                    try
                    {
                        commandControl.Delete();
                        Marshal.ReleaseComObject(commandControl);
                    }
                    catch (COMException ex)
                    {
                        Logger.Debug("COMException releasing CommandControl.", ex);
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

            RibbonTab tabIdw = UiDefinitionHelper.SetupTab("ExtrabbitCode.Inventor.Attributes", "ExtrabbitCode.Inventor.Attributes", idwRibbon);
            RibbonTab tabIpt = UiDefinitionHelper.SetupTab("ExtrabbitCode.Inventor.Attributes", "ExtrabbitCode.Inventor.Attributes", iptRibbon);
            RibbonTab tabIam = UiDefinitionHelper.SetupTab("ExtrabbitCode.Inventor.Attributes", "ExtrabbitCode.Inventor.Attributes", iamRibbon);
            RibbonTab tabIpn = UiDefinitionHelper.SetupTab("ExtrabbitCode.Inventor.Attributes", "ExtrabbitCode.Inventor.Attributes", ipnRibbon);
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

            if (_defaultButton != null)
            {
                CommandControl defaultButtonIdw = addinPanelIdw.CommandControls.AddButton(_defaultButton, true);
                CommandControl defaultButtonIpt = addinPanelIpt.CommandControls.AddButton(_defaultButton, true);
                CommandControl defaultButtonIam = addinPanelIam.CommandControls.AddButton(_defaultButton, true);
                CommandControl defaultButtonIpn = addinPanelIpn.CommandControls.AddButton(_defaultButton, true);
                _buttons.Add(defaultButtonIdw);
                _buttons.Add(defaultButtonIpt);
                _buttons.Add(defaultButtonIam);
                _buttons.Add(defaultButtonIpn);
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

                if (Globals.ActiveTheme.Name != theme) //check if theme has changed
                {
                    Deactivate();
                    OnActivate();
                }
            }

            handlingCode = HandlingCodeEnum.kEventNotHandled;
        }
    }
}