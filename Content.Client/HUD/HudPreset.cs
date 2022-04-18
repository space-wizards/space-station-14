using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Sandboxing;

namespace Content.Client.HUD;

[Virtual]
public abstract class HudPreset
{
    [Dependency] private readonly IHudManager _hudManager = default!;
    [Dependency] private readonly ISandboxHelper _sandboxHelper = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    protected abstract void DefinePreset();
    protected virtual Thickness Margins => new Thickness(0);
    private readonly Dictionary<Type, HudWidget> _widgets = new();
    private readonly HashSet<Type> _allowedStates = new();
    private readonly List<Type> _linkedEntitySystemTypes = new();
    private readonly Control _presetRoot;
    private bool _isAttachedToRoot = false;
    public bool IsAttachedToRoot => _isAttachedToRoot;
    public Control RootContainer => _presetRoot;
    protected HudPreset()
    {
        IoCManager.InjectDependencies(this);
        _presetRoot = new Control()
        {
            Name = GetType().Name+"_HudPreset"
        };
        _presetRoot.MouseFilter = Control.MouseFilterMode.Ignore;
    }
    internal void Initialize()
    {
        _presetRoot.Margin = Margins;
        DefinePreset();
        foreach (var widgetData in _widgets)
        {
            _presetRoot.AddChild(widgetData.Value);
        }
        foreach (var systemType in _linkedEntitySystemTypes)
        {
            ((IHasHudConnection)_entitySystemManager.GetEntitySystem(systemType)).LinkHudElements(_hudManager, this);
        }
    }

    public bool SupportsState(State state)
    {
        return _allowedStates.Contains(state.GetType());
    }

    private void OnResized()
    {
        _presetRoot.SetSize = _hudManager.StateRoot!.Size;
        _presetRoot.Arrange(_hudManager.StateRoot.Rect);
    }


    internal void LoadPreset()
    {
        _hudManager.AddNewControl(_presetRoot);
        _hudManager.StateRoot!.OnResized += OnResized;
        _presetRoot.SetSize = _hudManager.StateRoot!.Size;
        _presetRoot.Margin = Margins;
        _isAttachedToRoot = true;
    }

    internal void UnloadPreset()
    {
        _hudManager.StateRoot!.OnResized -= OnResized;
        _hudManager.RemoveControl(_presetRoot);
        _isAttachedToRoot = false;
    }

    //register a new hud widget in this preset, internal use only
    protected T RegisterWidget<T>() where T: HudWidget, new()
    {
        if (_widgets.ContainsKey(typeof(T))) throw new Exception("Hud Widget not found");
        var newWidget = (T)_sandboxHelper.CreateInstance(typeof(T));
        _widgets[typeof(T)] = newWidget;
        return newWidget;
    }

    public bool HasWidget<T>() where T : HudWidget
    {
        return _widgets.ContainsKey(typeof(T));
    }
    //get a hud widget from this preset by type
    public T? GetWidgetOrNull<T>() where T : HudWidget
    {
        return (T?) _widgets.GetValueOrDefault(typeof(T));
    }

    //get a hud widget from this preset by type
    public T GetWidget<T>() where T : HudWidget
    {
        return (T) _widgets[typeof(T)];
    }

    //show/hide a hud widget by type
    public void ShowWidget<T>(bool show) where T : HudWidget
    {
        _widgets[typeof(T)].Visible = show;
    }

    public bool IsWidgetShown<T>() where T : HudWidget
    {
        return _widgets.TryGetValue(typeof(T), out var widget) && widget.Visible;
    }


    //dispose of this preset, this should not be manually called
    internal void Dispose()
    {
        foreach (var systemType in _linkedEntitySystemTypes)
        {
            ((IHasHudConnection)_entitySystemManager.GetEntitySystem(systemType)).UnLinkHudElements(_hudManager, this);
        }
        UnloadPreset();
        foreach (var widget in _widgets)
        {
            widget.Value?.Dispose();
        }
        _presetRoot.Dispose();
    }

    protected void RegisterAllowedState<T>() where T : State
    {
        if (_allowedStates.Contains(typeof(T))) return;
        _allowedStates.Add(typeof(T));
    }

    protected void RegisterLinkedEntitySystem<T>() where T : IEntitySystem, IHasHudConnection
    {
        _linkedEntitySystemTypes.Add(typeof(T));
    }

}
