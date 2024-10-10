using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Shared.Polymorph;
using Content.Shared.Polymorph.Components;
using Content.Shared.Popups;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Whitelist;

namespace Content.Shared.Polymorph.Systems;

/// <summary>
/// Handles whitelist/blacklist checking.
/// Actual polymorphing and deactivation is done serverside.
/// </summary>
public abstract class SharedChameleonProjectorSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISerializationManager _serMan = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonProjectorComponent, AfterInteractEvent>(OnInteract);
    }

    private void OnInteract(Entity<ChameleonProjectorComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not {} target)
            return;

        var user = args.User;
        args.Handled = true;

        if (IsInvalid(ent.Comp, target))
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.InvalidPopup), target, user);
            return;
        }

        _popup.PopupClient(Loc.GetString(ent.Comp.SuccessPopup), target, user);
        Disguise(ent.Comp, user, target);
    }

    /// <summary>
    /// Returns true if an entity cannot be used as a disguise.
    /// </summary>
    public bool IsInvalid(ChameleonProjectorComponent comp, EntityUid target)
    {
        return _whitelistSystem.IsWhitelistFail(comp.Whitelist, target)
            || _whitelistSystem.IsBlacklistPass(comp.Blacklist, target);
    }

    /// <summary>
    /// On server, polymorphs the user into an entity and sets up the disguise.
    /// </summary>
    public virtual void Disguise(ChameleonProjectorComponent comp, EntityUid user, EntityUid entity)
    {
    }

    /// <summary>
    /// Copy a component from the source entity/prototype to the disguise entity.
    /// </summary>
    /// <remarks>
    /// This would probably be a good thing to add to engine in the future.
    /// </remarks>
    protected bool CopyComp<T>(Entity<ChameleonDisguiseComponent> ent) where T: Component, new()
    {
        if (!GetSrcComp<T>(ent.Comp, out var src))
            return true;

        // remove then re-add to prevent a funny
        RemComp<T>(ent);
        var dest = AddComp<T>(ent);
        _serMan.CopyTo(src, ref dest, notNullableOverride: true);
        Dirty(ent, dest);
        return false;
    }

    /// <summary>
    /// Try to get a single component from the source entity/prototype.
    /// </summary>
    private bool GetSrcComp<T>(ChameleonDisguiseComponent comp, [NotNullWhen(true)] out T? src) where T: Component
    {
        src = null;
        if (TryComp(comp.SourceEntity, out src))
            return true;

        if (comp.SourceProto is not {} protoId)
            return false;

        if (!_proto.TryIndex<EntityPrototype>(protoId, out var proto))
            return false;

        return proto.TryGetComponent(out src);
    }
}

/// <summary>
/// Action event for toggling transform NoRot on a disguise.
/// </summary>
public sealed partial class DisguiseToggleNoRotEvent : InstantActionEvent
{
}

/// <summary>
/// Action event for toggling transform Anchored on a disguise.
/// </summary>
public sealed partial class DisguiseToggleAnchoredEvent : InstantActionEvent
{
}
