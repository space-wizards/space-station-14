using Content.Server.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Devour;
using Content.Shared.Devour.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;

namespace Content.Server.Devour;

public sealed class DevourSystem : SharedDevourSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DevourerComponent, DevourDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DevourerComponent, BeingGibbedEvent>(OnBeingGibbed);
        SubscribeLocalEvent<DevourerComponent, ComponentRemove>(OnRemoved);
    }

    private void OnDoAfter(EntityUid uid, DevourerComponent component, DevourDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        //Store the target in the stomach if it's allowed.
        if (component.ShouldStoreDevoured && HasComp<MobStateComponent>(args.Args.Target))
        {
            ContainerSystem.Insert(args.Args.Target.Value, component.Stomach);

            EnsureComp<DevouredComponent>(args.Args.Target.Value);

            var ev = new DevouredEvent(uid);
            RaiseLocalEvent(args.Args.Target.Value, ref ev);
        }

        //Only alter the passive healing if the devoured entity belongs to the devourers food preference.
        if (component.FoodPreference == FoodPreference.All ||
            (component.FoodPreference == FoodPreference.Humanoid &&
             HasComp<HumanoidAppearanceComponent>(args.Args.Target)))
        {
            if(TryComp<PassiveDamageComponent>(uid, out var passiveHealing))
            {
                if (component.PassiveDevourHealing != null)
                {
                    passiveHealing.Damage += component.PassiveDevourHealing;
                }
            }
        }

        //TODO: Figure out a better way of removing structures via devour that still entails standing still and waiting for a DoAfter. Somehow.
        //If it does not have a mobState, it must be a structure
        else if (args.Args.Target != null)
        {
            QueueDel(args.Args.Target.Value);
        }

        AudioSystem.PlayPvs(component.SoundDevour, uid);
    }

    /// <summary>
    /// Removes the devoured component from entities in stomach so it's effects can be removed.
    /// Empties the stomach so the entities in it don't get deleted together with the dragon.
    /// </summary>
    public void EmptyStomach(EntityUid uid, DevourerComponent component)
    {
        foreach (var entity in component.Stomach.ContainedEntities)
        {
            RemComp<DevouredComponent>(entity);
        }
        ContainerSystem.EmptyContainer(component.Stomach);
    }

    /// <summary>
    /// Empties the stomach when gibbed.
    /// </summary>
    private void OnBeingGibbed(EntityUid uid, DevourerComponent component, BeingGibbedEvent args)
    {
        EmptyStomach(uid, component);
    }

    /// <summary>
    /// Empties the stomach when component is removed.
    /// </summary>
    private void OnRemoved(EntityUid uid, DevourerComponent component, ComponentRemove args)
    {
        EmptyStomach(uid,component);
    }
}
