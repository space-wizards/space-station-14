using Robust.Shared.Serialization.Manager;

namespace Content.Server.Botany.Components;

/// <summary>
/// Base class for plant growth components.
/// </summary>
public abstract partial class PlantGrowthComponent : Component
{
    /// <summary>
    /// Creates a copy of this growth components.
    /// </summary>
    public PlantGrowthComponent DupeComponent()
    {
        return IoCManager.Resolve<ISerializationManager>().CreateCopy(this, notNullableOverride: true);
    }
}
