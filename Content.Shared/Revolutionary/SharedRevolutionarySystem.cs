using Content.Shared.IdentityManagement;
using Content.Shared.Implants;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Stunnable;
using Content.Shared.Tag;

namespace Content.Shared.Revolutionary;

public sealed class SharedRevolutionarySystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    [ValidatePrototypeId<TagPrototype>]
    public const string MindShieldTag = "MindShield";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RevolutionaryComponent, AddImplantAttemptEvent>(PreventSelfDeconvert);
        SubscribeLocalEvent<RevolutionaryComponent, PopupAfterFailedImplantEvent>(InformTargetWasSelf);
        SubscribeLocalEvent<MindShieldComponent, MapInitEvent>(MindShieldImplanted);
    }

    /// <summary>
    /// Prevents Revs from attempting to implant themselves with a mindshield.
    /// </summary>
    public void PreventSelfDeconvert(EntityUid uid, RevolutionaryComponent comp, ref AddImplantAttemptEvent ev)
    {
        if (IsMindshieldTargetSelf(ev.User, ev.Target, ev.Implant))
        {
            ev.Cancel();
        }
    }

    /// <summary>
    /// Informs the Rev why the implant failed.
    /// </summary>
    public void InformTargetWasSelf(EntityUid uid, RevolutionaryComponent comp, ref PopupAfterFailedImplantEvent ev)
    {
        if (IsMindshieldTargetSelf(ev.User, ev.Target, ev.Implant))
        {
            _popupSystem.PopupEntity(Loc.GetString("rev-fail-self-mindshield"), ev.User);
            ev.Handled = true;
        }
    }

    /// <summary>
    /// Checks if a Rev is attempting to implant themselves with a mindshield.
    /// </summary>
    public bool IsMindshieldTargetSelf(EntityUid user, EntityUid target, EntityUid implant)
    {
        return _tag.HasTag(implant, MindShieldTag) && HasComp<RevolutionaryComponent>(user) && !HasComp<HeadRevolutionaryComponent>(user) && user == target;
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
            _sharedStun.TryParalyze(uid, stunTime, true);
            _popupSystem.PopupEntity(Loc.GetString("rev-break-control", ("name", name)), uid);
        }
    }
}
