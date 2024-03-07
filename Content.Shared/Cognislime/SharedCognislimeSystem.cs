using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Interaction;
using Content.Shared.Whitelist;
using Content.Shared.Popups;

namespace Content.Shared.Cognislime;

/// <summary>
/// Makes objects sentient.
/// </summary>
public abstract class SharedCognislimeSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CognislimeComponent, AfterInteractEvent>(OnCognislimeAfterInteract);
    }
    public void OnCognislimeAfterInteract(EntityUid uid, CognislimeComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || args.Handled || !args.CanReach)
            return;

        if (!CheckTarget(args.Target.Value, component.Whitelist, component.Blacklist))
        {
            _popup.PopupEntity(Loc.GetString("cognislime-invalid"), args.Target.Value, args.User);
            return;
        }

        args.Handled = true;

        TryApplyCognizine(component, args.User, args.Target.Value, uid);
    }
    public bool CheckTarget(EntityUid target, EntityWhitelist? whitelist, EntityWhitelist? blacklist)
    {
        return whitelist?.IsValid(target, EntityManager) != false &&
            blacklist?.IsValid(target, EntityManager) != true;
    }

    public void TryApplyCognizine(CognislimeComponent component, EntityUid user, EntityUid target, EntityUid cognislime)
    {
        var ev = new CognislimeDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, user, component.ApplyCognislimeDuration, ev, cognislime, target: target, used: cognislime)
        {
            BreakOnUserMove = true,
            BreakOnTargetMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (_doAfter.TryStartDoAfter(args))
            _popup.PopupEntity(Loc.GetString("cognislime-applying"), target, user);
    }

    [Serializable, NetSerializable]
    public sealed partial class CognislimeDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
