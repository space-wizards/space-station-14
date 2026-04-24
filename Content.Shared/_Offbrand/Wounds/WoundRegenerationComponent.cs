using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class WoundRegenerationComponent : Component
{
    /// <summary>
    /// Damage to attempt to heal every interval
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public float UpdateIntervalMultiplier = 1f;

    [ViewVariables]
    public TimeSpan AdjustedUpdateInterval => UpdateInterval * UpdateIntervalMultiplier;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    [AutoNetworkedField]
    public TimeSpan? LastUpdate;
}
