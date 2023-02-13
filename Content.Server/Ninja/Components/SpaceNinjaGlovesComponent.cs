using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Tag;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

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
}

public sealed class NinjaDoorjackEvent : EntityTargetActionEvent { }

public sealed class NinjaStunEvent : EntityTargetActionEvent { }
