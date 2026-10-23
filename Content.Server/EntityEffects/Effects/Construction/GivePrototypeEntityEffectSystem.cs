using Content.Server.Stack;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Construction;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects.Construction;

/// <summary>
/// Spawns a prototype, if the user can hold it, it puts it in their hands.
/// </summary>
public sealed partial class GivePrototypeEntityEffectSystem : EntityEffectSystem<TransformComponent, GivePrototype>
{
    [Dependency] private StackSystem _stack = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<GivePrototype> args)
    {
        var prototype = args.Effect.Prototype;
        var amount = args.Effect.Amount;
        var user = args.User;

        if (ProtoMan.TryIndex(prototype, out var entProto) && entProto.HasComp<StackComponent>(Factory))
        {
            var stacks = _stack.SpawnMultipleNextToOrDrop(prototype, amount, user ?? entity);

            if (user is null || !TryComp<HandsComponent>(user, out var handsComp))
                return;

            foreach (var item in stacks)
            {
                _stack.TryMergeToHands(item, (user.Value, handsComp));
            }
        }
        else
        {
            for (var i = 0; i < amount; i++)
            {
                var item = SpawnNextToOrDrop(prototype, user ?? entity);

                if (TryComp<HandsComponent>(user, out var handsComp))
                 _hands.PickupOrDrop(user, item, handsComp: handsComp);
            }
        }
    }
}
