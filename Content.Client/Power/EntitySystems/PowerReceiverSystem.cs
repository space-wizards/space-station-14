using System.Diagnostics.CodeAnalysis;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Power;
using Robust.Shared.GameStates;

namespace Content.Client.Power.EntitySystems;

public sealed class PowerReceiverSystem : SharedPowerReceiverSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PowerReceiverComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PowerReceiverComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnExamined(Entity<PowerReceiverComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(GetExamineText(ent.Comp.Powered));
    }

    private void OnHandleState(EntityUid uid, PowerReceiverComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not PowerReceiverComponentState state)
            return;

        var powerChanged = component.Powered != state.Powered;
        component.Powered = state.Powered;
        component.NeedsPower = state.NeedsPower;
        component.Enabled = state.Enabled;
        // SO client systems can handle it. The main reason for this is we can't guarantee compstate ordering.

        if (powerChanged)
            RaisePower((uid, component));
    }

    protected override void RaisePower(Entity<PowerReceiverComponent> entity)
    {
        var ev = new PowerChangedEvent(entity.Comp.Powered, 0f);
        RaiseLocalEvent(entity.Owner, ref ev);
    }

    public override bool ResolveApc(EntityUid entity, [NotNullWhen(true)] ref PowerReceiverComponent? component)
    {
        if (component != null)
            return true;

        if (!TryComp(entity, out PowerReceiverComponent? receiver))
            return false;

        component = receiver;
        return true;
    }
}
