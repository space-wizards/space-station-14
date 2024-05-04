using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.TypingIndicator;

/// <summary>
///     Show typing indicator icon when player typing text in chat box.
///     Added automatically when player poses entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTypingIndicatorSystem))]
public sealed partial class TypingIndicatorComponent : Component
{
    /// <summary>
    ///     Prototype id that store all visual info about typing indicator.
    /// </summary>
    [DataField]
    public ProtoId<TypingIndicatorPrototype> DefaultTypingIndicator = "default";

    /// <summary>
    ///     A list of all indicators that override the default one. E.g if you put on
    ///     a moth mask the list would have both the lawyer and the moth indicator.
    /// </summary>
    [DataField]
    public List<ProtoId<TypingIndicatorPrototype>> TypingIndicatorOverrideList = new List<ProtoId<TypingIndicatorPrototype>>();
}
