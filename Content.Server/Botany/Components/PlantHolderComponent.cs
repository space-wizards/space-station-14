using Content.Shared.Chemistry.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Audio;

namespace Content.Server.Botany.Components;

[RegisterComponent]
public sealed partial class PlantHolderComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public int MissingGas;

    [DataField]
    public TimeSpan CycleDelay = TimeSpan.FromSeconds(15f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastCycle = TimeSpan.Zero;

    [DataField]
    public SoundSpecifier? WateringSound;

    [DataField]
    public bool UpdateSpriteAfterUpdate;

    [DataField]
    public bool DrawWarnings = false;

    [DataField]
    public float WaterLevel = 100f;

    [DataField]
    public float NutritionLevel = 100f;

    [DataField]
    public float PestLevel;

    [DataField]
    public float WeedLevel;

    [DataField]
    public float Toxins;

    [DataField]
    public float WeedCoefficient = 1f;

    [DataField]
    public bool ImproperHeat;

    [DataField]
    public bool ImproperPressure;

    [DataField]
    public bool ForceUpdate;

    [DataField]
    public string SoilSolutionName = "soil";

    [DataField]
    public Entity<SolutionComponent>? SoilSolution = null;

    [DataField]
    public EntityUid? PlantUid;
}
