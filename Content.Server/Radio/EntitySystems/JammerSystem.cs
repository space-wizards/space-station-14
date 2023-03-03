using Content.Server.PowerCell;
using Content.Server.Radio.Components;
using Content.Shared.Interaction;

namespace Content.Server.Radio.EntitySystems;

public sealed class JammerSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioJammerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioSendAttempt);
    }

    public override void Update(float frameTime)
    {
        foreach (var jam in EntityQuery<RadioJammerComponent>())
        {
            var uid = jam.Owner;
            if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery))
            {
                jam.Enabled = false;
                continue;
            }
            if (jam.Enabled)
                jam.Enabled = battery.TryUseCharge(jam.Wattage * frameTime);
        }
    }

    private void OnActivate(EntityUid uid, RadioJammerComponent comp, ActivateInWorldEvent args)
    {
        comp.Enabled = !comp.Enabled;
    }

    private void OnRadioSendAttempt(ref RadioReceiveAttemptEvent args)
    {
        if (args.RadioSource == null)
            return;
        var source = Transform(args.RadioSource.Value).Coordinates;
        foreach (var jam in EntityQuery<RadioJammerComponent>())
        {
            if (jam.Enabled && source.InRange(EntityManager, Transform(jam.Owner).Coordinates, jam.Range))
            {
                args.Cancelled = true;
                return;
            }
        }
    }
}
