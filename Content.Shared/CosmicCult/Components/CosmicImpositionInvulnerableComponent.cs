using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Component for displaying Vacuous Imposition's visuals on a player.
/// </summary>
[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class CosmicImpositionInvulnerableComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan Expiry;
}
