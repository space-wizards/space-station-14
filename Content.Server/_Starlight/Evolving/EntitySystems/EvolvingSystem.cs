using System.Linq;
using Content.Shared._Starlight.Evolving;
using Content.Shared._Starlight.Weapons.Melee.Events;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Server._Starlight.Evolving.Conditions;
using Content.Shared.Actions;
using Content.Shared._Starlight.Antags.TerrorSpider;
using Content.Shared._Starlight.Spider.Events;

namespace Content.Server._Starlight.Evolving.EntitySystems;

public sealed class EvolvingSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EvolvingComponent, EvolveEvent>(OnEvolve);

        // Watchers

        SubscribeLocalEvent<EvolvingComponent, AfterMeleeHitEvent>(AfterMeleeHit); // Damage Deal Condition
        SubscribeLocalEvent<EvolvingComponent, EggsInjectedEvent>(OnEggsInjected); // Eggs Inject Condition
        SubscribeLocalEvent<EvolvingComponent, SpiderWebSpawnedEvent>(OnSpiderWebSpawn);
    }

    // TODO: Make all of this shit generalized, so you don't just copy and paste.
    #region Watchers
    private void AfterMeleeHit(EntityUid uid, EvolvingComponent component, AfterMeleeHitEvent args)
    {
        if (args.Handled || args.HitEntities.Count <= 0)
            return;

        foreach (var condition in component.Conditions)
        {
            if (condition is DamageDealCondition damageDealCondition)
            {
                if (damageDealCondition.Condition()) // If condition already met, we don't need to update it and do unnecessary linq operations
                    continue;
                if (!damageDealCondition.OnlyAlive
                    || args.HitEntities.All(entity => _mobStateSystem.IsAlive(entity))) // Melee hit event don't have separated damage amount, so...
                    damageDealCondition.AddDamage(args.DealedDamage.GetTotal().Float()); // Just update the damage dealt if all alive/we don't need only alive.
            }
        }
        TryAddAction(uid, component); // So when we update damage, we can try to add action if we can.
    }

    private void OnEggsInjected(EntityUid uid, EvolvingComponent component, EggsInjectedEvent args)
    {
        foreach (var condition in component.Conditions)
        {
            if (condition is EggsInjectCondition eggsInject)
            {
                if (eggsInject.Condition())
                    continue;
                eggsInject.UpdateEggs(1);
            }
        }

        TryAddAction(uid, component);
    }

    private void OnSpiderWebSpawn(EntityUid uid, EvolvingComponent component, SpiderWebSpawnedEvent args)
    {
        foreach (var condition in component.Conditions)
        {
            if (condition is SpiderWebCondition spiderWeb)
            {
                if (spiderWeb.Condition())
                    continue;
                spiderWeb.UpdateWebs(1);
            }
        }

        TryAddAction(uid, component);
    }

    #endregion

    #region Logic

    /// <summary>
    /// Tries to add evolve action to the entity if it can.
    /// </summary>
    /// <param name="uid">Target Entity</param>
    /// <param name="component">Evolving Component</param>
    /// <returns>Whether or not the action added</returns>
    private bool TryAddAction(EntityUid uid, EvolvingComponent component)
    {
        if (component.EvolveActionEntity != null // If EvolveActionEntity not null, we already added action.
            || !CanEvolve(uid, component))
            return false;

        component.EvolveActionEntity = _actionsSystem.AddAction(uid, component.EvolveActionId);

        return component.EvolveActionEntity != null;
    }

    /// <summary>
    ///   Tries to evolve the entity if it can.
    /// </summary>
    /// <param name="uid">Entity which evolve to...</param>
    /// <param name="component">Evolving Component</param>
    /// <returns>Whether or not the entity evolved</returns>
    private bool TryEvolve(EntityUid uid, EvolvingComponent component)
    {
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind)
            || !CanEvolve(uid, component))
            return false;

        // All conditions met, evolve.
        var ent = EntityManager.SpawnEntity(component.EvolveTo, Transform(uid).Coordinates);
        _mindSystem.TransferTo(mindId, ent, mind: mind);
        QueueDel(uid);
        return true;
    }

    private void OnEvolve(EntityUid uid, EvolvingComponent component, EvolveEvent args) => TryEvolve(uid, component);

    /// <summary>
    ///  Checks if the entity can evolve.
    /// </summary>
    /// <param name="uid">Target Entity</param>
    /// <param name="component">Evolving Component</param>
    /// <returns>Whether or not the entity can evolve</returns>
    private bool CanEvolve(EntityUid uid, EvolvingComponent component) => component.Conditions.All(c => c.Condition(new EvolvingConditionArgs(uid, component.EvolveActionEntity, EntityManager)));
    #endregion
}