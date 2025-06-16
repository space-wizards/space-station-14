namespace Content.Shared.Starlight.EntityEffects.Components;

[RegisterComponent]
public sealed partial class ThermiteComponent : Component
{
    [DataField("tag")]
    public string? requiredTag = null;
}