using Content.Shared.Power.Components;

namespace Content.Shared.Power.EntitySystems;

public abstract class SharedPowerNetSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public abstract bool IsPoweredCalculate(SharedApcPowerReceiverComponent comp);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AppearanceComponent, PowerChangedEvent>(OnPowerAppearance);
    }

    private void OnPowerAppearance(Entity<AppearanceComponent> ent, ref PowerChangedEvent args)
    {
        _appearance.SetData(ent, PowerDeviceVisuals.Powered, args.Powered, ent.Comp);
    }
}
