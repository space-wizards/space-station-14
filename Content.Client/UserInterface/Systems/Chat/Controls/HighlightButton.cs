using System.Numerics;
using Content.Client.Resources;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class HighlightButton : ChatPopupButton<HighlightPopup>
{
    private readonly TextureRect? _textureRect;

    public HighlightButton()
    {
        var highlightTexture = IoCManager.Resolve<IResourceCache>()
            .GetTexture("/Textures/Interface/Nano/highlight-button.png");

        AddChild(
            (_textureRect = new TextureRect
            {
                Texture = highlightTexture,
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
            })
        );
    }
    protected override UIBox2 GetPopupPosition()
    {
        var globalPos = GlobalPosition;
        var (minX, minY) = Popup.MinSize;
        return UIBox2.FromDimensions(
            globalPos,
            new Vector2(Math.Max(minX, Popup.MinWidth), minY));
    }
}
