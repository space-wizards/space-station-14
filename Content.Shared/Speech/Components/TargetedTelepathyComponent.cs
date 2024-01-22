using Robust.Shared.GameStates;

namespace Content.Shared.Speech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TargetedTelepathyComponent : Component
{
    /// <summary>
    /// The target of the telepathic speaker. Only the target will be able to hear speech from this entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    [AutoNetworkedField]
    public EntityUid Target;
}
