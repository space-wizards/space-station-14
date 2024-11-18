using Content.Shared.Bar;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Bar;

public sealed class TransformableContainerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionsSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PolymorphGlassComponent, GetVerbsEvent<Verb>>(OnGetVerb);
    }

    private void ChangeGlass(EntityUid uid, PolymorphGlassComponent component, EntityPrototype prototype)
    {
        if (!_solutionsSystem.TryGetSolution(uid, "drink", out var sourceSolutionEntity, out var sourceSolution))
            return;

        var position = Transform(uid).Coordinates;
        var targetEntity = Spawn(prototype.ID, position);

        if (!_solutionsSystem.TryGetSolution(targetEntity, "drink", out var targetSolutionEntity, out var targetSolution))
        {
            Del(uid);
            return;
        }

        _solutionsSystem.TryTransferSolution(targetSolutionEntity.Value, sourceSolution, 30.0f);

        if (!TryComp<ItemSlotsComponent>(uid, out var sourceItemSlots))
        {
            Del(uid);
            return;
        }

        if (!_itemSlotsSystem.TryGetSlot(uid, "garnish_slot", out var sourceGarnishSlot) ||
            !_itemSlotsSystem.TryGetSlot(uid, "decoration_slot", out var sourceDecorationSlot))
        {
            Del(uid);
            return;
        }

        var outGarnish = _itemSlotsSystem.TryEject(uid, sourceGarnishSlot, null, out var sourceGarnish, true);
        var outDecoration = _itemSlotsSystem.TryEject(uid, sourceDecorationSlot, null, out var sourceDecoration, true);

        if (TryComp<ItemSlotsComponent>(targetEntity, out var targetItemSlots))
        {
            if (!_itemSlotsSystem.TryGetSlot(targetEntity, "garnish_slot", out var targetGarnishSlot) ||
                !_itemSlotsSystem.TryGetSlot(targetEntity, "decoration_slot", out var targetDecorationSlot))
            {
                Del(uid);
                return;
            }

            if (outGarnish && sourceGarnish.HasValue)
                _itemSlotsSystem.TryInsert(targetEntity, "garnish_slot", sourceGarnish.Value, null, targetItemSlots, true);
            if (outDecoration && sourceDecoration.HasValue)
                _itemSlotsSystem.TryInsert(targetEntity, "decoration_slot", sourceDecoration.Value, null, targetItemSlots, true);
        }

        Del(uid);
    }

    private void OnGetVerb(EntityUid uid, PolymorphGlassComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var thisProto = Prototype(uid, Comp<MetaDataComponent>(uid));

        foreach (var glass in component.Glasses)
        {
            var proto = _prototypeManager.Index<EntityPrototype>(glass);
            if (proto == thisProto)
                continue;

            var v = new Verb
            {
                Priority = 3,
                Category = VerbCategory.SelectType,
                Text = proto.Name,
                DoContactInteraction = true,
                Act = () =>
                {
                    ChangeGlass(uid, component, proto);
                }
            };
            args.Verbs.Add(v);
        }
    }

}
