using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.SolutionAppearanceRelay;

[RegisterComponent]
public sealed partial class SolutionAppearanceRelayComponent : Component
{
    [DataField(required: true)]
    public string Solution;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}

[Serializable, NetSerializable]
public enum SolutionAppearanceRelayedVisuals : byte
{
    HasRelay
}
