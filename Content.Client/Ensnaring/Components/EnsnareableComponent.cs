using Content.Shared.Ensnaring.Components;

namespace Content.Client.Ensnaring.Components;
[RegisterComponent]
[ComponentReference(typeof(SharedEnsnareableComponent))]
public sealed class EnsnareableComponent : SharedEnsnareableComponent
{
    [DataField("sprite")]
    public string? Sprite;

    [DataField("state")]
    public string? State;
}
