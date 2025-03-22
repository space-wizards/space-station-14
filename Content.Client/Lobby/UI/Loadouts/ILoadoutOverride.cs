using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.Loadouts;

public interface ILoadoutOverride
{
    public Action<KeyValuePair<string, string>>? OnValueChanged { get; set; }
    HumanoidCharacterProfile? Profile { get; set; }

    void Refresh(HumanoidCharacterProfile? profile, RoleLoadout loadout, IPrototypeManager protoMan);
}
