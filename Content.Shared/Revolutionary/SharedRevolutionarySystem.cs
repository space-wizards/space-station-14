using Content.Shared.IdentityManagement;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Stunnable;

namespace Content.Shared.Revolutionary;

public abstract class SharedRevolutionarySystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MindShieldComponent, MapInitEvent>(MindShieldImplanted);
        SubscribeLocalEvent<RevolutionaryComponent, FreedFromControlMessage>(FreedFromControl);
    }

    /// <summary>
    /// When the mindshield is implanted in the rev it will popup saying they were deconverted. In Head Revs it will remove the mindshield component.
    /// </summary>
    private void MindShieldImplanted(EntityUid uid, MindShieldComponent comp, MapInitEvent init)
    {
        if (HasComp<HeadRevolutionaryComponent>(uid))
        {
            RemCompDeferred<MindShieldComponent>(uid);
            return;
        }

        FreeFromControl(uid);
    }

    private void FreedFromControl(EntityUid uid, RevolutionaryComponent comp, FreedFromControlMessage ev)
    {
        FreeFromControl(uid);
    }

    private void FreeFromControl(EntityUid uid)
    {
        if (!HasComp<RevolutionaryComponent>(uid))
            return;

        var stunTime = TimeSpan.FromSeconds(4);
        var name = Identity.Entity(uid, EntityManager);
        RemComp<RevolutionaryComponent>(uid);
        _sharedStun.TryParalyze(uid, stunTime, true);
        _popupSystem.PopupEntity(Loc.GetString("rev-break-control", ("name", name)), uid);
    }
}
