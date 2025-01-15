using Content.Server.Actions;
using Content.Server.Humanoid;
using Content.Server.Inventory;
using Content.Server.Mind.Commands;
using Content.Server.Polymorph.Components;
using Content.Shared.Actions;
using Content.Shared.Buckle;
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
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Polymorph.Systems;

public sealed partial class PolymorphSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
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

    private const string RevertPolymorphId = "ActionRevertPolymorph";

    public override void Initialize()
    {
        SubscribeLocalEvent<PolymorphableComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<PolymorphedEntityComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<PolymorphableComponent, PolymorphActionEvent>(OnPolymorphActionEvent);
        SubscribeLocalEvent<PolymorphedEntityComponent, RevertPolymorphActionEvent>(OnRevertPolymorphActionEvent);

        SubscribeLocalEvent<PolymorphedEntityComponent, BeforeFullyEatenEvent>(OnBeforeFullyEaten);
        SubscribeLocalEvent<PolymorphedEntityComponent, BeforeFullySlicedEvent>(OnBeforeFullySliced);
        SubscribeLocalEvent<PolymorphedEntityComponent, DestructionEventArgs>(OnDestruction);

        InitializeCollide();
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

        UpdateCollide();
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
            action.EntityIcon = component.Parent;
            action.UseDelay = TimeSpan.FromSeconds(component.Configuration.Delay);
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

    private void OnBeforeFullyEaten(Entity<PolymorphedEntityComponent> ent, ref BeforeFullyEatenEvent args)
    {
        var (_, comp) = ent;
        if (comp.Configuration.RevertOnEat)
        {
            args.Cancel();
            Revert((ent, ent));
        }
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
    /// Polymorphs the target entity into another
    /// </summary>
    /// <param name="uid">The entity that will be transformed</param>
    /// <param name="configuration">Polymorph data</param>
    /// <returns></returns>
    public EntityUid? PolymorphEntity(EntityUid uid, PolymorphConfiguration configuration)
    {
        // if it's already morphed, don't allow it again with this condition active.
        if (!configuration.AllowRepeatedMorphs && HasComp<PolymorphedEntityComponent>(uid))
            return null;

        // If this polymorph has a cooldown, check if that amount of time has passed since the
        // last polymorph ended.
        if (TryComp<PolymorphableComponent>(uid, out var polymorphableComponent) &&
            polymorphableComponent.LastPolymorphEnd != null &&
            _gameTiming.CurTime < polymorphableComponent.LastPolymorphEnd + configuration.Cooldown)
            return null;

        // mostly just for vehicles
        _buckle.TryUnbuckle(uid, uid, true);

        var targetTransformComp = Transform(uid);

        if (configuration.PolymorphSound != null)
            _audio.PlayPvs(configuration.PolymorphSound, targetTransformComp.Coordinates);

        var child = Spawn(configuration.Entity, _transform.GetMapCoordinates(uid, targetTransformComp), rotation: _transform.GetWorldRotation(uid));

        MakeSentientCommand.MakeSentient(child, EntityManager);

        var polymorphedComp = _compFact.GetComponent<PolymorphedEntityComponent>();
        polymorphedComp.Parent = uid;
        polymorphedComp.Configuration = configuration;
        AddComp(child, polymorphedComp);

        var childXform = Transform(child);
        _transform.SetLocalRotation(child, targetTransformComp.LocalRotation, childXform);

        if (_container.TryGetContainingContainer((uid, targetTransformComp, null), out var cont))
            _container.Insert(child, cont);

        //Transfers all damage from the original to the new one
        if (configuration.TransferDamage &&
            TryComp<DamageableComponent>(child, out var damageParent) &&
            _mobThreshold.GetScaledDamage(uid, child, out var damage) &&
            damage != null)
        {
            _damageable.SetDamage(child, damageParent, damage);
        }

        if (configuration.Inventory == PolymorphInventoryChange.Transfer)
        {
            _inventory.TransferEntityInventories(uid, child);
            foreach (var hand in _hands.EnumerateHeld(uid))
            {
                _hands.TryDrop(uid, hand, checkActionBlocker: false);
                _hands.TryPickupAnyHand(child, hand);
            }
        }
        else if (configuration.Inventory == PolymorphInventoryChange.Drop)
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
        }

        if (configuration.TransferName && TryComp(uid, out MetaDataComponent? targetMeta))
            _metaData.SetEntityName(child, targetMeta.EntityName);

        if (configuration.TransferHumanoidAppearance)
        {
            _humanoid.CloneAppearance(uid, child);
        }

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, child, mind: mind);

        //Ensures a map to banish the entity to
        EnsurePausedMap();
        if (PausedMap != null)
            _transform.SetParent(uid, targetTransformComp, PausedMap.Value);

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

        var parent = component.Parent;
        if (Deleted(parent))
            return null;

        var uidXform = Transform(uid);
        var parentXform = Transform(parent);

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

        _popup.PopupEntity(Loc.GetString("polymorph-revert-popup-generic",
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

        if (!_actions.TryGetActionData(actionId, out var baseAction))
            return;

        baseAction.Icon = new SpriteSpecifier.EntityPrototype(polyProto.Configuration.Entity);
        if (baseAction is InstantActionComponent action)
            action.Event = new PolymorphActionEvent(id);
    }

    public void RemovePolymorphAction(ProtoId<PolymorphPrototype> id, Entity<PolymorphableComponent> target)
    {
        if (target.Comp.PolymorphActions == null)
            return;

        if (target.Comp.PolymorphActions.TryGetValue(id, out var val))
            _actions.RemoveAction(target, val);
    }
}
