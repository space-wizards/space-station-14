using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.TypingIndicator;

/// <summary>
///     Used to display typing status bubble
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(SharedTypingIndicatorSystem))]
public sealed partial class TypingIndicatorComponent : Component
{
    /// <summary>
    ///     Status icon prototype that will be displayed as typing indicator
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<TypingIndicatorPrototype>))]
    [AutoNetworkedField]
    public string Prototype = SharedTypingIndicatorSystem.InitialIndicatorId;

    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TypingStatus Status = TypingStatus.None;
}
