using Robust.Client.UserInterface;

namespace Content.Client.HUD;

public interface IHudWidget
{
    public Control Root { get; }
    public void Dispose();
}
