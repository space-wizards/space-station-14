namespace Content.Server.Weapons.Ranged.Components
{
    [RegisterComponent]
    public sealed partial class ChemicalAmmoComponent : Component
    {
        public const string DefaultSolutionName = "ammo";

        [DataField("solution")]
        public string SolutionName { get; set; } = DefaultSolutionName;
    }
}
