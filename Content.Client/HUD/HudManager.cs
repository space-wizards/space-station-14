using Content.Client.Options.UI;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;

namespace Content.Client.HUD;


public interface IHudManager
{
    public void Initialize();
    public void Startup();
    public void Shutdown();
    public void ShowUIWidget<T>(bool enabled) where T : HudWidget;
    public void AddGameHudWidget<T>() where T : HudWidget;
    public void RemoveGameHudWidget<T>() where T : HudWidget;
    public T? GetUIWidget<T>() where T : HudWidget;
    public EscapeMenu? EscapeMenu { get; }

}

public sealed class HudManager  : IHudManager
{
    [Dependency] private readonly ISandboxHelper _sandboxHelper = default!;
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    private readonly Dictionary<System.Type, Control?> _gameHudWidgets = new();

    public LayoutContainer? StateRoot => _userInterfaceManager.StateRoot;

    private EscapeMenu? _escapeMenu = default!;

    //if escape menu is null create a new escape menu
    public EscapeMenu? EscapeMenu => _escapeMenu ?? new EscapeMenu(_consoleHost);

    public HudManager()
    {
        IoCManager.InjectDependencies(this);
    }

    public void Initialize()
    {
        RegisterHudWidgets();
    }

    public void Startup()
    {
        foreach (var key in _gameHudWidgets.Keys)
        {
            //prevent double initialization if widgets are already initialized for some reason
            if (_gameHudWidgets[key] != null) continue;
            var control = (Control)_sandboxHelper.CreateInstance(key);
            _gameHudWidgets[key] = control;
            _userInterfaceManager.StateRoot.AddChild(control);
        }
        _escapeMenu = new EscapeMenu(_consoleHost);
    }

    public void Shutdown()
    {
        foreach (var widgetData in _gameHudWidgets)
        {
            Internal_RemoveHudWidget(widgetData.Key);
        }

        _escapeMenu?.Dispose();
    }

    public void ShowUIWidget<T>(bool enabled) where T : HudWidget
    {
        var widget = _gameHudWidgets[typeof(T)];
        if (widget == null) return;
        widget.Visible = enabled;
    }

    //uses reflection to automatically register all widget types to the hud
    private void RegisterHudWidgets()
    {
        {
            var widgetTypes = _reflectionManager.GetAllChildren<HudWidget>();
            foreach (var widgetType in widgetTypes)
            {
                _gameHudWidgets[widgetType] = null;
            }
        }
    }

    //adds a new hud widget to the hud
    public void AddGameHudWidget<T>() where T : HudWidget
    {
        if (_gameHudWidgets.ContainsKey(typeof(T)))
        {
            //if the widget is registered with the hud but not initialized, initialize it
            _gameHudWidgets[typeof(T)] ??= (Control) _sandboxHelper.CreateInstance(typeof(T));
        }
        else
        {
            //if the widget is not registered, then register and initialize it
            var control = (Control)_sandboxHelper.CreateInstance(typeof(T));
            _gameHudWidgets[typeof(T)] = control;
            _userInterfaceManager.StateRoot.AddChild(control);
        }
    }
    //internal function to remove a widget from the gamehud without de-registering it
    private void Internal_RemoveHudWidget(System.Type type)
    {
        var widget = _gameHudWidgets[type];
        //return out if the control is null
        if (widget == null) return;
        _userInterfaceManager.StateRoot.RemoveChild(widget);
        widget.Dispose();
    }

    //Remove a widget from the game hud
    public void RemoveGameHudWidget<T>() where T : HudWidget
    {
        Internal_RemoveHudWidget(typeof(T));
    }

    //Grabs a reference to the specified widget from the HUD
    public T? GetUIWidget<T>() where T : HudWidget
    {
        if (!_gameHudWidgets.TryGetValue(typeof(T), out var widget)) return null;
        //return a null control if it isn't found
        if (widget == null) return null;
        return (T)widget;
    }
}
