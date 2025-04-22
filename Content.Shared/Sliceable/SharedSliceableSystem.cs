using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;

using Robust.Shared.Serialization;

namespace Content.Shared.Sliceable;

public abstract class SharedSliceableSystem : EntitySystem
{
    [Dependency] private readonly SharedToolSystem _tools = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SliceableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SliceableComponent, TrySliceEvent>(AfterSlicing);
    }

    private void AfterSlicing(EntityUid uid, SliceableComponent comp, TrySliceEvent args)
    {
        var ev = new SliceFoodEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    private void OnInteractUsing(EntityUid uid, SliceableComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ToolComponent>(args.Used, out var toolComp))
            return;

        _tools.UseTool(
            args.Used,
            args.User,
            uid,
            comp.SliceTime,
            toolComp.Qualities,
            new TrySliceEvent());
    }

    /// <summary>
    ///     Called after doafter.
    /// </summary>
    [ByRefEvent]
    public record struct SliceFoodEvent();
}

/// <summary>
///     Called during doafter.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class TrySliceEvent : SimpleDoAfterEvent;
