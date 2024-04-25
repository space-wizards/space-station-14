using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.Respiration.Systems;


/// <summary>
///
/// </summary>
[Virtual]
public abstract class SharedLungsSystem : EntitySystem //Never forget the communal-lung incident of 2023
{
    [Dependency] protected readonly IPrototypeManager ProtoManager = default!;
    [Dependency] protected readonly IGameTiming GameTiming = default!;

    ///Most of the logic for this is in the server system!
    /// When atmos gets moved to shared (and hopefully predicted), then we can move the serverside simulation code back here

    /// <inheritdoc/>
    public override void Initialize()
    {
    }
}
