using System.Linq;
using System.Runtime.InteropServices;
using Content.Shared.Damage;
using Content.Shared.Medical.Wounds.Prototypes;

namespace Content.Shared.Medical.Wounds.Components;

[RegisterComponent]
public sealed class WoundableComponent : Component
{
    public Dictionary<string, List<WoundData>> Wounds = new();
}
