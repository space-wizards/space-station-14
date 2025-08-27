using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.KillTome;

/// <summary>
/// Entity with this component is a Kill Tome target.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class KillTomeTargetComponent : Component
{
    ///<summary>
    /// Damage that will be dealt to the target.
    /// </summary>
    [DataField, AutoNetworkedField]
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
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan KillTime = TimeSpan.Zero;

    /// <summary>
    /// Indicates this target has been killed by the killtome.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Dead;

    // Disallows cheat clients from seeing who is about to die to the killtome.
    public override bool SendOnlyToOwner => true;
}
