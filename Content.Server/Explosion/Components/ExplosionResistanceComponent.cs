using Content.Server.Explosion.EntitySystems;
using Content.Shared.Explosion;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Explosion.Components;

/// <summary>
///     Component that provides entities with explosion resistance.
/// </summary>
/// <remarks>
///     This is desirable over just using damage modifier sets, given that equipment like bomb-suits need to
///     significantly reduce the damage, but shouldn't be silly overpowered in regular combat.
/// </remarks>
[RegisterComponent]
[Access(typeof(ExplosionResistanceSystem))]
public sealed class ExplosionResistanceComponent : Component
{
    /// <summary>
    ///     The explosive resistance coefficient, This fraction is multiplied into the total resistance.
    /// </summary>
    [DataField("damageCoefficient")]
    public float DamageCoefficient = 1;

    /// <summary>
    ///     Like <see cref="GlobalResistance"/>, but specified specific to each explosion type for more customizability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("resistances", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<float, ExplosionPrototype>))]
    public Dictionary<string, float> Resistances = new();

    /// <summary>
    ///     The examine group used for grouping together examine details.
    /// </summary>
    [DataField("examineGroup")] public string ExamineGroup = "worn-stats";

    [DataField("examinePriority")] public int ExaminePriority = 6;
}
