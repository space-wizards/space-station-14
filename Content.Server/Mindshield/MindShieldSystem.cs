using Content.Server.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Revolutionary.Components;
using Content.Server.Popups;
using Content.Shared.IdentityManagement;

namespace Content.Server.Mindshield;
/// <summary>
/// System used for checking if the implanted is a Rev or Head Rev.
/// </summary>
public sealed class MindShieldSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;

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
            var name = Identity.Entity(uid, EntityManager);
            RemComp<RevolutionaryComponent>(uid);
            _popup.PopupEntity(Loc.GetString("rev-break-control", ("name", name)), uid);

        }
        else if (HasComp<HeadRevolutionaryComponent>(uid))
        {
            RemComp<MindShieldComponent>(uid);
            _popup.PopupEntity(Loc.GetString("head-rev-break-mindshield"), uid);
        }
    }
}
