namespace Content.Shared.Maps.Components;

[RegisterComponent]
public sealed partial class SharedPausedMapStorageComponent : Component
{
    [DataField]
    public EntityUid Proxy;
}
