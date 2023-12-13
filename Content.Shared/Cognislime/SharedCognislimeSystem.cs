using System.Numerics;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Foldable;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Cognislime;

/// <summary>
/// Makes stuff sentient.
/// </summary>
public abstract partial class SharedCognislimeSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private   readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CognislimeDoAfterEvent>(OnCognislimeDoAfter);

        SubscribeLocalEvent<CognislimeComponent, AfterInteractEvent>(OnCognislimeInteract);
    }


    private void OnCognislimeDoAfter(CognislimeDoAfterEvent args)
    {
        if (args.Cancelled || args.Target == null || !TryComp<CognislimeComponent>(args.Used, out var cognislime))
            return;

        Logger.Debug("HOLY S IT WORKED");

        Audio.PlayPredicted(cognislime.CognislimeSound, args.Target.Value, args.User);
    }

    private void OnCognislimeInteract(EntityUid uid, CognislimeComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || args.Handled || !args.CanReach)
            return;

        if (!CanCognislime(args.Target.Value, uid, component))
        {
            _popup.PopupClient(Loc.GetString("fulton-invalid"), uid, uid);
            return;
        }

        args.Handled = true;

        var ev = new CognislimeDoAfterEvent();
        _doAfter.TryStartDoAfter(
            new DoAfterArgs(EntityManager, args.User, component.ApplyCognislimeDuration, ev, args.Target, args.Target, args.Used)
            {
                CancelDuplicate = true,
                MovementThreshold = 0.5f,
                BreakOnUserMove = true,
                BreakOnTargetMove = true,
                Broadcast = true,
                NeedHand = true,
            });
    }

    private bool CanCognislime(EntityUid targetUid, EntityUid uid, CognislimeComponent component)
    {
        if (component.Whitelist?.IsValid(targetUid, EntityManager) != true)
        {
            return false;
        }

        return true;
    }

    [Serializable, NetSerializable]
    private sealed partial class CognislimeDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
