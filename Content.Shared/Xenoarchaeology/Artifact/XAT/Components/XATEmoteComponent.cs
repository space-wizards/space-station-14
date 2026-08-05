using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for a xenoarch trigger that activates when something emotes nearby.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATEmoteSystem)), AutoGenerateComponentState]
public sealed partial class XATEmoteComponent : Component
{
    /// <summary>
    /// List of accepted emotes.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public List<ProtoId<EmotePrototype>> Emotes = new();

    /// <summary>
    /// Range within which artifact reacts to emote events.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 10;
}
