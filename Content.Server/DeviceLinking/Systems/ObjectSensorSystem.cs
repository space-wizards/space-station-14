using Content.Server.DeviceLinking.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Systems;

namespace Content.Server.DeviceLinking.Systems;

public sealed class ObjectSensorSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    private readonly int ModeCount = Enum.GetValues(typeof(ObjectSensorMode)).Length;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ObjectSensorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ObjectSensorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ObjectSensorComponent, InteractUsingEvent>(OnInteractUsing);
    }

    public override void Update(float deltaTime)
    {
        var query = EntityQueryEnumerator<ObjectSensorComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            UpdateOutput(uid, component);
        }
    }

    private void OnInteractUsing(EntityUid uid, ObjectSensorComponent component, InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, component.CycleQuality))
            return;

        if (TryComp<UseDelayComponent>(uid, out var delay)
            && !_useDelay.TryResetDelay((uid, delay), true))
            return;

        var mode = (int) component.Mode;
        mode = ++mode % ModeCount;
        component.Mode = (ObjectSensorMode) mode;

        UpdateOutput(uid, component);

        _audio.PlayPvs(component.CycleSound, uid);
        var msg = Loc.GetString("object-sensor-cycle", ("mode", component.Mode.ToString()));
        _popup.PopupEntity(msg, uid, args.User);
    }

    private void OnExamined(EntityUid uid, ObjectSensorComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("object-sensor-examine", ("mode", component.Mode.ToString())));
    }

    private void OnInit(EntityUid uid, ObjectSensorComponent component, ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(uid, component.OutputPort1, component.OutputPort2, component.OutputPort3, component.OutputPort4OrMore);
    }

    private void SetOut(EntityUid uid, ObjectSensorComponent component, int port, bool signal)
    {
        Logger.Debug($"{ToPrettyString(uid)} {port} {signal}");
        switch (port)
        {
            case 1:
                _deviceLink.SendSignal(uid, component.OutputPort1, signal);
                break;
            case 2:
                _deviceLink.SendSignal(uid, component.OutputPort2, signal);
                break;
            case 3:
                _deviceLink.SendSignal(uid, component.OutputPort3, signal);
                break;
            case 4:
                _deviceLink.SendSignal(uid, component.OutputPort4OrMore, signal);
                break;
        }
    }

    private void UpdateOutput(EntityUid uid, ObjectSensorComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        var oldTotal = component.Contacting;

        var contacting = _physics.GetContactingEntities(uid);
        var total = 0;

        foreach (var ent in contacting)
        {
            switch (component.Mode)
            {
                case ObjectSensorMode.Living:
                    if (TryComp(ent, out MobStateComponent? mobState)
                        && mobState is { CurrentState: MobState.Alive })
                        total++;
                    break;
                case ObjectSensorMode.Items:
                    if (TryComp(ent, out ItemComponent? _))
                        total++;
                    break;
                case ObjectSensorMode.All:
                    total++;
                    break;
            }
        }

        if (total == oldTotal)
            return;

        component.Contacting = total;

        Logger.Debug($"{oldTotal} from {component.Contacting}");

        if (component.Contacting > oldTotal)
        {
            for (var i = oldTotal; i <= (component.Contacting > 4 ? 4 : component.Contacting); i++)
            {
                SetOut(uid, component, i, true);
            }
        }
        else
        {
            for (var i = oldTotal; i >= component.Contacting; i--)
            {
                SetOut(uid, component, i, false);
            }
        }
    }
}
