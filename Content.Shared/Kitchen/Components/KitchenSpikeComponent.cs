using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, Access(typeof(SharedKitchenSpikeSystem))]
public sealed partial class KitchenSpikeComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultSpike = new("Spike");

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ContainerId = "body";

    /// <summary>
    ///
    /// </summary>
    [ViewVariables]
    public ContainerSlot BodyContainer;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier(DefaultSpike);

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier SpikeDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Piercing", 20 },
        },
    };

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier ButcherDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Slash", 30 },
        },
    };

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier DamageOverTime = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Bloodloss", 4 },
        },
    };

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Delay = 7.0f;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DelayAlive = 10.0f;
}

/// <summary>
///
/// </summary>
[Serializable, NetSerializable]
public enum KitchenSpikeVisuals : byte
{
    Status,
}

/// <summary>
///
/// </summary>
[Serializable, NetSerializable]
public enum KitchenSpikeStatus : byte
{
    Empty,
    Bloody,
}
