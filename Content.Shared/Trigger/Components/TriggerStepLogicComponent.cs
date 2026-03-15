
using Content.Shared.Trigger.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(TriggerSystem))]
public sealed partial class TriggerStepLogicComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> Colliding = new();

    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> CurrentlySteppedOn = new();

    [DataField, AutoNetworkedField]
    public bool Active = true;

    [DataField, AutoNetworkedField]
    public float IntersectRatio = 0.3f;

    [DataField, AutoNetworkedField]
    public float RequiredTriggeredSpeed = 3.5f;

    [DataField, AutoNetworkedField]
    public bool IgnoreWeightless;
}
