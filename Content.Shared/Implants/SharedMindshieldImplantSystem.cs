using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Stunnable;

namespace Content.Shared.Implants;

public abstract partial class SharedMindshieldImplantSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedStunSystem _sharedStun = default!;
    [Dependency] private SharedMindSystem _mindSystem = default!;
    [Dependency] private SharedRoleSystem _roleSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindShieldImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
    }

    /// <summary>
    /// When the mindshield is implanted in the rev it will popup saying they were deconverted. In Head Revs it will remove the mindshield component.
    /// </summary>
    private void OnImplantImplanted(Entity<MindShieldImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        // Entity that was implanted
        var uid = args.Implanted;
        if (HasComp<HeadRevolutionaryComponent>(uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("head-rev-break-mindshield"), uid);
            QueueDel(args.Implant);
            return;
        }

        if (TryComp<RevolutionaryComponent>(uid, out var comp))
        {
            if (_mindSystem.TryGetMind(uid, out var mindId, out _) &&
            _roleSystem.MindRemoveRole<RevolutionaryRoleComponent>(mindId))
            {
                TryLog(uid);
            }

            var name = Identity.Entity(uid, EntityManager);
            RemComp<RevolutionaryComponent>(uid);
            _sharedStun.TryUpdateParalyzeDuration(uid, comp.StunTime);
            _popupSystem.PopupEntity(Loc.GetString("rev-break-control", ("name", name)), uid);
        }
    }

    /// <summary>
    /// Will be implemented on the server side to log the mind that was deconverted
    /// </summary>
    protected abstract void TryLog(EntityUid uid);

}
