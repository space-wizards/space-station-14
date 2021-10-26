using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Supermatter.Components
{
    /// <summary>
    /// Overrides exactly how much energy this object gives to Supermatter.
    /// </summary>
    [RegisterComponent]
    public class SupermatterFoodComponent : Component
    {
        public override string Name => "SupermatterFood";
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("energy")]
        public int Energy { get; set; } = 1;
    }
}
