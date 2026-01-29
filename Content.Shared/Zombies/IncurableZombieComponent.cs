using Robust.Shared.Prototypes;

namespace Content.Shared.Zombies;

/// <summary>
/// This is used for a zombie that cannot be cured by any methods. Gives a succumb to zombie infection action.
/// </summary>
/// <remarks> We don't network this component for anti-cheat purposes.</remarks>
[RegisterComponent]
public sealed partial class IncurableZombieComponent : Component
{
    [DataField]
    public EntProtoId ZombifySelfActionPrototype = "ActionTurnUndead";

    [DataField]
    public EntityUid? Action;
}
