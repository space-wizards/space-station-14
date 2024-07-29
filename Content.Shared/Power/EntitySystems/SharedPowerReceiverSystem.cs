using Content.Shared.Examine;
using Content.Shared.Power.Components;

namespace Content.Shared.Power.EntitySystems;

public abstract class SharedPowerReceiverSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedApcPowerReceiverComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<SharedApcPowerReceiverComponent> ent, ref ExaminedEvent args)
    {
        var powered = ent.Comp.Powered ? "powered" : "unpowered";
        var state = Loc.GetString($"power-receiver-component-on-examine-{powered}");
        args.PushMarkup(Loc.GetString("power-receiver-component-on-examine-main", ("stateText", state)));
    }
}
