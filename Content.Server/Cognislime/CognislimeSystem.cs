using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Emoting;
using Content.Shared.Mind.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Speech;
using Content.Shared.IdentityManagement;
using Content.Shared.Cognislime;
using Content.Shared.Whitelist;
using Content.Shared.Interaction.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Resist;

namespace Content.Server.Cognislime;

/// <summary>
/// Makes objects sentient.
/// </summary>
public sealed class CognislimeSystem : SharedCognislimeSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CognislimeComponent, AfterInteractEvent>(OnCognislimeAfterInteract);
        SubscribeLocalEvent<CognislimeComponent, CognislimeDoAfterEvent>(ApplyCognislime);
    }

    public void OnCognislimeAfterInteract(EntityUid uid, CognislimeComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || args.Handled || !args.CanReach)
            return;

        if (!CheckTarget(args.Target.Value, component.Whitelist, component.Blacklist))
        {
            _popup.PopupEntity(Loc.GetString("cognislime-invalid"), args.Target.Value, args.User);
            return;
        }

        args.Handled = true;

        TryApplyCognizine(component, args.User, args.Target.Value, uid);
    }
    public bool CheckTarget(EntityUid target, EntityWhitelist? whitelist, EntityWhitelist? blacklist)
    {
        return whitelist?.IsValid(target, EntityManager) != false &&
            blacklist?.IsValid(target, EntityManager) != true;
    }

    public void TryApplyCognizine(CognislimeComponent component, EntityUid user, EntityUid target, EntityUid cognislime)
    {
        var ev = new CognislimeDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, user, component.ApplyCognislimeDuration, ev, cognislime, target: target, used: cognislime)
        {
            BreakOnUserMove = true,
            BreakOnTargetMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (_doAfter.TryStartDoAfter(args))
            _popup.PopupEntity(Loc.GetString("cognislime-applying"), target, user);
    }
    public void ApplyCognislime(EntityUid uid, CognislimeComponent component, CognislimeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null || args.Used == null)
            return;

        var target = args.Target.Value;

        _popup.PopupEntity(Loc.GetString("cognislime-applied", ("target", Identity.Entity(target, EntityManager))), target, args.User);

        EnsureComp<MindContainerComponent>(target);
        EnsureComp<ExaminerComponent>(target);

        if (!HasComp<GhostRoleComponent>(target))
        {
            var ghostRole = AddComp<GhostRoleComponent>(target);
            ghostRole.RoleName = Name(target);
            ghostRole.RoleDescription = Loc.GetString("ghost-role-information-cognislime-description");
            EnsureComp<GhostTakeoverAvailableComponent>(target);
        }

        if (component.CanMove)
        {
            EnsureComp<InputMoverComponent>(target);
            EnsureComp<MobMoverComponent>(target);
            EnsureComp<MovementSpeedModifierComponent>(target);
            EnsureComp<CanEscapeInventoryComponent>(target);
            RemComp<BlockMovementComponent>(target);

            // Todo: change fixtures so affected entities can't do stuff like push tanks by walking into them
        }

        if (component.CanSpeak)
        {
            EnsureComp<SpeechComponent>(target);
            EnsureComp<EmotingComponent>(target);
        }

        QueueDel(uid);
    }
}
