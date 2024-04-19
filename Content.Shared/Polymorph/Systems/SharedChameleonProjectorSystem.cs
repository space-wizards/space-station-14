using Content.Shared.Actions;
using Content.Shared.Construction.Components;
using Content.Shared.Damage;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Polymorph;
using Content.Shared.Polymorph.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Polymorph.Systems;

/// <summary>
/// Handles disguise validation, disguising and revealing.
/// Most appearance copying is done clientside.
/// </summary>
public abstract class SharedChameleonProjectorSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISerializationManager _serMan = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonDisguiseComponent, GotEquippedHandEvent>(OnDisguiseEquippedHand);
        SubscribeLocalEvent<ChameleonDisguiseComponent, ComponentShutdown>(OnDisguiseShutdown);

        SubscribeLocalEvent<ChameleonDisguisedComponent, DamageChangedEvent>(OnDamageChanged);

        SubscribeLocalEvent<ChameleonProjectorComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<ChameleonProjectorComponent, DisguiseToggleNoRotEvent>(OnToggleNoRot);
        SubscribeLocalEvent<ChameleonProjectorComponent, DisguiseToggleAnchoredEvent>(OnToggleAnchored);
        SubscribeLocalEvent<ChameleonProjectorComponent, HandDeselectedEvent>(OnDeselected);
        SubscribeLocalEvent<ChameleonProjectorComponent, GotUnequippedHandEvent>(OnUnequipped);
        SubscribeLocalEvent<ChameleonProjectorComponent, ComponentShutdown>(OnProjectorShutdown);
    }

    #region Disguise entity

    private void OnDisguiseEquippedHand(Entity<ChameleonDisguiseComponent> ent, ref GotEquippedHandEvent args)
    {
        TryReveal(ent.Comp.User);
        args.Handled = true;
    }

    private void OnDisguiseShutdown(Entity<ChameleonDisguiseComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveProvidedActions(ent.Comp.User, ent.Comp.Projector);
    }

    #endregion

    #region Disguised player

    private void OnDamageChanged(Entity<ChameleonDisguisedComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is not {} damage)
            return;

        // reveal once enough damage is taken for the disguise to reveal itself
        var total = damage.GetTotal();
        if (total > ent.Comp.Integrity)
            TryReveal((ent, ent.Comp));
        else
            ent.Comp.Integrity -= total;
    }

    #endregion

    #region Projector

    private void OnInteract(Entity<ChameleonProjectorComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not {} target)
            return;

        var user = args.User;
        args.Handled = true;

        if (_container.IsEntityInContainer(target))
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.ContainerPopup), target, user);
            return;
        }

        if (IsInvalid(ent.Comp, target))
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.InvalidPopup), target, user);
            return;
        }

        _popup.PopupClient(Loc.GetString(ent.Comp.SuccessPopup), target, user);
        Disguise(ent, user, target);
    }

    private void OnToggleNoRot(Entity<ChameleonProjectorComponent> ent, ref DisguiseToggleNoRotEvent args)
    {
        if (ent.Comp.Disguised is not {} uid)
            return;

        var xform = Transform(uid);
        _xform.SetLocalRotationNoLerp(uid, 0, xform);
        xform.NoLocalRotation = !xform.NoLocalRotation;
        args.Handled = true;
    }

    private void OnToggleAnchored(Entity<ChameleonProjectorComponent> ent, ref DisguiseToggleAnchoredEvent args)
    {
        if (ent.Comp.Disguised is not {} uid)
            return;

        var xform = Transform(uid);
        if (xform.Anchored)
            _xform.Unanchor(uid, xform);
        else
            _xform.AnchorEntity((uid, xform));

        args.Handled = true;
    }

    private void OnDeselected(Entity<ChameleonProjectorComponent> ent, ref HandDeselectedEvent args)
    {
        RevealProjector(ent);
    }

    private void OnUnequipped(Entity<ChameleonProjectorComponent> ent, ref GotUnequippedHandEvent args)
    {
        RevealProjector(ent);
    }

    private void OnProjectorShutdown(Entity<ChameleonProjectorComponent> ent, ref ComponentShutdown args)
    {
        RevealProjector(ent);
    }

    #endregion

    #region API

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
    public void Disguise(Entity<ChameleonProjectorComponent> ent, EntityUid user, EntityUid entity)
    {
        var proj = ent.Comp;

        // no spawning prediction sorry
        if (_net.IsClient)
            return;

        // reveal first to allow quick switching
        TryReveal(user);

        // add actions for controlling transform aspects
        _actions.AddAction(user, ref proj.NoRotActionEntity, proj.NoRotAction, container: ent);
        _actions.AddAction(user, ref proj.AnchorActionEntity, proj.AnchorAction, container: ent);

        proj.Disguised = user;

        var xform = Transform(user);
        var disguise = Spawn(proj.DisguiseProto, xform.Coordinates);
        var disguiseXform = Transform(disguise);
        _xform.SetParent(disguise, disguiseXform, user, xform);

        var disguised = AddComp<ChameleonDisguisedComponent>(user);
        disguised.Disguise = disguise;
        Dirty(user, disguised);

        // make disguise look real (for simple things at least)
        var meta = MetaData(entity);
        _meta.SetEntityName(disguise, meta.EntityName);
        _meta.SetEntityDescription(disguise, meta.EntityDescription);

        var comp = EnsureComp<ChameleonDisguiseComponent>(disguise);
        comp.User = user;
        comp.Projector = ent;
        comp.SourceEntity = entity;
        comp.SourceProto = Prototype(entity)?.ID;
        Dirty(disguise, comp);

        // item disguises can be picked up to be revealed, also makes sure their examine size is correct
        CopyComp<ItemComponent>((disguise, comp));

        _appearance.CopyData(entity, disguise);

        var mass = CompOrNull<PhysicsComponent>(entity)?.Mass ?? 0f;

        // let the disguise die when its taken enough damage, which then transfers to the player
        // health is proportional to mass, and capped to not be insane
        if (TryComp<MobThresholdsComponent>(disguise, out var thresholds) && TryComp<MobThresholdsComponent>(user, out var userThresholds))
        {
            // cap disguise integrity at max health so you dont have to kill beforeif the player is of flesh and blood, cap max health to theirs
            // so that when reverting damage scales 1:1 and not round removing
            var playerMax = _mobThreshold.GetThresholdForState(user, MobState.Dead, userThresholds).Float();
            var max = playerMax == 0f ? proj.MaxHealth : Math.Max(proj.MaxHealth, playerMax);
            disguised.Integrity = max;
        }
    }

    /// <summary>
    /// Removes the disguise, if the user is disguised.
    /// </summary>
    public bool TryReveal(Entity<ChameleonDisguisedComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (TryComp<ChameleonDisguiseComponent>(ent.Comp.Disguise, out var disguise)
            && TryComp<ChameleonProjectorComponent>(disguise.Projector, out var proj))
        {
            proj.Disguised = null;
        }

        var xform = Transform(ent);
        xform.NoLocalRotation = false;
        _xform.Unanchor(ent, xform);

        Del(ent.Comp.Disguise);
        RemComp<ChameleonDisguisedComponent>(ent);
        return true;
    }

    /// <summary>
    /// Reveal a projector's user, if any.
    /// </summary>
    public void RevealProjector(Entity<ChameleonProjectorComponent> ent)
    {
        if (ent.Comp.Disguised is {} user)
            TryReveal(user);
    }

    #endregion

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
