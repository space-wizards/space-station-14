using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Controls;

public sealed class VSpacer : Control
{
    public float Spacing{ get => MinWidth; set => MinWidth = value; }
    public VSpacer()
    {
        MinWidth = Spacing;
    }
    public VSpacer(float width = 5)
    {
        Spacing = width;
        MinWidth = width;
    }
}
