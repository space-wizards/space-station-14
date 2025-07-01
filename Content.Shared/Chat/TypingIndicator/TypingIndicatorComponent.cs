using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.TypingIndicator;

/// <summary>
///     Show typing indicator icon when player typing text in chat box.
///     Added automatically when player poses entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState] //CD - AutoGenerateComponentState fixes a bug with synth trait
//[Access(typeof(SharedTypingIndicatorSystem))] CD - Restricted access breaks synth trait because it rewrites the speech bubble over the default race indicator
public sealed partial class TypingIndicatorComponent : Component
{
    /// <summary>
    ///     Prototype id that store all visual info about typing indicator.
    /// </summary>
    [DataField("proto"), AutoNetworkedField] // CD - AutoNetworkField fixes a bug in synth trait
    public ProtoId<TypingIndicatorPrototype> TypingIndicatorPrototype = "default";
}
