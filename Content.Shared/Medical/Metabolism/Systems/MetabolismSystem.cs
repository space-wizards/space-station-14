using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.Metabolism.Systems;


/// <summary>
/// Handles metabolic reactions, primarily cellular respiration which energizes organs by converting Oxygen/Glucose into Co2/Energy
/// Also... The mitochondria is the powerhouse of the cell.
/// </summary>
public sealed partial class MetabolismSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly DamageableSystem _damageSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        MetabolizerInit();
        BodyMetabolismInit();
    }

    public override void Update(float frameTime)
    {
        MetabolizerUpdate(frameTime);
        UpdateBodyMetabolism(frameTime);
    }
}
