using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.Systems;

public class InternalsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InternalsComponent, InhaleLocationEvent>(OnInhaleLocation);
    }

    private void OnInhaleLocation(EntityUid uid, InternalsComponent component, InhaleLocationEvent args)
    {
        if (component.AreInternalsWorking())
        {
            var gasTank = Comp<GasTankComponent>(component.GasTankEntity!.Value);
            args.Gas = gasTank.RemoveAirVolume(Atmospherics.BreathVolume);
        }
    }
}
