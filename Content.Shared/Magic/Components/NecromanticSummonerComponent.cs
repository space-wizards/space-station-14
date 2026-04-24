using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Magic.Components;

/// <summary>
/// Allows an item to turn a dead player into a summoned mob.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NecromanticSummonerComponent : Component
{
    /// <summary>
    /// The entity prototype to spawn and transfer the dead target player to.
    /// Why do moths have a human skeleton inside them you ask? It's a magic skeleton of course!
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId Prototype = "MobSkeletonSummon";

    /// <summary>
    /// The whitelist for which mobs are allowed to be turned into thralls.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// The blacklist for which mobs are allowed to be turned into thralls.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// The DoAfter time when the item is being used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DoAfterTime = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The sound to play when the summon is successful.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SummonSound;

    /// <summary>
    /// The popup to show to the user when starting the DoAfter.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId DoAfterPopup = "necromantic-summoner-doafter";

    /// <summary>
    /// The popup to show to the user when the summon fails due to the item having no charges left.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId NoChargesPopup = "necromantic-summoner-empty";

    /// <summary>
    /// The popup to show to the user when the summon fails due to the target not being dead.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId NotDeadPopup = "necromantic-summoner-not-dead";

    /// <summary>
    /// The popup to show to the user when the summon fails due to the target not having a player attached.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId NoSoulPopup = "necromantic-summoner-no-soul";

    /// <summary>
    /// The popup to show to the user when the summon is successful.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId SummonUserPopup = "necromantic-summoner-summon-user";

    /// <summary>
    /// The popup to show to the target when the summon is successful.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId SummonTargetPopup = "necromantic-summoner-summon-target";

    /// <summary>
    /// The popup to show to others when the summon is successful.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId SummonOthersPopup = "necromantic-summoner-summon-others";

    /// <summary>
    /// Gib the old body on successful summon?
    /// </summary>
    /// <remarks>
    /// Once we have surgery consider actually removing their skeleton from their body instead of spawning one.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public bool GibBody = true;
}
