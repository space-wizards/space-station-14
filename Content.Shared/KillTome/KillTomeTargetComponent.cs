using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.KillTome;

/// <summary>
/// Entity with this component is a Kill Tome target.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class KillTomeTargetComponent : Component
{
    ///<summary>
    /// Damage that will be dealt to the target.
    /// </summary>
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Blunt", 200 }
        }
    };

    /// <summary>
    /// The time when the target is killed.
    /// </summary>
    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan KillTime;
}
