using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.Humanoid;
using Content.Server.Inventory;
using Content.Server.Polymorph.Components;
using Content.Shared.Buckle;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Polymorph.Systems;

public sealed partial class PolymorphSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly ServerInventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    private const string RevertPolymorphId = "ActionRevertPolymorph";

    public override void Initialize()
    {
        SubscribeLocalEvent<PolymorphableComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<PolymorphedEntityComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<PolymorphableComponent, PolymorphActionEvent>(OnPolymorphActionEvent);
        SubscribeLocalEvent<PolymorphedEntityComponent, RevertPolymorphActionEvent>(OnRevertPolymorphActionEvent);

        SubscribeLocalEvent<PolymorphedEntityComponent, BeforeFullySlicedEvent>(OnBeforeFullySliced);
        SubscribeLocalEvent<PolymorphedEntityComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<PolymorphedEntityComponent, EntityTerminatingEvent>(OnPolymorphedTerminating);

        InitializeMap();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PolymorphedEntityComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Time += frameTime;

            if (comp.Configuration.Duration != null && comp.Time >= comp.Configuration.Duration)
            {
                Revert((uid, comp));
                continue;
            }

            if (!TryComp<MobStateComponent>(uid, out var mob))
                continue;

            if (comp.Configuration.RevertOnDeath && _mobState.IsDead(uid, mob) ||
                comp.Configuration.RevertOnCrit && _mobState.IsIncapacitated(uid, mob))
            {
                Revert((uid, comp));
            }
        }
    }

    private void OnComponentStartup(Entity<PolymorphableComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.InnatePolymorphs != null)
        {
            foreach (var morph in ent.Comp.InnatePolymorphs)
            {
                CreatePolymorphAction(morph, ent);
            }
        }
    }

    private void OnMapInit(Entity<PolymorphedEntityComponent> ent, ref MapInitEvent args)
    {
        var (uid, component) = ent;
        if (component.Configuration.Forced)
            return;

        if (_actions.AddAction(uid, ref component.Action, out var action, RevertPolymorphId))
        {
            _actions.SetEntityIcon((component.Action.Value, action), component.Parent);
            _actions.SetUseDelay(component.Action.Value, TimeSpan.FromSeconds(component.Configuration.Delay));
        }
    }

    private void OnPolymorphActionEvent(Entity<PolymorphableComponent> ent, ref PolymorphActionEvent args)
    {
        if (!_proto.TryIndex(args.ProtoId, out var prototype) || args.Handled)
            return;

        PolymorphEntity(ent, prototype.Configuration);

        args.Handled = true;
    }

    private void OnRevertPolymorphActionEvent(Entity<PolymorphedEntityComponent> ent,
        ref RevertPolymorphActionEvent args)
    {
        Revert((ent, ent));
    }

    private void OnBeforeFullySliced(Entity<PolymorphedEntityComponent> ent, ref BeforeFullySlicedEvent args)
    {
        var (_, comp) = ent;
        if (comp.Configuration.RevertOnEat)
        {
            args.Cancel();
            Revert((ent, ent));
        }
    }

    /// <summary>
    /// It is possible to be polymorphed into an entity that can't "die", but is instead
    /// destroyed. This handler ensures that destruction is treated like death.
    /// </summary>
    private void OnDestruction(Entity<PolymorphedEntityComponent> ent, ref DestructionEventArgs args)
    {
        if (ent.Comp.Configuration.RevertOnDeath)
        {
            Revert((ent, ent));
        }
    }

    private void OnPolymorphedTerminating(Entity<PolymorphedEntityComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!ent.Comp.Configuration.RevertOnDelete)
            return;

        Revert(ent.AsNullable());
    }

    /// <summary>
    /// Polymorphs the target entity into the specific polymorph prototype
    /// </summary>
    /// <param name="uid">The entity that will be transformed</param>
    /// <param name="protoId">The id of the polymorph prototype</param>
    public EntityUid? PolymorphEntity(EntityUid uid, ProtoId<PolymorphPrototype> protoId)
    {
        var config = _proto.Index(protoId).Configuration;
        return PolymorphEntity(uid, config);
    }

    /// <summary>
    /// Polymorphs the target entity into another.
    /// </summary>
    /// <param name="uid">The entity that will be transformed</param>
    /// <param name="configuration">The new polymorph configuration</param>
    /// <returns>The new entity, or null if the polymorph failed.</returns>
    public EntityUid? PolymorphEntity(EntityUid uid, PolymorphConfiguration configuration)
    {
        // Assert that this entity is valid to be polymorphed.

        // If they're morphed, check their current config to see if they can be
        // morphed again.
        if (!configuration.IgnoreAllowRepeatedMorphs
            && TryComp<PolymorphedEntityComponent>(uid, out var currentPoly)
            && !currentPoly.Configuration.AllowRepeatedMorphs)
            return null;

        // If this polymorph has a cooldown, check if that amount of time has passed since the
        // last polymorph ended.
        if (TryComp<PolymorphableComponent>(uid, out var polymorphableComponent) &&
            polymorphableComponent.LastPolymorphEnd != null &&
            _gameTiming.CurTime < polymorphableComponent.LastPolymorphEnd + configuration.Cooldown)
            return null;

        // The entity's valid.
        // The entity is now being polymorphed. It should not interact with anything.
        var parentIsCollidable = false;

        if (TryComp<PhysicsComponent>(uid, out var physComp))
            parentIsCollidable = physComp.CanCollide;

        _physics.SetCanCollide(uid, false, true, true);

        // Make sure the entity is unbuckled.
        _buckle.TryUnbuckle(uid, uid, true);

        // We're going to banish the polymorph-ed entity to nullspace, so let's make sure we know where it is first.
        var targetTransformComp = Transform(uid);
        var mapCoordinates = _transform.GetMapCoordinates(uid, targetTransformComp);
        var rotation = _transform.GetWorldRotation(uid);
        _container.TryGetContainingContainer((uid, targetTransformComp, null), out var parentContainer);

        // Send the parent to baby jail. Parent jail? That room at the kid's arcade that sells alcohol?
        _transform.SetParent(uid, targetTransformComp, EnsurePausedMap());

        // Spawn the child. Which in this analogy is the snotty kid in the arcade.
        var child = Spawn(configuration.Entity, mapCoordinates, rotation: rotation);

        // The child needs to know that it's a polymorph.
        var polymorphedComp = Factory.GetComponent<PolymorphedEntityComponent>();
        polymorphedComp.Parent = uid;
        polymorphedComp.Configuration = configuration;
        polymorphedComp.ParentWasCollidable = parentIsCollidable;
        AddComp(child, polymorphedComp);

        // The child needs to be sentient so we can transfer the parent's mind over.
        _mindSystem.MakeSentient(child);

        // Make sure the child is rotated to match the parent. Important in a few situations, such as when a Wizard
        // polymorphs into an Immovable Rod.
        var childXform = Transform(child);
        _transform.SetLocalRotation(child, targetTransformComp.LocalRotation, childXform);

        // Make sure the child is in the same container the parent was supposed to be in.
        if (parentContainer is not null)
            _container.Insert(child, parentContainer);

        if (configuration.TransferDamage)
        {
            if (_mobThreshold.GetScaledDamage(uid, child, out var damage) && damage is not null)
                //Transfer all damage from the original to the new one.
                // Child _must_ have a damage comp here (as otherwise GetScaledDamage would have returned null for damage).
                _damageable.SetDamage(child, Comp<DamageableComponent>(child), damage);
        }

        // Handle inventory management.
        switch (configuration.Inventory)
        {
            case PolymorphInventoryChange.Transfer:
            {
                _inventory.TransferEntityInventories(uid, child);

                foreach (var hand in _hands.EnumerateHeld(uid))
                {
                    _hands.TryDrop(uid, hand, checkActionBlocker: false);
                    _hands.TryPickupAnyHand(child, hand);
                }

                break;
            }
            case PolymorphInventoryChange.Drop:
            {
                if (_inventory.TryGetContainerSlotEnumerator(uid, out var enumerator))
                {
                    while (enumerator.MoveNext(out var slot))
                    {
                        _inventory.TryUnequip(uid, slot.ID, true, true);
                    }
                }

                foreach (var held in _hands.EnumerateHeld(uid))
                {
                    _hands.TryDrop(uid, held);
                }

                break;
            }
            case PolymorphInventoryChange.None:
            default:
                break;
        }

        if (configuration.TransferName && TryComp(uid, out MetaDataComponent? targetMeta))
            _metaData.SetEntityName(child, targetMeta.EntityName);

        if (configuration.TransferHumanoidAppearance)
            _humanoid.CloneAppearance(uid, child);

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, child, mind: mind);

        // Raise an event to inform anything that wants to know about the entity swap
        var ev = new PolymorphedEvent(uid, child, false);
        RaiseLocalEvent(uid, ref ev);

        // Polymorph OK!

        // Spawn sidecar effects; popups, sounds, etc.

        if (configuration.EffectProto is not null)
            SpawnAttachedTo(configuration.EffectProto, child.ToCoordinates());

        if (configuration.PolymorphPopup is not null)
        {
            _popup.PopupEntity(Loc.GetString(configuration.PolymorphPopup,
                    ("parent", Identity.Entity(uid, EntityManager)),
                    ("child", Identity.Entity(child, EntityManager))),
                child);
        }

        if (configuration.PolymorphSound is not null)
            _audio.PlayEntity(configuration.PolymorphSound, Filter.Pvs(child), child, true);

        if (configuration.SayOnPolymorph is not null)
            _chat.TrySendInGameICMessage(child, Loc.GetString(configuration.SayOnPolymorph), InGameICChatType.Speak, false, false);

        return child;
    }

    /// <summary>
    /// Reverts a polymorphed entity back into its original form
    /// </summary>
    /// <param name="uid">The entityuid of the entity being reverted</param>
    /// <param name="component"></param>
    public EntityUid? Revert(Entity<PolymorphedEntityComponent?> ent)
    {
        var (uid, component) = ent;
        if (!Resolve(ent, ref component))
            return null;

        if (Deleted(uid))
            return null;

        if (component.Parent is not { } parent)
            return null;

        if (Deleted(parent))
            return null;

        var uidXform = Transform(uid);
        var parentXform = Transform(parent);

        // Don't swap back onto a terminating grid
        if (TerminatingOrDeleted(uidXform.ParentUid))
            return null;

        // Beyond this point: reverting is legal and can now happen.

        // Because the entity the parent has polymorphed into now does not exist, it
        // needs its fixtures removing immediately so it will not interact with the
        // parent as they are rescued from nullspace.
        _physics.SetCanCollide(uid, false, dirty: true, force: true);

        // The polymorphed entity may have been collidable. Make sure it has the same state it did before it was
        // sent to baby jail.
        _physics.SetCanCollide(uid, component.ParentWasCollidable, true, true);

        if (component.Configuration.ExitPolymorphSound != null)
            _audio.PlayPvs(component.Configuration.ExitPolymorphSound, uidXform.Coordinates);

        _transform.SetParent(parent, parentXform, uidXform.ParentUid);
        _transform.SetCoordinates(parent, parentXform, uidXform.Coordinates, uidXform.LocalRotation);

        if (component.Configuration.TransferDamage &&
            TryComp<DamageableComponent>(parent, out var damageParent) &&
            _mobThreshold.GetScaledDamage(uid, parent, out var damage) &&
            damage != null)
        {
            _damageable.SetDamage(parent, damageParent, damage);
        }

        if (component.Configuration.Inventory == PolymorphInventoryChange.Transfer)
        {
            _inventory.TransferEntityInventories(uid, parent);
            foreach (var held in _hands.EnumerateHeld(uid))
            {
                _hands.TryDrop(uid, held);
                _hands.TryPickupAnyHand(parent, held, checkActionBlocker: false);
            }
        }
        else if (component.Configuration.Inventory == PolymorphInventoryChange.Drop)
        {
            if (_inventory.TryGetContainerSlotEnumerator(uid, out var enumerator))
            {
                while (enumerator.MoveNext(out var slot))
                {
                    _inventory.TryUnequip(uid, slot.ID);
                }
            }

            foreach (var held in _hands.EnumerateHeld(uid))
            {
                _hands.TryDrop(uid, held);
            }
        }

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, parent, mind: mind);

        if (TryComp<PolymorphableComponent>(parent, out var polymorphableComponent))
            polymorphableComponent.LastPolymorphEnd = _gameTiming.CurTime;

        // if an item polymorph was picked up, put it back down after reverting
        _transform.AttachToGridOrMap(parent, parentXform);

        // Raise an event to inform anything that wants to know about the entity swap
        var ev = new PolymorphedEvent(uid, parent, true);
        RaiseLocalEvent(uid, ref ev);

        // visual effect spawn
        if (component.Configuration.EffectProto != null)
            SpawnAttachedTo(component.Configuration.EffectProto, parent.ToCoordinates());

        if (component.Configuration.ExitPolymorphPopup != null)
            _popup.PopupEntity(Loc.GetString(component.Configuration.ExitPolymorphPopup,
                ("parent", Identity.Entity(uid, EntityManager)),
                ("child", Identity.Entity(parent, EntityManager))),
                parent);
        QueueDel(uid);

        return parent;
    }

    /// <summary>
    /// Creates a sidebar action for an entity to be able to polymorph at will
    /// </summary>
    /// <param name="id">The string of the id of the polymorph action</param>
    /// <param name="target">The entity that will be gaining the action</param>
    public void CreatePolymorphAction(ProtoId<PolymorphPrototype> id, Entity<PolymorphableComponent> target)
    {
        target.Comp.PolymorphActions ??= new();
        if (target.Comp.PolymorphActions.ContainsKey(id))
            return;

        if (!_proto.TryIndex(id, out var polyProto))
            return;

        var entProto = _proto.Index(polyProto.Configuration.Entity);

        EntityUid? actionId = default!;
        if (!_actions.AddAction(target, ref actionId, RevertPolymorphId, target))
            return;

        target.Comp.PolymorphActions.Add(id, actionId.Value);

        var metaDataCache = MetaData(actionId.Value);
        _metaData.SetEntityName(actionId.Value, Loc.GetString("polymorph-self-action-name", ("target", entProto.Name)), metaDataCache);
        _metaData.SetEntityDescription(actionId.Value, Loc.GetString("polymorph-self-action-description", ("target", entProto.Name)), metaDataCache);

        if (_actions.GetAction(actionId) is not {} action)
            return;

        _actions.SetIcon((action, action.Comp), new SpriteSpecifier.EntityPrototype(polyProto.Configuration.Entity));
        _actions.SetEvent(action, new PolymorphActionEvent(id));
    }

    public void RemovePolymorphAction(ProtoId<PolymorphPrototype> id, Entity<PolymorphableComponent> target)
    {
        if (target.Comp.PolymorphActions is not {} actions)
            return;

        if (actions.TryGetValue(id, out var action))
            _actions.RemoveAction(target.Owner, action);
    }
}
