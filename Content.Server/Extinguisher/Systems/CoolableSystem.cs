using Content.Server.Extinguisher.Components;
using Content.Server.Extinguisher.Events;
using Content.Shared.Extinguisher.Events;
using Content.Shared.Interaction;

namespace Content.Server.Extinguisher.Systems;

public sealed class CoolableSystem : EntitySystem
{
    [Dependency]
    private readonly FireExtinguisherSystem _extinguisher = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CoolableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CoolableComponent, CoolingFinishedDoAfterEvent>(OnDoAfter);
    }

    private void OnInteractUsing(EntityUid uid, CoolableComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryCool(uid, args.Used, args.User, component);
    }

    private bool CanCool(EntityUid uid, EntityUid extinguisher, EntityUid user, CoolableComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        // Other component systems
        var attempt = new CoolingAttemptEvent(user, extinguisher);
        RaiseLocalEvent(uid, attempt, true);
        return !attempt.Cancelled;
    }

    private bool TryCool(EntityUid uid, EntityUid extinguisher, EntityUid user, CoolableComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!CanCool(uid, extinguisher, user, component))
            return false;

        return _extinguisher.UseExtinguisher(
            extinguisher,
            user,
            uid,
            component.CoolingTime.Seconds,
            new CoolingFinishedDoAfterEvent(),
            water: component.WaterConsumption);
    }

    private void OnDoAfter(EntityUid uid, CoolableComponent component, CoolingFinishedDoAfterEvent args)
    {
        if (args.Cancelled || args.Used == null)
            return;

        // Check if target is still valid
        if (!CanCool(uid, args.Used.Value, args.User, component))
            return;

        RaiseLocalEvent(uid, new CoolableEvent());
    }
}

