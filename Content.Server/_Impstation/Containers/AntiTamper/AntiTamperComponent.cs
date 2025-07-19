using Content.Shared.Damage;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Containers.AntiTamper;

/// <summary>
/// When a locked container with this component is destroyed, it will
/// acidify the contents.
/// </summary>
[RegisterComponent]
[Access(typeof(AntiTamperSystem))]
public sealed partial class AntiTamperComponent : Component
{
    /// <summary>
    /// List of containers to acidify. If null,
    /// all containers will acidify.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public HashSet<string>? Containers;

    /// <summary>
    /// The popup message to display when the anti-tamper module
    /// is triggered.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public LocId Message = "anti-tamper-contents-destroyed";

    /// <summary>
    /// The popup message to display when the anti-tamper module
    /// fails to trigger.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public LocId FailureMessage = "anti-tamper-random-failure";

    /// <summary>
    /// The sound to play when the anti-tamper module is triggered.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/soda_spray.ogg");

    /// <summary>
    /// If true, mobs with a <seealso cref="MindContainerComponent"/> will not be
    /// deleted, and instead will take <seealso cref="MobDamage"/>.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool PreventRoundRemoval = true;

    /// <summary>
    /// If <seealso cref="PreventRoundRemoval"/> is <c>true</c>, mobs caught inside
    /// of the container when the anti-tamper is activated will receive this
    /// damage instead of being deleted.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier MobDamage = new()
    {
        DamageDict = new()
        {
            { "Caustic", 85 }
        },
    };

    /// <summary>
    /// If true, mobs with
    /// <seealso cref="Content.Shared.Interaction.Components.ComplexInteractionComponent">ComplexInteractionComponent</seealso>
    /// will be able to disarm the anti-tamper component the crate is open or they are inside of it.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool CanDisarm = true;

    /// <summary>
    /// The length of time it takes to disarm the anti-tamper module. Multiplied by
    /// <seealso cref="DisarmLockedMultiplier"/> if the disarming mob is locked
    /// inside of the container.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DisarmTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// If the disarming mob is locked inside of the container,
    /// the <seealso cref="DisarmTime"/> will be multiplied by this.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DisarmLockedMultiplier = 4;

    /// <summary>
    /// The tool required to disarm the anti-tamper module. If null,
    /// no tool is required.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ToolQualityPrototype>? DisarmToolRequired = "Screwing";
}
