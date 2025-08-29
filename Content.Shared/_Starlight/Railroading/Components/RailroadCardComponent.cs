using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._Starlight.Railroading;

[RegisterComponent]
public sealed partial class RailroadCardComponent : Component
{
    [DataField(required: true)]
    public string Title;

    [DataField(required: true)]
    public string Description;

    [DataField(required: true)]
    public string Icon;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public Color IconColor = Color.White;

    [DataField]
    public Texture? Image; // This thing just for single images, list for random

    [DataField]
    public List<Texture> Images = [];
}
