using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.LeaderHighlight;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class LeaderHighlightComponent : Component
{
    /// <summary>
    /// The mind of your current Leader. As this is only implemented in Revs right now, this marks the HeadRev who converted you.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Leader = null;

    /// <summary>
    /// The color the text will be highlighted in.
    /// </summary>
    [DataField]
    public Color HighlightColor = Color.Red;
}
