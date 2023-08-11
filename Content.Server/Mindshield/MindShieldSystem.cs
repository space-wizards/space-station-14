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
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MindShieldComponent, ComponentAdd>(MindShieldAdded);
    }

    /// <summary>
    /// When the MindShield is added this will trigger to check if the implanted is a Rev or Head Rev and will remove Rev or "destroy" implant respectively.
    /// </summary>
    private void MindShieldAdded(EntityUid uid, MindShieldComponent comp, ComponentAdd add)
    {
        if (HasComp<RevolutionaryComponent>(uid) && !HasComp<HeadRevolutionaryComponent>(uid))
        {
            var mind = _mind.GetMind(uid);
            var name = Identity.Entity(uid, EntityManager);
            if (mind != null)
            {
                if (mind.OwnedEntity != null)
                {
                    RemComp<RevolutionaryComponent>(mind.OwnedEntity.Value);
                    _popup.PopupEntity(Loc.GetString("rev-break-control", ("name", name)), mind.OwnedEntity.Value);
                }
            }
        }
        else if (HasComp<HeadRevolutionaryComponent>(uid))
        {
            var mind = _mind.GetMind(uid);
            if (mind != null)
            {
                if (mind.OwnedEntity != null)
                {
                    RemComp<MindShieldComponent>(mind.OwnedEntity.Value);
                    _popup.PopupEntity(Loc.GetString("head-rev-break-mindshield"), mind.OwnedEntity.Value);
                }
            }
        }
    }
}
