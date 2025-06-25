using System.Linq;
using Content.Server.Objectives.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Paper;
using Robust.Shared.Containers;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Conditions that are closely tied to the Tourist ghostrole.
/// </summary>
public sealed class TouristConditionsSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private EntityQuery<ContainerManagerComponent> _containerQuery;

    public override void Initialize()
    {
        base.Initialize();

        _containerQuery = GetEntityQuery<ContainerManagerComponent>();

        SubscribeLocalEvent<StampedPapersConditionComponent, ObjectiveGetProgressEvent>(OnStampedPapersGetProgress);
        SubscribeLocalEvent<DrunkInBarConditionComponent, ObjectiveGetProgressEvent>(OnDrunkInBarGetProgress);
        SubscribeLocalEvent<PetStationPetsConditionComponent, ObjectiveGetProgressEvent>(OnPetStationPetsGetProgress);
        SubscribeLocalEvent<PetStationPetsTargetComponent, InteractionSuccessEvent>(OnPetInteractionSuccessEvent);
    }

    private void OnStampedPapersGetProgress(Entity<StampedPapersConditionComponent> entity, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = StampedPapersProgress((args.MindId, args.Mind), entity.Comp, _number.GetTarget(entity));
    }

    private void OnDrunkInBarGetProgress(Entity<DrunkInBarConditionComponent> entity, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = entity.Comp.Completed ? 1 : 0;
    }

    private void OnPetStationPetsGetProgress(Entity<PetStationPetsConditionComponent> entity, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = PetStationPetsProgress(entity, _number.GetTarget(entity));
    }

    // Stamps!
    private float StampedPapersProgress(Entity<MindComponent> mind, StampedPapersConditionComponent comp, float target)
    {
        if (!_containerQuery.TryGetComponent(mind.Comp.OwnedEntity, out var currentManager))
            return 0;

        var containerStack = new Stack<ContainerManagerComponent>();
        var foundStamps = new HashSet<StampDisplayInfo>();

        // Recursively check each container for the item
        // Checks inventory, bag, implants, etc.
        do
        {
            foreach (var container in currentManager.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    if (TryComp<PaperComponent>(entity, out var paper))
                        foundStamps.UnionWith(paper.StampedBy.ToHashSet());

                    // If it is a container check its contents
                    if (_containerQuery.TryGetComponent(entity, out var containerManager))
                        containerStack.Push(containerManager);
                }
            }
        } while (containerStack.TryPop(out currentManager));

        var result = foundStamps.Count / target;
        result = Math.Clamp(result, 0, 1);
        return result;
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
                objective.Comp.PettedPets.Add(entity.Owner); // It's a hashset, so it checks for duplicates for free.
            }
        }
    }
}
