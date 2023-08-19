using Content.Shared.Mindshield.Components;
using Content.Shared.Revolutionary.Components;
using Content.Server.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.Database;
using Content.Server.Administration.Logs;
using Content.Shared.Stunnable;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Mindshield;
/// <summary>
/// System used for checking if the implanted is a Rev or Head Rev.
/// </summary>
public sealed class MindShieldSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MindShieldComponent, ComponentInit>(MindShieldAdded);
    }

    /// <summary>
    /// When the MindShield is added this will trigger to check if the implanted is a Rev or Head Rev and will remove Rev or "destroy" implant respectively.
    /// </summary>
    private void MindShieldAdded(EntityUid uid, MindShieldComponent comp, ComponentInit init)
    {
        if (HasComp<RevolutionaryComponent>(uid) && !HasComp<HeadRevolutionaryComponent>(uid))
        {
            var mind = _mindSystem.GetMind(uid);
            var stunTime = TimeSpan.FromSeconds(4);
            var name = Identity.Entity(uid, EntityManager);
            RemComp<RevolutionaryComponent>(uid);
            _sharedStun.TryParalyze(uid, stunTime, true);
            _popup.PopupEntity(Loc.GetString("rev-break-control", ("name", name)), uid);
            _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(uid)} was deconverted due to being implanted with a Mindshield.");
            //if (mind != null && _mindSystem.HasRole<RevolutionaryRole>(mind))
            //{
            //    var role = new RevolutionaryRole(mind, _prototypeManager.Index<AntagPrototype>("Rev"));
            //    _mindSystem.RemoveRole(mind, role);
            //}

        }
        else if (HasComp<HeadRevolutionaryComponent>(uid))
        {
            RemComp<MindShieldComponent>(uid);
            _popup.PopupEntity(Loc.GetString("head-rev-break-mindshield"), uid);
        }
    }
}
