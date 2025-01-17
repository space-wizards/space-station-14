using Content.Shared.Actions;
using Content.Shared.Communications;
using Content.Shared.Ninja.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Starlight.Antags.Abductor;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem)), AutoGenerateComponentState]
public sealed partial class AbductorVestComponent : Component
{
    [DataField, AutoNetworkedField]
    public string CurrentState = "stealth";
}