using Content.Shared.Damage;

namespace Content.Shared.KillTome;

/// <summary>
/// Paper with that component is KillTome.
/// </summary>
[RegisterComponent]
public sealed partial class KillTomeComponent : Component
{
    // if delay is not specified, it will use this default value
    [DataField]
    public float DefaultKillDelay = 40f;

    [DataField]
    public DamageSpecifier Damage;

    // to keep a track of already killed people so they won't be killed again
    public HashSet<EntityUid> KilledEntities = [];
}
