using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Wounds.Systems;

public sealed class WoundSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private RobustRandom _random = default!;

    public override void Initialize()
    {
    }



}

[Serializable, NetSerializable]
[Flags]
public enum WoundDepth
{
    None = 0,
    Surface = 1 <<1,
    Internal = 1 << 2,
    Solid = 1 << 3,
}

[Serializable, NetSerializable, DataRecord]
public record struct WoundData (string WoundId, float Severity, float Tended, float Size, float Infected);
