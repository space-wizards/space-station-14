// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.PowerCell;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Content.Shared.DeadSpace.Autopsy;
using Content.Server.Buckle.Systems;
using Content.Server.Popups;
using Content.Shared.Buckle.Components;
using Content.Shared.Mobs;

namespace Content.Server.DeadSpace.Autopsy;

public sealed class AutopsyScannerSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly BuckleSystem _buckleSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AutopsyScannerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<AutopsyScannerComponent, AutopsyScannerDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, AutopsyScannerComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<MobStateComponent>(args.Target) || !_cell.HasActivatableCharge(uid, user: args.User))
            return;

        if (!TryComp<MobStateComponent>(args.Target, out var mobState) || mobState.CurrentState != MobState.Dead)
        {
            _popupSystem.PopupEntity(Loc.GetString("autopsy-scanner-must-be-dead"), uid);
            return;
        }

        if (!_buckleSystem.IsBuckled(args.Target.Value)
            && TryComp<BuckleComponent>(args.Target, out var buckle) && !HasComp<AutopsyTableComponent>(buckle.BuckledTo))
        {
            _popupSystem.PopupEntity(Loc.GetString("autopsy-scanner-must-be-buckle-to-autopsy-table"), uid);
            return;
        }

        if (!HasComp<DamageableComponent>(args.Target) || !HasComp<HumanoidDamageSequenceComponent>(args.Target))
            return;

        _audio.PlayPvs(component.ScanningSound, uid);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.ScanDelay, new AutopsyScannerDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            BreakOnMove = true,
            NeedHand = true
        });
    }

    private void OnDoAfter(EntityUid uid, AutopsyScannerComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null || !_cell.TryUseActivatableCharge(uid, user: args.User))
            return;

        // OnAfterInteract code duplicate checks
        if (!TryComp<MobStateComponent>(args.Args.Target, out var mobState) || mobState.CurrentState != MobState.Dead)
        {
            _popupSystem.PopupEntity(Loc.GetString("autopsy-scanner-must-be-dead"), uid);
            _audio.PlayPvs(component.ScanningErrorSound, args.Args.User);
            return;
        }

        if (!_buckleSystem.IsBuckled(args.Args.Target.Value)
            && TryComp<BuckleComponent>(args.Args.Target, out var buckle) && !HasComp<AutopsyTableComponent>(buckle.BuckledTo))
        {
            _popupSystem.PopupEntity(Loc.GetString("autopsy-scanner-must-be-buckle-to-autopsy-table"), uid);
            _audio.PlayPvs(component.ScanningErrorSound, args.Args.User);
            return;
        }

        if (!HasComp<DamageableComponent>(args.Args.Target) || !HasComp<HumanoidDamageSequenceComponent>(args.Args.Target))
        {
            _audio.PlayPvs(component.ScanningErrorSound, args.Args.User);
            return;
        }
        // OnAfterInteract code duplicate checks end

        _audio.PlayPvs(component.ScanningSound, uid);

        UpdateScannedUser(uid, args.Args.User, args.Args.Target.Value, component);
        args.Handled = true;
    }

    private void OpenUserInterface(EntityUid user, EntityUid scanner)
    {
        if (!_uiSystem.HasUi(scanner, AutopsyScannerUiKey.Key))
            return;

        _uiSystem.OpenUi(scanner, AutopsyScannerUiKey.Key, user);
    }

    public void UpdateScannedUser(EntityUid uid, EntityUid user, EntityUid? target, AutopsyScannerComponent? autopsyScanner)
    {
        if (!Resolve(uid, ref autopsyScanner))
            return;

        if (target == null || !_uiSystem.HasUi(uid, AutopsyScannerUiKey.Key))
            return;

        TryComp<HumanoidDamageSequenceComponent>(target, out var damageSequence);

        OpenUserInterface(user, uid);

        _uiSystem.ServerSendUiMessage(uid, AutopsyScannerUiKey.Key, new AutopsyScannerScannedUserMessage(
            GetNetEntity(target),
            damageSequence != null ? damageSequence.TimeOfDeath : null
            ));
    }
}
