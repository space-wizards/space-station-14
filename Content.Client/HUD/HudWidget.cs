using Content.Client.Viewport;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

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
}
