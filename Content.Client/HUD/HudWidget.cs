using Content.Client.Viewport;
using Robust.Client.UserInterface;

namespace Content.Client.HUD;


[Virtual]
//stop people from trying to hardcode widgets :D, use XAML!
public abstract class HudWidget : Control
{
    [Dependency] protected IHudManager HudManager = default!;
    protected HudWidget()
    {
        IoCManager.InjectDependencies(this);
    }
}
