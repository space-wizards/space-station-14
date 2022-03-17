using Robust.Client.UserInterface;
using Robust.Shared.Sandboxing;

namespace Content.Client.HUD;

[Virtual]
public abstract class HudPreset
{
    [Dependency] private readonly IHudManager _hudManager = default!;

    protected abstract void DefineWidgets();
    private readonly Dictionary<System.Type, HudWidget> _widgets = new();
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
        DefineWidgets();
        foreach (var widgetData in _widgets)
        {
            _presetRoot.AddChild(widgetData.Value);
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
        _widgets[typeof(T)] = new T();
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
        UnloadPreset();
        foreach (var widget in _widgets)
        {
            widget.Value?.Dispose();
        }
        _presetRoot.Dispose();
    }


}
