using Content.Shared.Cuffs;
using Content.Shared.Inventory;
using Content.Shared.Standing;

namespace Content.Shared.GreyStation.Cuffs;

/// <summary>
/// Makes cuff speed faster if you are wearing gloves with <see cref="FastCuffingComponent"/> and the target isn't standing up.
/// </summary>
public sealed class FastCuffingSystem : EntitySystem
{
    [Dependency] private readonly StandingStateSystem _standingState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FastCuffingComponent, InventoryRelayedEvent<ModifyCuffSpeedEvent>>(OnModifyCuffSpeed);
    }

    private void OnModifyCuffSpeed(Entity<FastCuffingComponent> ent, ref InventoryRelayedEvent<ModifyCuffSpeedEvent> args)
    {
        if (_standingState.IsDown(args.Args.Target))
            args.Args.Adjustment -= (float) ent.Comp.Reduction.TotalSeconds;
    }
}

/// <summary>
/// Raised by cuffable system on clothing to
/// </summary>
[ByRefEvent]
public record struct ModifyCuffSpeedEvent(EntityUid Target, float Adjustment = 0f) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.GLOVES;
}
