using Content.Client.Alerts.UI;
using Content.Client.Chat;
using Content.Client.Chat.Managers;
using Content.Client.Chat.UI;
using Content.Client.Construction.UI;
using Content.Client.Hands;
using Content.Client.HUD;
using Content.Client.HUD.UI;
using Content.Client.Voting;
using Content.Shared.Chat;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.Viewport;

public interface IGameHud
{
    public void AddGameHudWidget<T>() where T : HudWidget;
    public void RemoveGameHudWidget<T>() where T : HudWidget;
    public T? GetUIWidget<T>() where T : HudWidget;
}

public sealed partial class GameplayState : IGameHud
{
    public GameplayState()
    {
        RegisterHudWidgets();
    }
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;

    private readonly Dictionary<System.Type, Control?> _gameHudWidgets = new();
    private void InitializeGameHud()
    {
        foreach (var key in _gameHudWidgets.Keys)
        {
            //prevent double initialization if widgets are already initialized for some reason
            if (_gameHudWidgets[key] != null) continue;
            var control = (Control)_sandboxHelper.CreateInstance(key);
            _gameHudWidgets[key] = control;
            _userInterfaceManager.StateRoot.AddChild(control);
        }
    }
    private void ShutDownGameHud()
    {
        foreach (var widgetData in _gameHudWidgets)
        {
            Internal_RemoveHudWidget(widgetData.Key);
        }
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
        if (!Initialized) throw new System.Exception("Tried to add a widget before GameHud was initialized!");

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
        widget?.Dispose();
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
