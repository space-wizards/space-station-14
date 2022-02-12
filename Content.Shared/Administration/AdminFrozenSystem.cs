using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Movement;
using Content.Shared.Throwing;

namespace Content.Shared.Administration;

public sealed class AdminFrozenSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AdminFrozenComponent, UseAttemptEvent>((_, _, args) => args.Cancel());
        SubscribeLocalEvent<AdminFrozenComponent, PickupAttemptEvent>((_, _, args) => args.Cancel());
        SubscribeLocalEvent<AdminFrozenComponent, ThrowAttemptEvent>((_, _, args) => args.Cancel());
        SubscribeLocalEvent<AdminFrozenComponent, AttackAttemptEvent>((_, _, args) => args.Cancel());
        SubscribeLocalEvent<AdminFrozenComponent, MovementAttemptEvent>((_, _, args) => args.Cancel());
        SubscribeLocalEvent<AdminFrozenComponent, InteractionAttemptEvent>((_, _, args) => args.Cancel());
    }
}
