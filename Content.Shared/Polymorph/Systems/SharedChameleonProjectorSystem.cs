using Content.Shared.Interaction;
using Content.Shared.Polymorph;
using Content.Shared.Polymorph.Components;
using Content.Shared.Popups;

namespace Content.Shared.Polymorph.Systems;

/// <summary>
/// Handles whitelist/blacklist checking.
/// Actual polymorphing and deactivation is done serverside.
/// </summary>
public abstract class SharedChameleonProjectorSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonProjectorComponent, AfterInteractEvent>(OnInteract);
    }

    private void OnInteract(Entity<ChameleonProjectorComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not {} target)
            return;

        var user = args.User;
        args.Handled = true;

        if (IsInvalid(ent.Comp, target))
        {
            _popup.PopupPredicted(Loc.GetString(ent.Comp.InvalidPopup), target, user);
            return;
        }

        _popup.PopupPredicted(Loc.GetString(ent.Comp.SuccessPopup), target, user);
        Disguise(ent.Comp.Polymorph, user, target);
    }

    /// <summary>
    /// Returns true if an entity cannot be used as a disguise.
    /// </summary>
    public bool IsInvalid(ChameleonProjectorComponent comp, EntityUid target)
    {
        return (comp.Whitelist?.IsValid(target, EntityManager) == false)
            || (comp.Blacklist?.IsValid(target, EntityManager) == true);
    }

    /// <summary>
    /// On server, polymorphs the user into an entity and sets up the disguise.
    /// </summary>
    public virtual void Disguise(PolymorphConfiguration config, EntityUid user, EntityUid entity)
    {
    }
}
