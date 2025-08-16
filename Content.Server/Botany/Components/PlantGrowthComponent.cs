using System.Security.Policy;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Botany.Components;

[RegisterComponent]
[DataDefinition]
public partial class PlantGrowthComponent : Component {
    /// <summary>
    /// Creates a copy of this component.
    /// </summary>
    public PlantGrowthComponent DupeComponent()
    {
        return IoCManager.Resolve<ISerializationManager>().CreateCopy(this, notNullableOverride: true);
    }
}


