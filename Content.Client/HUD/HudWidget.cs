using Content.Client.Viewport;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using TerraFX.Interop.Windows;
namespace Content.Client.HUD;


[Virtual]
//stop people from trying to hardcode widgets :D, use XAML!
public abstract class HudWidget : BoxContainer
{
    [Dependency] protected readonly IHudManager HudManager = default!;
    protected LayoutContainer? Layout => HudManager.StateRoot;
    protected HudWidget()
    {
        IoCManager.InjectDependencies(this);
    }

    //Layout functions. They all return a reference to the widget so that they can be daisy chained
    public HudWidget SetAnchorAndMarginPreset(LayoutContainer.LayoutPreset preset,
        LayoutContainer.LayoutPresetMode presetMode = LayoutContainer.LayoutPresetMode.MinSize, int margin = 0)
    {
        LayoutContainer.SetAnchorAndMarginPreset(this, preset, presetMode, margin);
        return this;
    }



    public HudWidget SetLayoutPosition(Vector2 pos)
    {
        LayoutContainer.SetPosition(this, pos);
        return this;
    }
    public HudWidget SetGrowHorizontalDirection(LayoutContainer.GrowDirection direction)
    {
        LayoutContainer.SetGrowHorizontal(this, direction);
        return this;
    }
    public HudWidget SetGrowVerticalDirection(LayoutContainer.GrowDirection direction)
    {
        LayoutContainer.SetGrowVertical(this, direction);
        return this;
    }
}
