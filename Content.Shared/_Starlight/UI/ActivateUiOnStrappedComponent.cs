namespace Content.Shared._Starlight;

[RegisterComponent]
public sealed partial class ActivateUiOnStrappedComponent : Component
{
    [DataField(required: true)]
    public Enum Key;
}