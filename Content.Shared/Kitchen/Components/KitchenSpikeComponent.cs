using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Kitchen.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedKitchenSpikeSystem))]
public sealed partial class KitchenSpikeComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultSpike = new("Spike");

    /// <summary>
    ///
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultSpikeButcher = new("SpikeButcher");

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
    public SoundSpecifier SpikeSound = new SoundCollectionSpecifier(DefaultSpike);

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier ButcherSound = new SoundCollectionSpecifier(DefaultSpikeButcher);

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
    public DamageSpecifier UpdateDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Piercing", 0.1 },
        },
    };

    /// <summary>
    ///
    /// </summary>
    [AutoPausedField, AutoNetworkedField]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HookDelay = 7.0f;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public float UnhookDelay = 10.0f;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ButcherDelayAlive = 10.0f;
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
