using Content.Shared.Weapons.Ranged;
using Robust.Shared.Map;

namespace Content.Client.Weapons.Ranged;

public sealed partial class NewGunSystem
{
    protected override void InitializeBallistic()
    {
        base.InitializeBallistic();
        SubscribeLocalEvent<BallisticAmmoProviderComponent, UpdateAmmoCounterEvent>(OnBallisticAmmoCount);
    }

    private void OnBallisticAmmoCount(EntityUid uid, BallisticAmmoProviderComponent component, UpdateAmmoCounterEvent args)
    {
        if (args.Control is DefaultStatusControl control)
        {
            control.Update(GetBallisticShots(component), component.Capacity);
            return;
        }
    }

    protected override void Cycle(BallisticAmmoProviderComponent component, MapCoordinates coordinates)
    {
        if (!Timing.IsFirstTimePredicted) return;

        EntityUid? ent = null;

        // TODO: Combine with TakeAmmo
        if (component.Entities.TryPop(out var existing))
        {
            component.Container.Remove(existing);
            EnsureComp<NewAmmoComponent>(existing);
        }
        else if (component.UnspawnedCount > 0)
        {
            component.UnspawnedCount--;
            ent = Spawn(component.FillProto, coordinates);
            EnsureComp<NewAmmoComponent>(ent.Value);
        }

        if (ent != null && ent.Value.IsClientSide())
            Del(ent.Value);
    }
}
