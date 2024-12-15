using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Starlight.CloudEmote;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class CloudEmoteActiveComponent : Component
{
    [DataField("emote_name"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string EmoteName = "";

    [DataField("phase"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int Phase = -1; // -1 - emote inited, 0 - emote started, 1 - emote in progress, 2 - emote ending

    [DataField("entity"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid Emote;
}
