using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Weapon.Ranged.Ammunition.Components
{
    [RegisterComponent]
    public sealed class ChemicalAmmoComponent : Component
    {
        public override string Name => "ChemicalAmmo";
        public const string DefaultSolutionName = "ammo";

        [DataField("solution")]
        public string SolutionName { get; set; } = DefaultSolutionName;
    }
}
