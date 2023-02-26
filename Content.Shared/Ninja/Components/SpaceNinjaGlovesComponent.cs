using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using System.Threading;

namespace Content.Shared.Ninja.Components;

// TODO: split into separate components, including enabled
[RegisterComponent]
public sealed class SpaceNinjaGlovesComponent : Component
{
    /// <summary>
    /// Whether abilities are enabled, can be toggled with the action.
    /// </summary>
    [DataField("enabled")]
    public bool Enabled;

    /// <summary>
    /// The action for toggling ninja gloves abilities
    /// </summary>
    [DataField("toggleAction")]
    public InstantAction ToggleAction = new()
    {
        DisplayName = "action-name-toggle-ninja-gloves",
        Description = "action-desc-toggle-ninja-gloves",
        Priority = -13,
        Event = new ToggleNinjaGlovesEvent()
    };

    // doorjacking

    /// <summary>
    /// The tag that marks an entity as immune to doorjacking
    /// </summary>
    [DataField("emagImmuneTag", customTypeSerializer: typeof(PrototypeIdSerializer<TagPrototype>))]
    public string EmagImmuneTag = "EmagImmune";

    // stunning

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

    // draining

    /// <summary>
    /// Conversion rate between joules in a device and joules added to suit
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

    // downloading

    /// <summary>
    /// Time taken to download research from a server
    /// </summary>
    [DataField("downloadTime")]
    public float DownloadTime = 20f;

    // terror

    /// <summary>
    /// Time taken to call in a threat
    /// </summary>
    [DataField("terrorTime")]
    public float TerrorTime = 20f;
}

public sealed class ToggleNinjaGlovesEvent : InstantActionEvent { }

public record struct PowerDrainData;

public record struct ResearchDownloadData;

public record struct TerrorData;
