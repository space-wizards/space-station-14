using System.Linq;
using Content.Server.Objectives.Components;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Paper;
using Content.Shared.Physics;
using Content.Shared.Strip;
using Content.Shared.Warps;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Test system until I can break them out into their own systems
/// </summary>
public sealed class TouristConditionsSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedStrippableSystem _strippable = default!;

    private EntityQuery<ContainerManagerComponent> _containerQuery;
    private EntityQuery<UseNearObjectiveTargetComponent> _useNearObjectiveTargetQuery;

    public override void Initialize()
    {
        base.Initialize();

        _containerQuery = GetEntityQuery<ContainerManagerComponent>();
        _useNearObjectiveTargetQuery = GetEntityQuery<UseNearObjectiveTargetComponent>();

        SubscribeLocalEvent<StampedPapersConditionComponent, ObjectiveGetProgressEvent>(OnStampedPapersGetProgress);
        SubscribeLocalEvent<EatSpecificFoodConditionComponent, ObjectiveGetProgressEvent>(OnEatSpecificFoodGetProgress);
        SubscribeLocalEvent<DrunkInBarConditionComponent, ObjectiveGetProgressEvent>(OnDrunkInBarGetProgress);
        SubscribeLocalEvent<EatSpecificFoodConditionComponent, ObjectiveAfterAssignEvent>(OnEatSpecificFoodAfterAssign);
        SubscribeLocalEvent<UseNearObjectiveConditionComponent, ObjectiveGetProgressEvent>(OnUseNearEntityGetProgress);
        SubscribeLocalEvent<UseNearObjectiveConditionComponent, ObjectiveAssignedEvent>(OnUseNearEntityRequirementCheck, after: new[] { typeof(PickObjectiveTargetSystem) });
        SubscribeLocalEvent<UseNearObjectiveConditionComponent, ObjectiveAfterAssignEvent>(OnUseNearAfterAssign);
        SubscribeLocalEvent<UseNearObjectiveTriggerComponent, UseInHandEvent>(OnTriggerUseInHand);
        SubscribeLocalEvent<PetStationPetsConditionComponent, ObjectiveGetProgressEvent>(OnPetStationPetsGetProgress);
        SubscribeLocalEvent<PetStationPetsTargetComponent, InteractionSuccessEvent>(OnPetInteractionSuccessEvent);
    }

    // Stamps!

    private void OnStampedPapersGetProgress(Entity<StampedPapersConditionComponent> entity, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = StampedPapersProgress((args.MindId, args.Mind), entity.Comp, _number.GetTarget(entity));
    }

    private void OnEatSpecificFoodGetProgress(Entity<EatSpecificFoodConditionComponent> entity, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = EatSpecificFoodProgress(entity, _number.GetTarget(entity));
    }

    private void OnDrunkInBarGetProgress(Entity<DrunkInBarConditionComponent> entity, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = entity.Comp.Completed ? 1 : 0;
    }

    private void OnUseNearEntityGetProgress(Entity<UseNearObjectiveConditionComponent> entity, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = entity.Comp.ObjectiveCompleted ? 1 : 0;

        // Check for if the target entity has been deleted while the objective isn't completed.
        // Helps inform the objective holder if their objective has become impossible.
        if (!entity.Comp.ObjectiveCompleted && entity.Comp.TargetSingleEntity)
        {
            var deleted = Deleted(entity.Comp.TargetEntity);
            if (deleted && !entity.Comp.TargetEntityDeleted)
            {
                entity.Comp.TargetEntityDeleted = true;
                if (entity.Comp.DescriptionTextDeleted != null)
                    _metaData.SetEntityDescription(entity.Owner, Loc.GetString(entity.Comp.DescriptionTextDeleted, ("itemName", entity.Comp.LocalizedName)));
            }
            else if (!deleted && entity.Comp.TargetEntityDeleted)
            {
                entity.Comp.TargetEntityDeleted = false;
                if (entity.Comp.DescriptionText != null)
                    _metaData.SetEntityDescription(entity.Owner, Loc.GetString(entity.Comp.DescriptionText, ("itemName", entity.Comp.LocalizedName)));
            }
        }
    }

    private void OnPetStationPetsGetProgress(Entity<PetStationPetsConditionComponent> entity, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = PetStationPetsProgress(entity, _number.GetTarget(entity));
    }

    private void OnUseNearEntityRequirementCheck(Entity<UseNearObjectiveConditionComponent> entity, ref ObjectiveAssignedEvent args)
    {
        if (args.Cancelled)
            return;

        // If there's a target objective (i.e. a player), that takes priority.
        if (TryComp<TargetObjectiveComponent>(entity, out var targetObjective))
        {
            if (!TryComp<MindComponent>(targetObjective.Target, out var targetMind) || targetMind.OwnedEntity == null)
            {
                args.Cancelled = true;
                return;
            }

            entity.Comp.TargetEntity = targetMind.OwnedEntity;
            entity.Comp.TargetSingleEntity = true;
            EnsureComp<UseNearObjectiveTargetComponent>(entity.Comp.TargetEntity.Value);
            return;
        }

        // Choose an entity eligible to be selected.
        var eligibleTargets = new List<EntityUid>();
        var query = EntityQueryEnumerator<UseNearObjectiveTargetComponent>();
        while (query.MoveNext(out var targetEntity, out _))
        {
            if (_whitelist.IsWhitelistPass(entity.Comp.TargetWhitelist, targetEntity))
                eligibleTargets.Add(targetEntity);
        }

        if (eligibleTargets.Count <= 0)
        {
            // No eligible targets, so just cancel the objective.
            args.Cancelled = true;
            return;
        }

        if (entity.Comp.TargetSingleEntity)
            entity.Comp.TargetEntity = _random.Pick(eligibleTargets);
    }

    private void OnTriggerUseInHand(Entity<UseNearObjectiveTriggerComponent> entity, ref UseInHandEvent args)
    {
        if (_mind.TryGetObjectiveEntities<UseNearObjectiveConditionComponent>(args.User, out var objectives))
        {
            var nearbyTargets = new HashSet<EntityUid>();
            foreach (var nearbyEntity in _entityLookup.GetEntitiesInRange(entity, entity.Comp.Range))
            {
                if (_useNearObjectiveTargetQuery.HasComponent(nearbyEntity))
                    nearbyTargets.Add(nearbyEntity);
            }

            foreach (var objective in objectives)
            {
                if (_whitelist.IsWhitelistPass(objective.Comp.UseWhitelist, entity))
                {
                    // Single entity target
                    if (objective.Comp.TargetSingleEntity)
                    {
                        if (objective.Comp.TargetEntity == null)
                            continue;

                        if (nearbyTargets.Contains(objective.Comp.TargetEntity.Value) &&
                            (!objective.Comp.PreventOcclusion || VisibleInRange(args.User, objective.Comp.TargetEntity.Value, entity.Comp.Range)))
                        {
                            objective.Comp.ObjectiveCompleted = true;
                            continue;
                        }
                    }
                    else
                    {
                        // Multiple possible targets
                        foreach (var target in nearbyTargets)
                        {
                            if (_whitelist.IsWhitelistPass(objective.Comp.TargetWhitelist, target) &&
                                (!objective.Comp.PreventOcclusion || VisibleInRange(args.User, target, entity.Comp.Range)))
                            {
                                objective.Comp.ObjectiveCompleted = true;
                                continue;
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if an entity is visible to another entity IC; includes items worn by other players.
    /// </summary>
    private bool VisibleInRange(EntityUid viewer, EntityUid target, float range)
    {
        if (Deleted(target))
            return false;

        if (_examine.InRangeUnOccluded(viewer, target, range) && _container.IsInSameOrTransparentContainer((viewer, Transform(viewer)), (viewer, Transform(target))))
            return true;

        if (!_container.TryGetContainingContainer(target, out var container))
            return false;

        var wearer = container.Owner;
        if (!_inventory.TryGetSlot(wearer, container.ID, out var slotDef) && !_hands.IsHolding(wearer, target))
            return false;

        if (slotDef != null && _strippable.IsStripHidden(slotDef, viewer))
            return false;

        if (_examine.InRangeUnOccluded(viewer, wearer, range))
            return false;

        return true;
    }

    /// <summary>
    /// Sets the name, description and icon for the objective.
    /// </summary>
    private void OnEatSpecificFoodAfterAssign(Entity<EatSpecificFoodConditionComponent> condition, ref ObjectiveAfterAssignEvent args)
    {
        var count = _number.GetTarget(condition.Owner);

        var localizedName = Loc.GetString(condition.Comp.Name);

        var description = count > 1
            ? Loc.GetString(condition.Comp.DescriptionTextMultiple, ("itemName", localizedName), ("count", count))
            : Loc.GetString(condition.Comp.DescriptionText, ("itemName", localizedName));

        _metaData.SetEntityName(condition.Owner, Loc.GetString(condition.Comp.TitleText, ("itemName", localizedName)), args.Meta);
        _metaData.SetEntityDescription(condition.Owner, description, args.Meta);
        _objectives.SetIcon(condition.Owner, condition.Comp.Sprite, args.Objective);
    }

    /// <summary>
    /// Sets the name, description and icon for the objective.
    /// </summary>
    private void OnUseNearAfterAssign(Entity<UseNearObjectiveConditionComponent> condition, ref ObjectiveAfterAssignEvent args)
    {
        // Get the name of the objective
        if (condition.Comp.Name != null)
        {
            condition.Comp.LocalizedName = Loc.GetString(condition.Comp.Name);
        }
        else
        {
            if (condition.Comp.TargetEntity != null)
            {
                if (TryComp<WarpPointComponent>(condition.Comp.TargetEntity, out var warp) && warp.Location != null)
                    condition.Comp.LocalizedName = warp.Location;
                else
                    condition.Comp.LocalizedName = MetaData(condition.Comp.TargetEntity.Value).EntityName;
            }
        }

        if (condition.Comp.TitleText != null)
            _metaData.SetEntityName(condition.Owner, Loc.GetString(condition.Comp.TitleText, ("itemName", condition.Comp.LocalizedName)), args.Meta);

        if (condition.Comp.DescriptionText != null)
            _metaData.SetEntityDescription(condition.Owner, Loc.GetString(condition.Comp.DescriptionText, ("itemName", condition.Comp.LocalizedName)), args.Meta);

        if (condition.Comp.Sprite != null)
            _objectives.SetIcon(condition.Owner, condition.Comp.Sprite, args.Objective);
    }



    private float StampedPapersProgress(Entity<MindComponent> mind, StampedPapersConditionComponent comp, float target)
    {
        if (!_containerQuery.TryGetComponent(mind.Comp.OwnedEntity, out var currentManager))
            return 0;

        var containerStack = new Stack<ContainerManagerComponent>();
        var foundStamps = new HashSet<StampDisplayInfo>();

        // recursively check each container for the item
        // checks inventory, bag, implants, etc.
        do
        {
            foreach (var container in currentManager.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    if (TryComp<PaperComponent>(entity, out var paper))
                        foundStamps.UnionWith(paper.StampedBy.ToHashSet());

                    // if it is a container check its contents
                    if (_containerQuery.TryGetComponent(entity, out var containerManager))
                        containerStack.Push(containerManager);
                }
            }
        } while (containerStack.TryPop(out currentManager));

        var result = foundStamps.Count / target;
        result = Math.Clamp(result, 0, 1);
        return result;
    }

    private float EatSpecificFoodProgress(EatSpecificFoodConditionComponent comp, int target)
    {
        // Prevent divide-by-zero
        if (target == 0)
            return 1f;

        return MathF.Min(comp.FoodEaten / (float) target, 1f);
    }

    private float PetStationPetsProgress(PetStationPetsConditionComponent comp, int target)
    {
        // Prevent divide-by-zero
        if (target == 0)
            return 1f;

        return MathF.Min(comp.PettedPets.Count / (float) target, 1f);
    }

    private void OnPetInteractionSuccessEvent(Entity<PetStationPetsTargetComponent> entity, ref InteractionSuccessEvent args)
    {
        if (_mind.TryGetObjectiveEntities<PetStationPetsConditionComponent>(args.User, out var objectives))
        {
            foreach (var objective in objectives)
            {
                objective.Comp.PettedPets.Add(entity.Owner); // It's a hashset, so it checks for free.
            }
        }
    }
}
