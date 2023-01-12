using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Chat.TypingIndicator;

/// <summary>
///     Show typing indicator icon when player typing text in chat box.
///     Added automatically when player poses entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTypingIndicatorSystem))]
public sealed class TypingIndicatorComponent : Component
{
    /// <summary>
    ///     Prototype id that store all visual info about typing indicator.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("proto", customTypeSerializer: typeof(PrototypeIdSerializer<TypingIndicatorPrototype>))]
    public string Prototype = SharedTypingIndicatorSystem.InitialIndicatorId;
}
