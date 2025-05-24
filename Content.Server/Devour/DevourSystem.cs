using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Devour;
using Content.Shared.Devour.Components;
using Content.Shared.Humanoid;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Server.Devour;

public sealed class DevourSystem : SharedDevourSystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly BodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DevourerComponent, DevourDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DevourerComponent, BeingGibbedEvent>(OnGibContents);
    }

    private void OnDoAfter(Entity<DevourerComponent> entity, ref DevourDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        // Heal the devourer if the target is one of its favored foods.
        if (entity.Comp.FoodPreference == FoodPreference.All ||
            (entity.Comp.FoodPreference == FoodPreference.Humanoid &&
             HasComp<HumanoidAppearanceComponent>(args.Args.Target)))
        {
            var ichorInjection = new Solution(entity.Comp.Chemical, entity.Comp.HealRate);
            _bloodstreamSystem.TryAddToChemicals(entity, ichorInjection);
        }

        // Either put the entity into the devourer's stomach or delete it.
        if (args.Args.Target is { } target)
        {
            InsertEntityToDevourerStomachOrDelete(entity, target);
        }

        _audioSystem.PlayPvs(entity.Comp.SoundDevour, entity);
    }

    private void InsertEntityToDevourerStomachOrDelete(DevourerComponent devourer, EntityUid target)
    {
        if (devourer.ShouldStoreDevoured)
        {
            // Humanoids go into the stomach entirely
            if (HasComp<HumanoidAppearanceComponent>(target))
            {
                ContainerSystem.Insert(target, devourer.Stomach);
                return;
            }

            // Borgs get gibbed and their brain goes in the stomach
            if (TryComp<BorgChassisComponent>(target, out var borg))
            {
                if (borg.BrainEntity is { } brain)
                {
                    ContainerSystem.Insert(brain, devourer.Stomach);
                }
                _body.GibBody(target);

                return;
            }
        }

        // Everything else gets deleted.
        QueueDel(target);
    }

    private void OnGibContents(EntityUid uid, DevourerComponent component, ref BeingGibbedEvent args)
    {
        if (!component.ShouldStoreDevoured)
            return;

        // For some reason we have two different systems that should handle gibbing,
        // and for some another reason GibbingSystem, which should empty all containers, doesn't get involved in this process
        ContainerSystem.EmptyContainer(component.Stomach);
    }
}
