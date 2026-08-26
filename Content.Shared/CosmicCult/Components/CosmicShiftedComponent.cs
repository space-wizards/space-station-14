using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Marker component for entities under the effect of Astral Shift.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CosmicShiftedComponent : Component
{
    public DoAfterId? ReturnDoAfter;

    public MapCoordinates DepartureCoordinates;

    [DataField] public EntProtoId CosmicReturnAction = "ActionCosmicReturn";

    [DataField] public EntityUid? CosmicReturnActionActionEntity;

    [DataField, AutoNetworkedField] public bool ReadyToReturn = true;

    [DataField, AutoNetworkedField] public bool Occupied;
}
