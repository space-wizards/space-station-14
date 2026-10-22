using Robust.Shared.GameStates;

namespace Content.Shared._MACRO.Species.Kodepiia.Consume.Components;

/// <summary>
/// Entities with this component are considered "consumed" and track how many times they've been bitten.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ConsumedComponent : Component
{
    /// <summary>
    /// How consumed this entity is, incremented by one every time they're consumed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ConsumedValue;

    /// <summary>
    ///     Examine tooltips for a partially-consumed entity, based on the level of consumption.
    /// </summary>
    /// <remarks>
    ///     All of these locales should take a "target" parameter representing this entity.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public List<KeyValuePair<float, LocId>> ExamineThresholds = new()
    {
        // [They] are missing a chunk of flesh...
        new(0.01f, "consumed-onexamine-1" ),
        // Bites mar [their] flesh.
        new(2.0f, "consumed-onexamine-2" ),
        // [Their] insides are exposed by numerous bite marks.
        new(4.0f, "consumed-onexamine-3" ),
        // [Their] body is barely strung together!
        new(8.0f, "consumed-onexamine-4" ),
    };
}
