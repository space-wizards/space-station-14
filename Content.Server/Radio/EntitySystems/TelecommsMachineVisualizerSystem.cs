using Content.Server.Power.Components;
using Content.Server.Radio.Components.Telecomms;
using Content.Shared.Radio;
using Robust.Shared.Timing;

namespace Content.Server.Radio.EntitySystems;
public sealed class TelecommsMachineVisualizerSystem : EntitySystem
{
    private readonly List<EntityUid> _transtmitFlicks = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TelecommsMachine, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<TelecommsMachine, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnComponentStartup(EntityUid uid, TelecommsMachine cabinet, ComponentStartup args)
    {
        UpdateAppearance(uid, cabinet.CanRun);
    }

    private void OnPowerChanged(EntityUid uid, TelecommsMachine cabinet, PowerChangedEvent args)
    {
        UpdateAppearance(uid, cabinet.CanRun);
    }

    // scuffed flick()
    public void DoTransmitFlick(EntityUid uid, TelecommsMachine machine)
    {
        if (_transtmitFlicks.Contains(uid))
        {
            // it exists, we are currently doing animation
            // i may use cancellation on the timer so that it continuously does it
            return;
        }

        UpdateAppearance(uid, machine.CanRun, true);
        _transtmitFlicks.Add(uid);
        // exactly 2.1 sec
        Timer.Spawn(TimeSpan.FromSeconds(2.1), () =>
        {
            UpdateAppearance(uid, machine.CanRun);
            _transtmitFlicks.Remove(uid);
        });
    }

    private void UpdateAppearance(EntityUid uid,
        bool isOn = false,
        bool isTransmiting = false)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        appearance.SetData(TelecommsMachineVisuals.IsOn, isOn);
        appearance.SetData(TelecommsMachineVisuals.IsTransmiting, isTransmiting);
    }
}
