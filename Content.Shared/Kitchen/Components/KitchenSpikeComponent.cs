using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Kitchen.Components;

/// <summary>
/// Used to mark entity that should act as a spike.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedKitchenSpikeSystem))]
public sealed partial class KitchenSpikeComponent : Component
{
    /// <summary>
    /// Default sound to play when the victim is hooked or unhooked.
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultSpike = new("Spike");

    /// <summary>
    /// Default sound to play when the victim is butchered.
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultSpikeButcher = new("SpikeButcher");

    /// <summary>
    /// ID of the container where the victim will be stored.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ContainerId = "body";

    /// <summary>
    /// Container where the victim will be stored.
    /// </summary>
    [ViewVariables]
    public ContainerSlot BodyContainer = default!;

    /// <summary>
    /// Sound to play when the victim is hooked or unhooked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier SpikeSound = new SoundCollectionSpecifier(DefaultSpike);

    /// <summary>
    /// Sound to play when the victim is butchered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier ButcherSound = new SoundCollectionSpecifier(DefaultSpikeButcher);

    /// <summary>
    /// Damage that will be applied to the victim when they are hooked or unhooked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier SpikeDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Piercing", 10 },
        },
    };

    /// <summary>
    /// Damage that will be applied to the victim when they are butchered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier ButcherDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Slash", 20 },
        },
    };

    /// <summary>
    /// Damage that the victim will receive over time.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier TimeDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Blunt", 1 }, // Mobs are only gibbed from blunt (at least for now).
        },
    };

    /// <summary>
    /// The next time when the damage will be applied to the victim.
    /// </summary>
    [AutoPausedField, AutoNetworkedField]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextDamage;

    /// <summary>
    /// How often the damage should be applied to the victim.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DamageInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Time that it will take to put the victim on the spike.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan HookDelay = TimeSpan.FromSeconds(7);

    /// <summary>
    /// Time that it will take to put the victim off the spike.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UnhookDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Time that it will take to butcher the victim while they are alive.
    /// </summary>
    /// <remarks>
    /// This is summed up with a <see cref="ButcherableComponent"/>'s butcher delay in butcher DoAfter.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public TimeSpan ButcherDelayAlive = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Value by which the butchering delay will be multiplied if the victim is dead.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ButcherModifierDead = 0.5f;
}

[Serializable, NetSerializable]
public enum KitchenSpikeVisuals : byte
{
    Status,
}

[Serializable, NetSerializable]
public enum KitchenSpikeStatus : byte
{
    Empty,
    Bloody, // TODO: Add sprites for different species.
}
