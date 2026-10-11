using Content.Server.Power.EntitySystems;

namespace Content.Server.Power.Generation.EnergyCore;

public sealed partial class EnergyCorePowerSystem : EntitySystem
{
    [Dependency] private EnergyCoreSystem _cores = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(PowerNetSystem));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _cores.FinishPowerUpdate(frameTime);
    }
}
