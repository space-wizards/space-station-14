using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Chemistry.Systems;
using Content.Shared.Devour;
using Content.Shared.Devour.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;

namespace Content.Server.Devour;

public sealed class DevourSystem : SharedDevourSystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedChemistryRegistrySystem _chemRegistry = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DevourerComponent, DevourDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DevourerComponent, BeingGibbedEvent>(OnGibContents);
        SubscribeLocalEvent<DevourerComponent, ComponentInit>(OnDevourInit);
    }

    private void OnDevourInit(Entity<DevourerComponent> ent, ref ComponentInit args)
    {
        UpdateInjectedChem(ent);
    }

    private void UpdateInjectedChem(Entity<DevourerComponent> ent)
    {
        if (!_chemRegistry.TryGetReagentDef(ent.Comp.Chemical, out var reagent, null, true))
            return;
        ent.Comp.CachedReagentDef = reagent;
    }

    private void OnDoAfter(EntityUid uid, DevourerComponent component, DevourDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var ichorInjection = component.HealRate;

        if (component.FoodPreference == FoodPreference.All ||
            (component.FoodPreference == FoodPreference.Humanoid && HasComp<HumanoidAppearanceComponent>(args.Args.Target)))
        {
            ichorInjection *= 0.5f;

            if (component.ShouldStoreDevoured && args.Args.Target is not null)
            {
                ContainerSystem.Insert(args.Args.Target.Value, component.Stomach);
            }
            _bloodstreamSystem.TryAddToChemicals(uid,  (component.CachedReagentDef, FixedPoint2.New(ichorInjection)));
        }

        //TODO: Figure out a better way of removing structures via devour that still entails standing still and waiting for a DoAfter. Somehow.
        //If it's not human, it must be a structure
        else if (args.Args.Target != null)
        {
            QueueDel(args.Args.Target.Value);
        }

        _audioSystem.PlayPvs(component.SoundDevour, uid);
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

