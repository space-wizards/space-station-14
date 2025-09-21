using Content.Shared._Starlight.Medical.Limbs;
using Content.Shared.Body.Part;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Shared.Starlight;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using static Content.Server._Starlight.Actions.EntitySystems.SLActionSystem;

namespace Content.Server._Starlight.Medical.Limbs;
public sealed partial class CyberLimbSystem : EntitySystem
{
    public void InitializeLimbWithItems()
    {
        SubscribeLocalEvent<LimbItemDeployerComponent, ToggleLimbEvent>(OnLimbToggle);
        SubscribeLocalEvent<LimbItemDeployerComponent, LimbPreDetachEvent>(LimbWithItemsRemoved);
    }

    private void LimbWithItemsRemoved(Entity<LimbItemDeployerComponent> ent, ref LimbPreDetachEvent args)
    {
        if (ent.Comp.Toggled)
        {
            var toggleLimbEvent = new ToggleLimbEvent()
            {
                Performer = ent.Owner,
            };
            OnLimbToggle((args.Limb, ent.Comp), ref toggleLimbEvent);
        }
    }

    private void OnLimbToggle(Entity<LimbItemDeployerComponent > ent, ref ToggleLimbEvent args)
    {
        if (!TryComp<LimbItemStorageComponent>(ent, out var storage))
            return;

        ent.Comp.Toggled = !ent.Comp.Toggled;

        if (ent.Comp.Toggled)
        {
            foreach (var item in storage.ItemEntities)
            {
                var handId = $"{ent.Owner}_{item}";
                var hands = EnsureComp<HandsComponent>(args.Performer);
                _hands.AddHand((args.Performer, hands), handId, HandLocation.Functional, whitelist: ent.Comp.HandWhitelist);
                _hands.DoPickup(args.Performer, handId, item, hands);
                EnsureComp<UnremoveableComponent>(item);
            }
        }
        else
        {
            var container = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.ContainerId, out _);
            foreach (var item in storage.ItemEntities)
            {
                var handId = $"{ent.Owner}_{item}";
                RemComp<UnremoveableComponent>(item);
                _container.Insert(_slEnt.Entity<TransformComponent, MetaDataComponent, PhysicsComponent>(item), container, force: true);
                _hands.RemoveHand(args.Performer, handId);
            }
        }

        if (_slEnt.TryEntity<BaseLayerIdComponent, BaseLayerIdToggledComponent, BodyPartComponent>(ent.Owner, out var limb, false)
            && _slEnt.TryEntity<HumanoidAppearanceComponent>(args.Performer, out var performer, false))
            _limb.ToggleLimbVisual(performer, limb, ent.Comp.Toggled);

        _audio.PlayPvs(ent.Comp.Sound, args.Performer);

        Dirty(ent);
    }
}
