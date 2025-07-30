using System.Linq;
using Content.Shared._Starlight.Medical.Limbs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Shared.Starlight;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using static Content.Server.Power.Pow3r.PowerState;

namespace Content.Server._Starlight.Medical.Limbs;
public sealed partial class CyberLimbSystem : EntitySystem
{
    public void InitializeLimbWithItems()
    {
        base.Initialize();
        SubscribeLocalEvent<LimbWithItemsComponent, ComponentInit>(OnLimbWithItemsInit);
        SubscribeLocalEvent<LimbWithItemsComponent, ToggleLimbEvent>(OnLimbToggle);
        SubscribeLocalEvent<BodyComponent, LimbRemovedEvent<LimbWithItemsComponent>>(LimbWithItemsRemoved);

    }

    private void LimbWithItemsRemoved(Entity<BodyComponent> ent, ref LimbRemovedEvent<LimbWithItemsComponent> args)
    {
        if (args.Comp.Toggled)
        {
            var toggleLimbEvent = new ToggleLimbEvent()
            {
                Performer = ent.Owner,
            };
            OnLimbToggle((args.Limb, args.Comp), ref toggleLimbEvent);
        }
    }

    private void OnLimbToggle(Entity<LimbWithItemsComponent> ent, ref ToggleLimbEvent args)
    {
        ent.Comp.Toggled = !ent.Comp.Toggled;

        if (ent.Comp.Toggled)
        {
            foreach (var item in ent.Comp.ItemEntities)
            {
                var handId = $"{ent.Owner}_{item}";
                var hands = EnsureComp<HandsComponent>(args.Performer);
                _hands.AddHand((args.Performer, hands), handId, HandLocation.Functional);
                _hands.DoPickup(args.Performer, handId, item, hands);
                EnsureComp<UnremoveableComponent>(item);
            }
        }
        else
        {
            var container = _container.EnsureContainer<Container>(ent.Owner, "cyberlimb", out _);
            foreach (var item in ent.Comp.ItemEntities)
            {
                var handId = $"{ent.Owner}_{item}";
                RemComp<UnremoveableComponent>(item);
                var hands = EnsureComp<HandsComponent>(args.Performer);
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

    private void OnLimbWithItemsInit(Entity<LimbWithItemsComponent> limb, ref ComponentInit args)
    {
        if (limb.Comp.ItemEntities?.Count == limb.Comp.Items.Count) return;
        var container = _container.EnsureContainer<Container>(limb.Owner, "cyberlimb", out _);

        limb.Comp.ItemEntities = [.. limb.Comp.Items.Select(EnsureItem)];

        DirtyEntity(limb);

        EntityUid EnsureItem(EntProtoId proto)
        {
            var id = Spawn(proto);
            _container.Insert(_slEnt.Entity<TransformComponent, MetaDataComponent, PhysicsComponent>(id), container, force: true);
            return id;
        }
    }
}
