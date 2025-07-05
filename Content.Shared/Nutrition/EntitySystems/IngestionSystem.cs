using Content.Shared.Chemistry.Components;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// This handles the ingestion of food and drinks.
/// </summary>
public sealed class IngestionSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {

    }

    [ByRefEvent]
    public record struct TryIngestEvent(bool Handled, EntityUid Ingested);

    [ByRefEvent]
    public record struct EdibleEvent(bool Cancelled);
}
