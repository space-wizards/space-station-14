using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Tag;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using System.Threading;

namespace Content.Server.Ninja.Components;

[RegisterComponent]
public sealed class SpaceNinjaGlovesComponent : Component
{
    /// <summary>
    /// The action for emagging doors with ninja gloves
    /// </summary>
    [DataField("doorjackAction")]
    public EntityTargetAction DoorjackAction = new()
    {
          UseDelay = TimeSpan.FromSeconds(1), // can't spam it ridiclously fast
          Icon = new SpriteSpecifier.Rsi(new ResourcePath("Objects/Tools/emag.rsi"), "icon"),
          ItemIconStyle = ItemActionIconStyle.BigAction,
          DisplayName = "action-name-ninja-doorjack",
          Description = "action-desc-ninja-doorjack",
          Priority = -11,
          Event = new NinjaDoorjackEvent()
    };

    /// <summary>
    /// The tag that marks an entity as immune to doorjacking
    /// </summary>
    [DataField("emagImmuneTag", customTypeSerializer: typeof(PrototypeIdSerializer<TagPrototype>))]
    public string EmagImmuneTag = "EmagImmune";

    /// <summary>
    /// The action for stunning people with ninja gloves
    /// </summary>
    [DataField("stunAction")]
    public EntityTargetAction StunAction = new()
    {
          UseDelay = TimeSpan.FromSeconds(1),
          Icon = new SpriteSpecifier.Rsi(new ResourcePath("Objects/Weapons/Melee/stunbaton.rsi"), "stunbaton_on"),
          ItemIconStyle = ItemActionIconStyle.BigAction,
          DisplayName = "action-name-ninja-stun",
          Description = "action-desc-ninja-stun",
          Priority = -12,
          Event = new NinjaStunEvent()
    };

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

    /// <summary>
    /// The action for emagging doors with ninja gloves
    /// </summary>
    [DataField("drainAction")]
    public EntityTargetAction DrainAction = new()
    {
          UseDelay = TimeSpan.FromSeconds(5), // can't visit every apc in rapid succession, gives incentive to drain substations and smeses
          Icon = new SpriteSpecifier.Rsi(new ResourcePath("Structures/Power/apc.rsi"), "apc0"),
          ItemIconStyle = ItemActionIconStyle.BigAction,
          DisplayName = "action-name-ninja-drain",
          Description = "action-desc-ninja-drain",
          Priority = -13,
          Event = new NinjaDrainEvent()
    };

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

    public CancellationTokenSource? DrainCancelToken = null;
}

public sealed class NinjaDoorjackEvent : EntityTargetActionEvent { }

public sealed class NinjaStunEvent : EntityTargetActionEvent { }

public sealed class NinjaDrainEvent : EntityTargetActionEvent { }

public record DrainSuccessEvent(EntityUid User, EntityUid Draining);

public record DrainCancelledEvent;
