using Content.Shared.Power.Components;
using Content.Shared.Power.Events;

namespace Content.Shared.Power.Systems;

public abstract partial class SharedPowerNetSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    public abstract bool IsPoweredCalculate(PowerReceiverComponent comp);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AppearanceComponent, PowerChangedEvent>(OnPowerAppearance);
    }

    private void OnPowerAppearance(Entity<AppearanceComponent> ent, ref PowerChangedEvent args)
    {
        _appearance.SetData(ent, PowerDeviceVisuals.Powered, args.Powered, ent.Comp);
    }

    public virtual void InitPowerNet(Entity<PowerNetComponent> powerNet) { }

    public virtual void DestroyPowerNet(Entity<PowerNetComponent> powerNet) { }

    public virtual void QueueReconnectPowerNet(Entity<PowerNetComponent> powerNet) { }
}
