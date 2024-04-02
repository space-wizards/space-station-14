using Content.Server.DeviceLinking.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Mobs.Systems;
using Content.Shared.ObjectSensors.Components;
using Content.Shared.ObjectSensors.Systems;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Components;
using Content.Shared.Timing;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Server.ObjectSensors;

public sealed partial class ObjectSensorSystem : SharedObjectSensorSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
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
            UpdateOutput((uid, component));
        }
    }

    private void OnExamined(Entity<ObjectSensorComponent> uid, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("object-sensor-examine", ("mode", uid.Comp.Mode.ToString())));
    }

    private void OnInit(Entity<ObjectSensorComponent> uid, ref ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(uid, uid.Comp.OutputPort1, uid.Comp.OutputPort2, uid.Comp.OutputPort3, uid.Comp.OutputPort4OrMore);
    }

    /// <summary>
    ///     Sends the given signal to the associated signal source.
    /// </summary>
    /// <param name="uid">The sensor</param>
    /// <param name="port">The index of the signal source</param>
    /// <param name="signal">The signal to be sent</param>
    private void SetOut(Entity<ObjectSensorComponent> uid, int port, bool signal)
    {
        var ports = uid.Comp.PortList;
        if (port <= 0)
            return;

        _deviceLink.SendSignal(uid, ports[Math.Min(port, ports.Count) - 1], signal);
    }

    /// <summary>
    ///     Updates the component's mode when interacted with using the right tool
    /// </summary>
    private void OnInteractUsing(Entity<ObjectSensorComponent> uid, ref InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, uid.Comp.CycleQuality))
            return;

        if (TryComp<UseDelayComponent>(uid, out var delay)
            && !_useDelay.TryResetDelay((uid, delay), true))
            return;

        var mode = (int) uid.Comp.Mode;
        mode = ++mode % ModeCount;
        uid.Comp.Mode = (ObjectSensorMode) mode;

        UpdateOutput(uid);

        _audio.PlayPvs(uid.Comp.CycleSound, uid);
    }

    /// <summary>
    ///     Updates all of the object sensors
    /// </summary>
    private void UpdateOutput(Entity<ObjectSensorComponent> uid)
    {
        var component = uid.Comp;

        var oldTotal = component.Contacting;

        if (!Transform(uid).Anchored)
            return;

        var total = GetTotalEntitites(uid);

        if (total == oldTotal)
            return;

        component.Contacting = total;

        _appearance.SetData(uid, ToggleVisuals.Toggled, total > 0);

        if (component.Contacting > oldTotal)
        {
            for (var i = oldTotal + 1; i <= (component.Contacting > 4 ? 4 : component.Contacting); i++)
                SetOut(uid, i, true);
        }
        else
        {
            for (var i = oldTotal; i > component.Contacting; i--)
                SetOut(uid, i, false);
        }
    }
}
