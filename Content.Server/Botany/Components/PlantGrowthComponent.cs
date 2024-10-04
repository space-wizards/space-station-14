using System.Security.Policy;

namespace Content.Server.Botany.Components;

[RegisterComponent]
public abstract partial class PlantGrowthComponent : Component {
    public PlantGrowthComponent DupeComponent()
    {
        return (PlantGrowthComponent)this.MemberwiseClone(); //TODO TEST if this carries all properties or only base class
    }
}


