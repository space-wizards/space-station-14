using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Emoting;
using Content.Shared.Mind.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Speech;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Cognislime;
using Content.Shared.Whitelist;

namespace Content.Server.Cognislime;

/// <summary>
/// Makes stuff sentient.
/// </summary>
public sealed partial class CognislimeSystem : SharedCognislimeSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

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
            _popup.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, user);
    }
    public void ApplyCognislime(EntityUid uid, CognislimeComponent component, CognislimeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null || args.Used == null)
            return;

        var target = args.Target.Value;

        EntityManager.EnsureComponent<MindContainerComponent>(target);

        if (!EntityManager.HasComponent<GhostRoleComponent>(uid))
        {
            var ghostRole = EntityManager.AddComponent<GhostRoleComponent>(target);
            ghostRole.RoleName = "cognislimzed";
            ghostRole.RoleDescription = "something";
            ghostRole.RoleRules = "thou shalt not kill";
            EntityManager.EnsureComponent<GhostTakeoverAvailableComponent>(target);
        }

        if (component.CanMove)
        {
            EntityManager.EnsureComponent<InputMoverComponent>(target);
            EntityManager.EnsureComponent<MobMoverComponent>(target);
            EntityManager.EnsureComponent<MovementSpeedModifierComponent>(target);
        }

        if (component.CanSpeak)
        {
            EntityManager.EnsureComponent<SpeechComponent>(target);
            EntityManager.EnsureComponent<EmotingComponent>(target);
        }
        _audio.PlayPredicted(component.CognislimeSound, target, args.User);
        EntityManager.EnsureComponent<ExaminerComponent>(target);

        QueueDel(uid);
    }
}
