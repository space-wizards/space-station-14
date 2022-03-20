using Robust.Client.Graphics;

namespace Content.Client.UserInterface;

public interface IHasHudTheme
{
    public HudTheme Theme { get; set; }
    public void UpdateTheme(HudTheme newTheme);
}
