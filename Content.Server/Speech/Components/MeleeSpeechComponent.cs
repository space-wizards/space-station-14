using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.Clothing;

namespace Content.Server.Speech.Components
{
    [RegisterComponent, NetworkedComponent]
    [AutoGenerateComponentState]
    [Access(typeof(SharedMeleeSpeechSystem), Other = AccessPermissions.ReadWrite)]
    public sealed class MeleeSpeechComponent : Component
    {

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("Battlecry")]
        [AutoNetworkedField]
        [Access(typeof(SharedMeleeSpeechSystem), Other = AccessPermissions.ReadWrite)]
        public string? Battlecry;
    }
}
