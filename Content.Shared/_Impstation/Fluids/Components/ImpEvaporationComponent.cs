using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Impstation.ImpEvaporation;

/// <summary>
/// A (hacky) way of applying (a type of) evaporation to reagents in YML.
/// Must be given to the reagent by EnsureTileReaction, and must have `impEvaporates: true` in its reagentPrototype.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ImpEvaporationSystem))]
public sealed partial class ImpEvaporationComponent : Component
{
    /// <summary>
    /// The next time we remove the EvaporationSystem reagent amount from this entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("nextTick", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick = TimeSpan.Zero;

    /// <summary>
    /// The cooldown between removing reagents.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("evaporationCooldown")]
    public TimeSpan EvaporationCooldown = TimeSpan.FromSeconds(10);


    [DataField(required: true)]
    public string Solution = string.Empty;
}