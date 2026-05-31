using Content.Server.Administration.Logs;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Revolutionary;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles.Components;
using Content.Shared.Stunnable;

namespace Content.Server.Revolutionary;

public sealed partial class RevolutionarySystem : SharedRevolutionarySystem
{
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedStunSystem _sharedStun = default!;
    [Dependency] private IAdminLogManager _adminLogManager = default!;
    [Dependency] private RoleSystem _roleSystem = default!;
    [Dependency] private MindSystem _mindSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<MindShieldImplantComponent, ImplantImplantedEvent>(MindShieldImplanted);
    }

    /// <summary>
    /// When the mindshield is implanted in the rev it will popup saying they were deconverted. In Head Revs it will remove the mindshield component.
    /// </summary>
    private void MindShieldImplanted(Entity<MindShieldImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        // Entity that was implanted
        var uid = args.Implanted;
        if (HasComp<HeadRevolutionaryComponent>(uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("head-rev-break-mindshield"), uid);
            QueueDel(args.Implant);
            return;
        }

        if (HasComp<RevolutionaryComponent>(uid))
        {
            if (_mindSystem.TryGetMind(uid, out var mindId, out _) &&
            _roleSystem.MindRemoveRole<RevolutionaryRoleComponent>(mindId))
            {
                _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(uid)} was deconverted due to being implanted with a Mindshield.");
            }

            var stunTime = TimeSpan.FromSeconds(4);
            var name = Identity.Entity(uid, EntityManager);
            RemComp<RevolutionaryComponent>(uid);
            _sharedStun.TryUpdateParalyzeDuration(uid, stunTime);
            _popupSystem.PopupEntity(Loc.GetString("rev-break-control", ("name", name)), uid);
        }
    }
}
