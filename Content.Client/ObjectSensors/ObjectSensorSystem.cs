using Content.Client.DeviceLinking;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.ObjectSensors.Components;
using Content.Shared.ObjectSensors.Systems;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Client.ObjectSensors;

public sealed class ObjectSensorSystem : SharedObjectSensorSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ObjectSensorComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<ObjectSensorComponent> uid, ref InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, uid.Comp.CycleQuality))
            return;

        if (TryComp<UseDelayComponent>(uid, out var delay)
            && !_useDelay.TryResetDelay((uid, delay), true))
            return;

        var mode = (int) uid.Comp.Mode;
        mode = ++mode % ModeCount;

        UpdateOutput(uid);
        var msg = Loc.GetString("object-sensor-cycle", ("mode", ((ObjectSensorMode) mode).ToString()));
        _popup.PopupClient(msg, uid, args.User);
    }

    /// <summary>
    ///     Updates all of the object sensors
    /// </summary>
    private void UpdateOutput(Entity<ObjectSensorComponent> uid)
    {
        var component = uid.Comp;
        var oldTotal = component.Contacting;
        var total = GetTotalEntitites(uid);

        Log.Debug($"my life be like {total}");

        if (total == oldTotal)
            return;

        _appearance.SetData(uid, ToggleVisuals.Toggled, total > 0);
    }
}
