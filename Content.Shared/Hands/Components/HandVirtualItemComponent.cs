using Robust.Shared.GameStates;

namespace Content.Shared.Hands.Components;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class HandVirtualItemComponent : Component
{
    /// <summary>
    ///     The entity blocking this hand.
    /// </summary>
    [DataField("blockingEntity"), AutoNetworkedField]
    public EntityUid BlockingEntity;
}
