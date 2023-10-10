// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Content.Shared.Interaction;

namespace Content.Shared.SS220.Cart.Components;

[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
[Access(typeof(CartSystem))]
public sealed partial class CartComponent : Component
{
    /// <summary>
    /// The entity which the cart is currently attached to
    /// </summary>
    [ViewVariables]
    [AutoNetworkedField]
    public EntityUid? Puller;

    /// <summary>
    /// Whether the cart is currently attached or not
    /// </summary>
    [ViewVariables]
    [AutoNetworkedField]
    public bool IsAttached;

    /// <summary>
    /// Time required for attaching/deattaching
    /// </summary>
    [DataField("attachToggleTime")]
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float AttachToggleTime = .3f;

    /// <summary>
    /// The range from which this cart can be attached to a <see cref="CartPullerComponent"/>
    /// </summary
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float AttachRange = SharedInteractionSystem.InteractionRange / 1.4f; // Also stolen from BuckleComponent
}
