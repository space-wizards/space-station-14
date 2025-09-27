using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Actions.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpawnOnActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Action = "Spawn";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField(required: true)]
    public EntProtoId EntityToSpawn;
}