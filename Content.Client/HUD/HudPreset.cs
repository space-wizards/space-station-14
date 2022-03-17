using Robust.Client.UserInterface;
using Robust.Shared.Sandboxing;
using TerraFX.Interop.Windows;

namespace Content.Client.HUD;

[Virtual]
public abstract class HudPreset
{
    [Dependency] private readonly IHudManager _hudManager = default!;
    [Dependency] private readonly ISandboxHelper _sandboxHelper = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    protected abstract void DefineWidgetsAndLinkedSystems();

    private readonly Dictionary<System.Type, HudWidget> _widgets = new();
    private readonly List<System.Type> _linkedEntitySystemTypes = new();
    private readonly Control _presetRoot;
    public Control RootContainer => _presetRoot;
    protected HudPreset()
    {
        IoCManager.InjectDependencies(this);
        _presetRoot = new Control();
        _presetRoot.Name = this.GetType().Name;
    }
    internal void Initialize()
    {
        DefineWidgetsAndLinkedSystems();
        foreach (var widgetData in _widgets)
        {
            _presetRoot.AddChild(widgetData.Value);
        }
        foreach (var systemType in _linkedEntitySystemTypes)
        {
            ((IHasHudConnection)_entitySystemManager.GetEntitySystem(systemType)).LinkHudElements(_hudManager);
        }
    }


    internal void LoadPreset()
    {
        _hudManager.AddNewControl(_presetRoot);
    }

    internal void UnloadPreset()
    {
        _hudManager.RemoveControl(_presetRoot);
    }

    //register a new hud widget in this preset, internal use only
    protected void RegisterWidget<T>() where T: HudWidget, new()
    {
        if (_widgets.ContainsKey(typeof(T))) return;
        _widgets[typeof(T)] = (T)_sandboxHelper.CreateInstance(typeof(T));
    }

    public bool HasWidget<T>() where T : HudWidget
    {
        return _widgets.ContainsKey(typeof(T));
    }


    //get a hud widget from this preset by type
    public T? GetWidget<T>() where T : HudWidget
    {
        return _hudManager.GetUIWidget<T>();
    }

    public bool IsWidgetShown<T>() where T : HudWidget
    {
        return _widgets.TryGetValue(typeof(T), out var widget) && widget.Visible;
    }
    //show/hide a hud widget by type
    public void ShowWidget<T>(bool show) where T : HudWidget
    {
        _hudManager.ShowUIWidget<T>(show);
    }

    //dispose of this preset, this should not be manually called
    internal void Dispose()
    {
        foreach (var systemType in _linkedEntitySystemTypes)
        {
            ((IHasHudConnection)_entitySystemManager.GetEntitySystem(systemType)).UnLinkHudElements(_hudManager);
        }
        UnloadPreset();
        foreach (var widget in _widgets)
        {
            widget.Value?.Dispose();
        }
        _presetRoot.Dispose();
    }

    protected void RegisterLinkedEntitySystem<T>() where T : IEntitySystem, IHasHudConnection
    {
        _linkedEntitySystemTypes.Add(typeof(T));
    }

}
