using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.DeepFryer.Components;

/// <summary>
/// Allows an entity storage to deep fry stored entities.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class DeepFryerComponent : Component
{
    /// <summary>
    /// The uninterrupted time it takes to deep fry non-mob entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CookTime = TimeSpan.FromSeconds(7);

    /// <summary>
    /// The least uninterrupted time it takes to deep fry mob entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MobCookTime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How much heat is added per second to the vat solution. Should start at zero so fryer isn't automatically "on" when map loads
    /// </summary>
    [DataField]
    public float HeatPerSecond = 0.0f;

    /// <summary>
    /// Maximum heat the vat solution can reach.
    /// </summary>
    [DataField]
    public float MaxHeat = 450.0f;

    /// <summary>
    /// Minimum heat the vat solution can reach. By default, room temperature.
    /// </summary>
    [DataField]
    public float MinHeat = 293.15f;

    /// <summary>
    /// How much HeatPerSecond changes each second. Additive if deep fryer is powered, and subtractive if it is not.
    /// </summary>
    [DataField]
    public float ChangeHeatPerSecond = 100.0f;

    /// <summary>
    /// Maximum HeatPerSecond value.
    /// </summary>
    [DataField]
    public float MaxHeatChange = 2000.0f;

    /// <summary>
    /// Minimum HeatPerSecond value.
    /// </summary>
    [DataField]
    public float MinHeatChange = -1000.0f;

    /// <summary>
    /// Heat threshold at which frying is allowed to occur
    /// </summary>
    [DataField]
    public float HeatThreshold = 375.0f;

    /// <summary>
    /// The timestamp at which deep frying is finished.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan ActiveUntil = TimeSpan.Zero;

    [DataField]
    public SoundSpecifier DeepFryStartSound = new SoundPathSpecifier("/Audio/Items/Lighters/lighter1.ogg");

    [DataField]
    public SoundSpecifier DeepFrySound = new SoundPathSpecifier("/Audio/Effects/burning.ogg");
}
