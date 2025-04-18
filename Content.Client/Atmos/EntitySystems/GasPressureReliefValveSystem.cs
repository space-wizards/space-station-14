using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Atmos.Piping.Binary.Components;

namespace Content.Client.Atmos.EntitySystems;

public sealed class GasPressureReliefValveSystem : SharedGasPressureReliefValveSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasPressureReliefValveComponent, AfterAutoHandleStateEvent>(OnValveUpdate);
    }

    private void OnValveUpdate(Entity<GasPressureReliefValveComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }

    protected override void UpdateUi(Entity<GasPressureReliefValveComponent> ent)
    {
        if (UserInterfaceSystem.TryGetOpenUi(ent.Owner, GasPressureReliefValveUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}
