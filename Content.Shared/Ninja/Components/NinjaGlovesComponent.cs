using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.DoAfter;
using Content.Shared.Ninja.Systems;
using Content.Shared.Tag;
using Content.Shared.Toggleable;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using System.Threading;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component for toggling glove powers.
/// Powers being enabled is controlled by GlovesEnabledComponent
/// </summary>
[Access(typeof(SharedNinjaGlovesSystem))]
[RegisterComponent, NetworkedComponent]
public sealed class NinjaGlovesComponent : Component
{
    /// <summary>
    /// Entity of the ninja using these gloves, usually means enabled
    /// </summary>
    [ViewVariables]
    public EntityUid? User;

    /// <summary>
    /// The action for toggling ninja gloves abilities
    /// </summary>
    [DataField("toggleAction")]
    public InstantAction ToggleAction = new()
    {
        DisplayName = "action-name-toggle-ninja-gloves",
        Description = "action-desc-toggle-ninja-gloves",
        Priority = -13,
        Event = new ToggleActionEvent()
    };
}

/// <summary>
/// Component for emagging doors on click, when gloves are enabled.
/// Only works on entities with DoorComponent.
/// </summary>
[RegisterComponent]
public sealed class NinjaDoorjackComponent : Component
{
    /// <summary>
    /// The tag that marks an entity as immune to doorjacking
    /// </summary>
    [DataField("emagImmuneTag", customTypeSerializer: typeof(PrototypeIdSerializer<TagPrototype>))]
    public string EmagImmuneTag = "EmagImmune";
}

/// <summary>
/// Component for stunning mobs on click, when gloves are enabled.
/// Knocks them down for a bit and deals shock damage.
/// </summary>
[RegisterComponent]
public sealed class NinjaStunComponent : Component
{
    /// <summary>
    /// Joules required in the suit to stun someone. Defaults to 10 uses on a small battery.
    /// </summary>
    [DataField("stunCharge")]
    public float StunCharge = 36.0f;

    /// <summary>
    /// Shock damage dealt when stunning someone
    /// </summary>
    [DataField("stunDamage")]
    public int StunDamage = 5;

    /// <summary>
    /// Time that someone is stunned for, stacks if done multiple times.
    /// </summary>
    [DataField("stunTime")]
    public TimeSpan StunTime = TimeSpan.FromSeconds(3);
}

/// <summary>
/// Component for draining power from APCs/substations/SMESes, when gloves are enabled.
/// </summary>
[RegisterComponent]
public sealed class NinjaDrainComponent : Component
{
    /// <summary>
    /// Conversion rate between joules in a device and joules added to suit.
    /// Should be very low since powercells store nothing compared to even an APC.
    /// </summary>
    [DataField("drainEfficiency")]
    public float DrainEfficiency = 0.001f;

    /// <summary>
    /// Time that the do after takes to drain charge from a battery, in seconds
    /// </summary>
    [DataField("drainTime")]
    public float DrainTime = 1f;

    [DataField("sparkSound")]
    public SoundSpecifier SparkSound = new SoundCollectionSpecifier("sparks");
}

/// <summary>
/// Component for downloading research nodes from a R&D server, when gloves are enabled.
/// Requirement for greentext.
/// </summary>
[RegisterComponent]
public sealed class NinjaDownloadComponent : Component
{
    /// <summary>
    /// Time taken to download research from a server
    /// </summary>
    [DataField("downloadTime")]
    public float DownloadTime = 20f;
}


/// <summary>
/// Component for hacking a communications console to call in a threat.
/// Called threat is rolled from the ninja gamerule config.
/// </summary>
[RegisterComponent]
public sealed class NinjaTerrorComponent : Component
{
    /// <summary>
    /// Time taken to hack the console
    /// </summary>
    [DataField("terrorTime")]
    public float TerrorTime = 20f;
}

/// <summary>
/// DoAfter event for drain ability.
/// </summary>
[Serializable, NetSerializable]
public sealed class DrainDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
/// DoAfter event for research download ability.
/// </summary>
[Serializable, NetSerializable]
public sealed class DownloadDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
/// DoAfter event for comms console terror ability.
/// </summary>
[Serializable, NetSerializable]
public sealed class TerrorDoAfterEvent : SimpleDoAfterEvent { }
