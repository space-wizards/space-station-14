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
    ///TODO: move all the logic back over except for fetching the gasMixture from the tile and equalizing.
    ///Use a gasMixture on the component for the externalGasMix, then sync that from the server so we can predict shit.
    /// It will still introduce a slight desync overtime but it will be (mostly) predicted, which is good enough because
    /// the desync will generally be small and player's won't see the actual numbers

    /// <inheritdoc/>
    public override void Initialize()
    {
    }
}
