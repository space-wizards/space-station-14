using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.TypingIndicator;

/// <summary>
///     Show typing indicator icon when player typing text in chat box.
///     Added automatically when player poses entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTypingIndicatorSystem))]
public sealed partial class TypingIndicatorComponent : Component
{
    /// <summary>
    ///     Contains all typing indicators that something has. Whatever is at the front of the list is what will be
    ///     used when they are typing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<TypingIndicatorPrototype>> TypingIndicatorOverrideList = new List<ProtoId<TypingIndicatorPrototype>>();
}
