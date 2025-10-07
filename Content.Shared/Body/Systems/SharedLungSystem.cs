using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Inventory.Events;

namespace Content.Shared.Body.Systems;

public abstract class SharedLungSystem : EntitySystem
{

    public static string LungSolutionName = "Lung";
    public override void Initialize()
    {
        base.Initialize();

        //Put all events/functions I want predicted here.
        //SubscribeLocalEvent<BatteryComponent, EmpPulseEvent>(OnEmpPulse);
        //OnEmpPulse for example would be 'private void OnEmpPulse'

        //Everything else handled by the server should be added as virtual methods
        //for example 'public virtual float UseCharge', then overriden.
    }

    public virtual Solution GasToReagent(GasMixture gas) { return new Solution(); }
}
