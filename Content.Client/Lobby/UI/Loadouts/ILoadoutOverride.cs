using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.Loadouts;

public interface ILoadoutOverride
{
    public Action<HumanoidCharacterProfile?>? OnValueChanged { get; set; }

    HumanoidCharacterProfile? Profile { get; set; }

    void Refresh(IPrototypeManager protoMan, ref HumanoidCharacterProfile? profile);
}
