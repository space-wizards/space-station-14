using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Foldable;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Salvage.Fulton;

public abstract class SharedFultonSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private   readonly MetaDataSystem _metadata = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private   readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private   readonly SharedFoldableSystem _foldable = default!;
    [Dependency] private   readonly SharedPopupSystem _popup = default!;
    [Dependency] private   readonly SharedStackSystem _stack = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;

    public static readonly TimeSpan FultonDuration = TimeSpan.FromSeconds(45);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FultonedDoAfterEvent>(OnFultonDoAfter);

        SubscribeLocalEvent<FultonedComponent, EntityUnpausedEvent>(OnFultonUnpaused);
        SubscribeLocalEvent<FultonedComponent, GetVerbsEvent<InteractionVerb>>(OnFultonedGetVerbs);
        SubscribeLocalEvent<FultonedComponent, ExaminedEvent>(OnFultonedExamine);
        SubscribeLocalEvent<FultonedComponent, EntGotInsertedIntoContainerMessage>(OnFultonContainerInserted);

        SubscribeLocalEvent<FultonComponent, AfterInteractEvent>(OnFultonInteract);
    }

    private void OnFultonContainerInserted(EntityUid uid, FultonedComponent component, EntGotInsertedIntoContainerMessage args)
    {
        RemCompDeferred<FultonedComponent>(uid);
    }

    private void OnFultonedExamine(EntityUid uid, FultonedComponent component, ExaminedEvent args)
    {
        var remaining = component.NextFulton + _metadata.GetPauseTime(uid) - Timing.CurTime;
        var message = Loc.GetString("fulton-examine", ("time", $"{remaining.TotalSeconds:0.00}"));

        args.PushText(message);
    }

    private void OnFultonedGetVerbs(EntityUid uid, FultonedComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        args.Verbs.Add(new InteractionVerb()
        {
            Text = Loc.GetString("fulton-remove"),
            Act = () =>
            {
                Unfulton(uid);
            }
        });
    }

    private void Unfulton(EntityUid uid, FultonedComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        RemCompDeferred<FultonedComponent>(uid);
    }

    private void OnFultonDoAfter(FultonedDoAfterEvent args)
    {
        if (args.Cancelled || args.Target == null || !TryComp<FultonComponent>(args.Used, out var fulton))
            return;

        if (!_stack.Use(args.Used.Value, 1))
        {
            return;
        }

        var fultoned = AddComp<FultonedComponent>(args.Target.Value);
        fultoned.Beacon = fulton.Beacon;
        fultoned.NextFulton = Timing.CurTime + FultonDuration;
        UpdateAppearance(args.Target.Value, fultoned);
        Dirty(args.Target.Value, fultoned);
        Audio.PlayPredicted(fulton.FultonSound, args.Target.Value, args.User);
    }

    private void OnFultonUnpaused(EntityUid uid, FultonedComponent component, ref EntityUnpausedEvent args)
    {
        component.NextFulton += args.PausedTime;
    }

    private void OnFultonInteract(EntityUid uid, FultonComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || args.Handled)
            return;

        if (TryComp<FultonBeaconComponent>(args.Target, out var beacon))
        {
            if (!_foldable.IsFolded(args.Target.Value))
            {
                component.Beacon = args.Target.Value;
                Audio.PlayPredicted(beacon.LinkSound, uid, args.User);
                _popup.PopupClient(Loc.GetString("fulton-linked"), uid, args.User);
            }
            else
            {
                component.Beacon = EntityUid.Invalid;
                _popup.PopupClient(Loc.GetString("fulton-folded"), uid, args.User);
            }

            return;
        }

        if (!CanFulton(args.Target.Value, uid, component))
        {
            _popup.PopupClient(Loc.GetString("fulton-invalid"), uid, uid);
            return;
        }

        if (HasComp<FultonedComponent>(args.Target))
        {
            _popup.PopupClient(Loc.GetString("fulton-fultoned"), uid, uid);
            return;
        }

        args.Handled = true;

        var ev = new FultonedDoAfterEvent();
        _doAfter.TryStartDoAfter(
            new DoAfterArgs(args.User, component.ApplyFultonDuration, ev, args.Target, args.Target, args.Used)
            {
                CancelDuplicate = true,
                MovementThreshold = 0.5f,
                BreakOnUserMove = true,
                BreakOnTargetMove = true,
                Broadcast = true,
                NeedHand = true,
            });
    }

    protected virtual void UpdateAppearance(EntityUid uid, FultonedComponent fultoned)
    {
        return;
    }

    private bool CanFulton(EntityUid targetUid, EntityUid uid, FultonComponent component)
    {
        if (Transform(targetUid).Anchored)
            return false;

        if (component.Whitelist?.IsValid(targetUid, EntityManager) != true)
        {
            return false;
        }

        return true;
    }

    [Serializable, NetSerializable]
    private sealed class FultonedDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
