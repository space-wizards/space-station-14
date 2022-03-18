using System.Linq;
using Content.Client.HUD.Presets;
using Content.Client.Options.UI;
using Content.Client.Resources;
using Content.Shared.CCVar;
using Content.Shared.HUD;
using Robust.Client.Console;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;
using Robust.Shared.Utility;

namespace Content.Client.HUD;

public interface IHasHudConnection
{
    public void LinkHudElements(IHudManager hudManager, HudPreset preset);
    public void UnLinkHudElements(IHudManager hudManager,HudPreset preset);
}


public interface IHudManager
{
    public void Initialize();
    public void Startup();
    public void Shutdown();
    public LayoutContainer? StateRoot { get; }
    public HudPreset? ActivePreset { get; }
    public T GetHudPreset<T>() where T : HudPreset;
    public void SpawnActivePreset();
    public void DespawnActivePreset();
    public bool SwitchHudPreset<T>() where T : HudPreset;
    public bool ValidateHudTheme(int idx);
    public Texture GetHudTexture(string path);
    public bool IsWidgetShown<T>() where T : HudWidget;
    public void ShowUIWidget<T>(bool enabled) where T : HudWidget;
    public T GetUIWidget<T>() where T : HudWidget;
    //a nullsafe version of get UI widget that doesn't throw exceptions
    public T? GetUIWidgetOrNull<T>() where T : HudWidget;
    public bool HasWidget<T>() where T : HudWidget;
    public bool EscapeMenuVisible { get; set; }
    public EscapeMenu? EscapeMenu { get; }

    //Do Not abuse these or I will eat you
    public void AddNewControl(Control control);

    public void RemoveControl(Control control);

}

public sealed class HudManager  : IHudManager
{
    [Dependency] private readonly ISandboxHelper _sandboxHelper = default!;
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private HudPreset? _activeHudPreset;

    public HudPreset? ActivePreset => _activeHudPreset;
    private HudPreset DefaultHudPreset => _defaultHudPreset!;
    private HudPreset? _defaultHudPreset = null;

    private readonly Dictionary<System.Type, HudPreset?> _hudPresets = new();
    public LayoutContainer? StateRoot => _userInterfaceManager.StateRoot;
    public bool EscapeMenuVisible
    {
        get => _escapeMenu is {Visible: true};
        set
        {
            if (_escapeMenu == null) return;
            _escapeMenu.Visible = value;
        }
    }
    private EscapeMenu? _escapeMenu;

    //if escape menu is null create a new escape menu
    public EscapeMenu? EscapeMenu => _escapeMenu ?? new EscapeMenu(_consoleHost);

    public HudManager()
    {
        IoCManager.InjectDependencies(this);
    }
    public void Initialize()
    {
        RegisterHudPresets();
    }

    public void SpawnActivePreset()
    {
        if (_activeHudPreset == null) return;
        if (!_activeHudPreset.SupportsState(_stateManager.CurrentState)) return;
        if (_activeHudPreset.IsAttachedToRoot) return;
        _activeHudPreset.LoadPreset();
    }

    public void DespawnActivePreset()
    {
        if (_activeHudPreset == null) return;
        if (!_activeHudPreset.IsAttachedToRoot) return;
        _activeHudPreset.UnloadPreset();
    }

    private void SetDefaultHudPreset<T>() where T: HudPreset
    {
        if (_hudPresets.TryGetValue(typeof(T), out var preset))
        {
            _defaultHudPreset = preset;
            return;
        }
        _defaultHudPreset = _hudPresets.First().Value;
    }

    public void Startup()
    {
        LoadAllPresets();
        SetDefaultHudPreset<DefaultHud>();
        _activeHudPreset = _defaultHudPreset;
        _escapeMenu = new EscapeMenu(_consoleHost);
        EscapeMenuVisible = false;
    }

    public void Shutdown()
    {
        _escapeMenu?.Dispose();
        foreach (var preset in _hudPresets)
        {
            preset.Value?.Dispose();
        }
    }

    private void RegisterHudPresets()
    {
        var presetTypes = _reflectionManager.GetAllChildren<HudPreset>().ToList();
        if (presetTypes.Count == 0) throw new NullReferenceException("No Hud presets found!");
        foreach (var presetType in presetTypes)
        {
            _hudPresets[presetType] = null;
        }
    }

    private void LoadAllPresets()
    {
        foreach (var presetData in _hudPresets)
        {
            var hudPreset = (HudPreset) _sandboxHelper.CreateInstance(presetData.Key);
            _hudPresets[presetData.Key] = hudPreset;
            hudPreset.Initialize();
        }
    }

    public bool HasWidget<T>() where T : HudWidget
    {
        return _activeHudPreset!.HasWidget<T>();
    }

    public T GetHudPreset<T>() where T : HudPreset
    {
        return (T) _hudPresets[typeof(T)]!;
    }

    public bool SwitchHudPreset<T>() where T : HudPreset
    {
        //Don't double set the hud preset
        if (_activeHudPreset!.GetType() == typeof(T)) return false;
        //unload the current preset if it's loaded
        if (_activeHudPreset.IsAttachedToRoot) _activeHudPreset.UnloadPreset();
        //update the active preset
        _activeHudPreset = _hudPresets[typeof(T)]!;
        //if the current state supports the preset load it
        if (_activeHudPreset.SupportsState(_stateManager.CurrentState)) _activeHudPreset.LoadPreset();
        return true;
    }

    public void AddNewControl(Control control)
    {
        StateRoot?.AddChild(control);
    }

    public void RemoveControl(Control control)
    {
        StateRoot?.RemoveChild(control);
    }

    public bool ValidateHudTheme(int idx)
    {
        if (!_prototypeManager.TryIndex(idx.ToString(), out HudThemePrototype? _))
        {
            Logger.ErrorS("hud", "invalid HUD theme id {0}, using different theme",
                idx);
            var proto = _prototypeManager.EnumeratePrototypes<HudThemePrototype>().FirstOrDefault();
            if (proto == null)
            {
                throw new NullReferenceException("No valid HUD prototypes!");
            }
            var id = int.Parse(proto.ID);
            _configManager.SetCVar(CCVars.HudTheme, id);
            return false;
        }
        return true;
    }

    public Texture GetHudTexture(string path)
    {
        var id = _configManager.GetCVar<int>("hud.theme");
        var dir = string.Empty;
        if (!_prototypeManager.TryIndex(id.ToString(), out HudThemePrototype? proto))
        {
            throw new ArgumentOutOfRangeException();
        }
        dir = proto.Path;

        var resourcePath = (new ResourcePath("/Textures/Interface/Inventory") / dir) / path;
        return _resourceCache.GetTexture(resourcePath);
    }

    public bool IsWidgetShown<T>() where T: HudWidget
    {
        return _activeHudPreset!.IsWidgetShown<T>();
    }

    //Grabs a reference to the specified widget from the HUD
    public void ShowUIWidget<T>(bool enabled) where T : HudWidget
    {
        _activeHudPreset!.ShowWidget<T>(enabled);
    }

    public T GetUIWidget<T>() where T : HudWidget
    {
        var widget = _activeHudPreset!.GetWidget<T>();
        if (widget == null) throw new NullReferenceException(typeof(T).Name + " was not found in active Hud preset!");
        return widget;
    }

    public T? GetUIWidgetOrNull<T>() where T : HudWidget
    {
        return _activeHudPreset!.GetWidget<T>();
    }
}
