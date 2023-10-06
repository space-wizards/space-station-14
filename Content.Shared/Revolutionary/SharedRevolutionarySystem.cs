using Content.Shared.Revolutionary.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;

namespace Content.Shared.Revolutionary;

public sealed class SharedRevolutionarySystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MindShieldComponent, MapInitEvent>(MindShieldImplanted);
    }

    /// <summary>
    /// When the mindshield is implanted in the rev it will popup saying they were deconverted. In Head Revs it will remove the mindshield component.
    /// </summary>
    private void MindShieldImplanted(EntityUid uid, MindShieldComponent comp, MapInitEvent init)
    {
        if (HasComp<RevolutionaryComponent>(uid) && !HasComp<HeadRevolutionaryComponent>(uid))
        {
            var stunTime = TimeSpan.FromSeconds(4);
            var name = Identity.Entity(uid, EntityManager);
            RemComp<RevolutionaryComponent>(uid);
            _sharedStun.TryParalyze(uid, stunTime, true);
            _popupSystem.PopupEntity(Loc.GetString("rev-break-control", ("name", name)), uid);
        }
        else if (HasComp<HeadRevolutionaryComponent>(uid))
        {
            RemCompDeferred<MindShieldComponent>(uid);
        }
    }
}
