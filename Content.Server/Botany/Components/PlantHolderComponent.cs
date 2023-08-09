namespace Content.Server.Botany.Components
{
    [RegisterComponent]
    public sealed class PlantHolderComponent : Component
    {
        [ViewVariables]
        public TimeSpan NextUpdate = TimeSpan.Zero;
        public TimeSpan UpdateDelay = TimeSpan.FromSeconds(3);

        [ViewVariables]
        public int LastProduce;

        [ViewVariables(VVAccess.ReadWrite)]
        public int MissingGas;

        public readonly TimeSpan CycleDelay = TimeSpan.FromSeconds(15f);

        [ViewVariables]
        public TimeSpan LastCycle = TimeSpan.Zero;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool UpdateSpriteAfterUpdate;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("drawWarnings")]
        public bool DrawWarnings = false;

        [ViewVariables(VVAccess.ReadWrite)]
        public float WaterLevel = 100f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float NutritionLevel = 100f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float PestLevel { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float WeedLevel { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float Toxins { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public int Age { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public int SkipAging { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Dead { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Harvest { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Sampled { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public int YieldMod { get; set; } = 1;

        [ViewVariables(VVAccess.ReadWrite)]
        public float MutationMod { get; set; } = 1f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float MutationLevel { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float Health { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float WeedCoefficient { get; set; } = 1f;

        [ViewVariables(VVAccess.ReadWrite)]
        public SeedData? Seed { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ImproperHeat { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ImproperPressure { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ImproperLight { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ForceUpdate { get; set; }

        [DataField("solution")]
        public string SoilSolutionName { get; set; } = "soil";
    }
}
