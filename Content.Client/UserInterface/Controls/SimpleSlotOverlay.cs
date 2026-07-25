using System.Numerics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// Simple overlay for slots. Used for stuff like admin overlays to indicate a status
/// </summary>
public sealed class SimpleSlotOverlay : TextureRect
{
    public SimpleSlotOverlay(string texturePath, Color? color = null)
    {
        TexturePath = texturePath;
        TextureScale = new Vector2(2, 2);
        SetSize = new Vector2(SlotControl.DefaultButtonSize);

        if (color != null)
            Modulate = color.Value;
    }
}
