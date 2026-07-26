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

public sealed partial class MindshieldImplantSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _sharedStun = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private ISharedAdminLogManager _log = default!;

    /// <summary>
    /// When the mindshield is implanted in the rev it will popup saying they were deconverted. In Head Revs it will remove the mindshield component.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnImplantImplanted(Entity<MindShieldImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        // Entity that was implanted
        var uid = args.Implanted;
        if (HasComp<HeadRevolutionaryComponent>(uid))
        {
            _popup.PopupEntity(Loc.GetString("head-rev-break-mindshield"), uid);
            PredictedQueueDel(args.Implant);
            return;
        }

        if (TryComp<RevolutionaryComponent>(uid, out var comp))
        {
            if (_mind.TryGetMind(uid, out var mindId, out _) && _role.MindRemoveRole<RevolutionaryRoleComponent>(mindId))
                _log.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(uid)} was deconverted due to being implanted with a Mindshield.");

            var name = Identity.Entity(uid, EntityManager);
            RemComp<RevolutionaryComponent>(uid);
            _sharedStun.TryUpdateParalyzeDuration(uid, comp.StunTime);
            _popup.PopupEntity(Loc.GetString("rev-break-control", ("name", name)), uid);
        }
    }
}
