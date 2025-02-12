using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controls;

public sealed class HSpacer : Control
{
    public float Spacing { get => MinHeight; set => MinHeight = value; }
    public HSpacer()
    {
        MinHeight = Spacing;
    }
    public HSpacer(float height = 5)
    {
        Spacing = height;
        MinHeight = height;
    }
}
