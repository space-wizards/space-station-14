using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Shared.Cognislime;

/// <summary>
/// Makes objects sentient.
/// </summary>
public abstract partial class SharedCognislimeSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CognislimeComponent, AfterInteractEvent>(OnAfterInteractEvent);
    }
    private void OnAfterInteractEvent(Entity<CognislimeComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || args.Handled || !args.CanReach)
            return;

        if (!CheckTarget(args.Target.Value, ent.Comp.Whitelist, ent.Comp.Blacklist))
        {
            _popup.PopupEntity(Loc.GetString("cognislime-invalid"), args.Target.Value, args.User);
            return;
        }

        args.Handled = true;

        TryApplyCognizine(ent.Comp, args.User, args.Target.Value, ent);
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
