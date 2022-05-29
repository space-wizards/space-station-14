using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using TerraFX.Interop.Windows;

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
