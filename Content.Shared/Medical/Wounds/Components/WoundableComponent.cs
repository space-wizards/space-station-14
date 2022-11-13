using System.Linq;
using System.Runtime.InteropServices;
using Content.Shared.Damage;
using Content.Shared.Medical.Wounds.Prototypes;
using Content.Shared.Medical.Wounds.Systems;

namespace Content.Shared.Medical.Wounds.Components;

[RegisterComponent]
public sealed class WoundableComponent : Component
{
    [Access(typeof(WoundSystem),Other = AccessPermissions.Read)]
    public Dictionary<string, List<WoundData>> Wounds = new();
}
