using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <summary>
///     Base class for procedural variation rule passes, which apply some kind of variation to a station,
///     so we simply reduce the boilerplate for the event handling a bit with this.
/// </summary>
public abstract class VariationPassSystem<T> : GameRuleSystem<T>
    where T: IComponent
{
    protected readonly StationSystem Stations = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, StationVariationPassEvent>(ApplyVariation);
    }

    protected abstract void ApplyVariation(Entity<T> ent, ref StationVariationPassEvent args);
}
