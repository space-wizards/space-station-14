using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Chat.TypingIndicator;

/// <summary>
///     If an item is equipped to someones inventory (Anything but the pockets), and has this component
///     the users typing indicator will be replaced by the prototype given in <c>TypingIndicatorPrototype</c>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(SharedTypingIndicatorSystem))]
public sealed partial class TypingIndicatorClothingComponent : Component
{
    /// <summary>
    ///     The typing indicator that will override the default typing indicator when the item is equipped to a users
    ///     inventory.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("proto", required: true)]
    public ProtoId<TypingIndicatorPrototype> TypingIndicatorPrototype = default!;

    /// <summary>
    ///     This stores the time the item was equipped in someones inventory. If null, item is currently not equipped.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan? GotEquippedTime = null;
}
