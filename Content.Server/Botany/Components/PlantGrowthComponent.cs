using System.Security.Policy;

namespace Content.Server.Botany.Components;

[RegisterComponent]
[Virtual]
public partial class PlantGrowthComponent : Component {
    /// <summary>
    /// Creates a copy of this component.
    /// </summary>
    public PlantGrowthComponent DupeComponent()
    {
        return (PlantGrowthComponent)this.MemberwiseClone();
    }
}


