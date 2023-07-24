namespace Content.Server._FTL.ShipTracker.Rules.EndOnShipDestruction;

[RegisterComponent, Access(typeof(EndOnShipDestructionSystem))]
public sealed class EndOnShipDestructionComponent : Component
{
    public EntityUid MainShip = default!;
}
