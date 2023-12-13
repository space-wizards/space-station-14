using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Content.Shared.Emoting;
using Content.Shared.Mind.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Speech;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Cognislime;

namespace Content.Server.Cognislime;

/// <summary>
/// Makes stuff sentient.
/// </summary>
public sealed partial class CognislimeSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CognislimeComponent, AfterInteractEvent>(OnCognislimeInteract);
    }

    public void OnCognislimeInteract(EntityUid uid, CognislimeComponent component, AfterInteractEvent args)
    {
        Logger.Debug("Got here");

        if (args.Target == null || args.Handled || !args.CanReach)
            return;

        if (!CanCognislime(uid, uid, component))
        {
            _popup.PopupClient(Loc.GetString("fulton-invalid"), uid, uid);
            return;
        }

        args.Handled = true;

        if (args.Target == null || !TryComp<CognislimeComponent>(args.Used, out var cognislime))
            return;

        var cognislimedEntity = args.Target.Value;

        if (EntityManager.TryGetComponent(uid, out GhostRoleComponent? ghostRole))
        {
            return;
        }

        if (EntityManager.HasComponent<GhostTakeoverAvailableComponent>(cognislimedEntity))
        {
            return;
        }

        ghostRole = EntityManager.AddComponent<GhostRoleComponent>(cognislimedEntity);
        EntityManager.AddComponent<GhostTakeoverAvailableComponent>(cognislimedEntity);
        ghostRole.RoleName = "cognislimzed";
        ghostRole.RoleDescription = "something";
        ghostRole.RoleRules = "thou shalt not kill";

        EntityManager.EnsureComponent<MindContainerComponent>(cognislimedEntity);
        EntityManager.EnsureComponent<InputMoverComponent>(cognislimedEntity);
        EntityManager.EnsureComponent<MobMoverComponent>(cognislimedEntity);
        EntityManager.EnsureComponent<MovementSpeedModifierComponent>(cognislimedEntity);



        EntityManager.EnsureComponent<SpeechComponent>(cognislimedEntity);
        EntityManager.EnsureComponent<EmotingComponent>(cognislimedEntity);

        EntityManager.EnsureComponent<ExaminerComponent>(cognislimedEntity);

        QueueDel(uid);
    }

    private bool CanCognislime(EntityUid targetUid, EntityUid uid, CognislimeComponent component)
    {
        if (component.Whitelist?.IsValid(targetUid, EntityManager) != true)
        {
            return false;
        }

        return true;
    }
}
