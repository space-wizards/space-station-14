using Robust.Shared.Sandboxing;

namespace Content.Client.HUD;

[Virtual]
public abstract class HudPreset
{
    [Dependency] private readonly IHudManager _hudManager = default!;

    public abstract void DefineWidgets();

    private readonly Dictionary<System.Type, HudWidget> _widgets = new();
    protected HudPreset()
    {
        IoCManager.InjectDependencies(this);
    }

    public void LoadPreset()
    {
        foreach (var widgetData in _widgets)
        {
            _hudManager.AddNewControl(widgetData.Value);
        }
    }

    public void UnloadPreset()
    {
        foreach (var widgetData in _widgets)
        {
            _hudManager.RemoveControl(widgetData.Value);
        }
    }

    //register a new hud widget in this preset, internal use only
    protected void RegisterWidget(HudWidget widget)
    {
        _widgets[widget.GetType()] = widget;
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

    //dispose of this preset
    public void Dispose()
    {
        UnloadPreset();
        foreach (var widget in _widgets)
        {
            widget.Value?.Dispose();
        }
    }


}
