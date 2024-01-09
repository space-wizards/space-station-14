using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Devour;
using Content.Shared.Devour.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Robust.Shared.Containers;

namespace Content.Server.Devour;

public sealed class DevourSystem : SharedDevourSystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DevourerComponent, DevourDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DevourerComponent, BeingGibbedEvent>(OnBeingGibbed);
    }

    private void OnDoAfter(EntityUid uid, DevourerComponent component, DevourDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var ichorInjection = new Solution(component.Chemical, component.HealSolutionSize);

        //Inject stomach chemicals into the devoured entity so the dragon can't be attacked from the inside.
        foreach (var chemical in component.StomachChemicals)
        {
            if (args.Args.Target != null)
            {
                _bloodstreamSystem.TryAddToChemicals(args.Args.Target.Value,
                    new Solution(chemical, component.StomachSolutionSize / component.StomachChemicals.Count));
            }
        }

        if (component.FoodPreference == FoodPreference.All ||
            (component.FoodPreference == FoodPreference.Humanoid &&
             HasComp<HumanoidAppearanceComponent>(args.Args.Target)))
        {
            _bloodstreamSystem.TryAddToChemicals(uid, ichorInjection);
        }

        if (component.ShouldStoreDevoured && HasComp<MobStateComponent>(args.Args.Target))
        {
            _containerSystem.Insert(args.Args.Target.Value, component.Stomach);

            var ev = new OnDevouredEvent(args.Args.Target.Value);
            RaiseLocalEvent(args.Args.Target.Value, ref ev);
        }
        //TODO: Figure out a better way of removing structures via devour that still entails standing still and waiting for a DoAfter. Somehow.
        //If it does not have a mobState, it must be a structure
        else if (args.Args.Target != null)
        {
            QueueDel(args.Args.Target.Value);
        }

        _audioSystem.PlayPvs(component.SoundDevour, uid);
    }

    private void OnBeingGibbed(EntityUid uid, DevourerComponent component, BeingGibbedEvent args)
    {
        _containerSystem.EmptyContainer(component.Stomach);
    }
}
