using System.Linq;
using Content.Client.HUD.Presets;
using Content.Client.Options.UI;
using Content.Client.Resources;
using Content.Shared.CCVar;
using Content.Shared.HUD;
using Robust.Client.Console;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
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
    public void LinkHudElements(IHudManager hudManager);
    public void UnLinkHudElements(IHudManager hudManager);
}


public interface IHudManager
{
    public void Initialize();
    public void Startup();
    public void Shutdown();
    public LayoutContainer? StateRoot { get; }
    public HudPreset? ActivePreset { get; }
    public T GetHudPreset<T>() where T : HudPreset;
    public bool SwitchHudPreset<T>() where T : HudPreset;
    public bool ValidateHudTheme(int idx);
    public Texture GetHudTexture(string path);
    public bool IsWidgetShown<T>() where T : HudWidget;
    public void ShowUIWidget<T>(bool enabled) where T : HudWidget;
    public T GetUIWidget<T>() where T : HudWidget;
    //a nullsafe version of get UI widget that doesn't throw exceptions
    public T? GetUIWidgetOrNull<T>() where T : HudWidget;
    public bool HasWidget<T>() where T : HudWidget;
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
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private HudPreset? _activeHudPreset;

    public HudPreset? ActivePreset => _activeHudPreset;
    private readonly HudPreset _defaultHudPreset = new DefaultHud();

    private readonly Dictionary<System.Type, HudPreset> _hudPresets = new();
    public LayoutContainer? StateRoot => _userInterfaceManager.StateRoot;

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
        _activeHudPreset = _defaultHudPreset;
        _escapeMenu = new EscapeMenu(_consoleHost);
    }

    public void Startup()
    {
        _userInterfaceManager.StateRoot.AddChild(_escapeMenu!);
        _activeHudPreset!.LoadPreset();//This will never be null at runtime
    }

    public void Shutdown()
    {
        _escapeMenu?.Dispose();
        foreach (var preset in _hudPresets)
        {
            preset.Value.Dispose();
        }
    }

    private void RegisterHudPresets()
    {
        var presetTypes = _reflectionManager.GetAllChildren<HudPreset>().ToList();
        if (presetTypes.Count == 0) throw new NullReferenceException("No Hud presets found!");
        foreach (var presetType in presetTypes)
        {
            var hudPreset = (HudPreset) _sandboxHelper.CreateInstance(presetType);
            _hudPresets[presetType] = hudPreset;
            hudPreset.Initialize();
        }
        _activeHudPreset = _hudPresets[presetTypes[0]]; //by default set the hud preset to the first type found.
    }

    public bool HasWidget<T>() where T : HudWidget
    {
        return _activeHudPreset!.HasWidget<T>();
    }

    public T GetHudPreset<T>() where T : HudPreset
    {
        return (T) _hudPresets[typeof(T)];
    }

    public bool SwitchHudPreset<T>() where T : HudPreset
    {
        if (_activeHudPreset!.GetType() == typeof(T)) return false;
        _activeHudPreset.UnloadPreset();
        _activeHudPreset = _hudPresets[typeof(T)];
        _activeHudPreset.LoadPreset();
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
