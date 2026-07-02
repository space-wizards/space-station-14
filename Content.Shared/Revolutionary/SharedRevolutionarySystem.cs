using Content.Shared.IdentityManagement;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Stunnable;

namespace Content.Shared.Revolutionary;

public abstract partial class SharedRevolutionarySystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedStunSystem _sharedStun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindShieldComponent, MapInitEvent>(MindShieldImplanted);
        SubscribeLocalEvent<RevolutionaryComponent, AttemptConvertRevolutionaryEvent>(OnAttemptConvert);
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

        if (HasComp<RevolutionaryComponent>(uid))
        {
            var stunTime = TimeSpan.FromSeconds(4);
            var name = Identity.Entity(uid, EntityManager);
            RemComp<RevolutionaryComponent>(uid);
            _sharedStun.TryUpdateParalyzeDuration(uid, stunTime);
            _popupSystem.PopupEntity(Loc.GetString("rev-break-control", ("name", name)), uid);
        }
    }

    private void OnAttemptConvert(Entity<RevolutionaryComponent> ent, ref AttemptConvertRevolutionaryEvent args)
    {
        args.Cancelled = true;
    }
}
