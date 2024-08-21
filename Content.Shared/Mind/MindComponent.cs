using Content.Shared.Actions;
using Content.Shared.GameTicking;
using Content.Shared.Mind.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.Mind;

/// <summary>
///     This component stores information about a player/mob mind. The component will be attached to a mind-entity
///     which is stored in null-space. The entity that is currently "possessed" by the mind will have a
///     <see cref="MindContainerComponent"/>.
/// </summary>
/// <remarks>
///     Roles are attached as components on the mind-entity entity.
///     Think of it like this: if a player is supposed to have their memories,
///     their mind follows along.
///
///     Things such as respawning do not follow, because you're a new character.
///     Getting borged, cloned, turned into a catbeast, etc... will keep it following you.
///
///     Minds are stored in null-space, and are thus generally not set to players unless that player is the owner
///     of the mind. As a result it should be safe to network "secret" information like roles & objectives
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class MindComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> Objectives = new();

    /// <summary>
    ///     The session ID of the player owning this mind.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(SharedMindSystem))]
    public NetUserId? UserId { get; set; }

    /// <summary>
    ///     The session ID of the original owner, if any.
    ///     May end up used for round-end information (as the owner may have abandoned Mind since)
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(SharedMindSystem))]
    public NetUserId? OriginalOwnerUserId { get; set; }

    /// <summary>
    ///     The first entity that this mind controlled. Used for round end information.
    ///     Might be relevant if the player has ghosted since.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? OriginalOwnedEntity;
    // This is a net entity, because this field currently ddoes not get set to null when this entity is deleted.
    // This is a lazy way to ensure that people check that the entity still exists.
    // TODO MIND Fix this properly by adding an OriginalMindContainerComponent or something like that.

    [ViewVariables]
    public bool IsVisitingEntity => VisitingEntity != null;

    [DataField, AutoNetworkedField, Access(typeof(SharedMindSystem))]
    public EntityUid? VisitingEntity { get; set; }

    [ViewVariables]
    public EntityUid? CurrentEntity => VisitingEntity ?? OwnedEntity;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public string? CharacterName { get; set; }

    /// <summary>
    ///     The time of death for this Mind.
    ///     Can be null - will be null if the Mind is not considered "dead".
    /// </summary>
    [DataField]
    public TimeSpan? TimeOfDeath { get; set; }

    /// <summary>
    ///     The entity currently owned by this mind.
    ///     Can be null.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(SharedMindSystem))]
    public EntityUid? OwnedEntity { get; set; }

    /// <summary>
    ///     An enumerable over all the objective entities this mind has.
    /// </summary>
    [ViewVariables, Obsolete("Use Objectives field")]
    public IEnumerable<EntityUid> AllObjectives => Objectives;

    /// <summary>
    ///     Prevents user from ghosting out
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("preventGhosting")]
    public bool PreventGhosting { get; set; }

    /// <summary>
    ///     Prevents user from suiciding
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("preventSuicide")]
    public bool PreventSuicide { get; set; }

    /// <summary>
    ///     The session of the player owning this mind.
    ///     Can be null, in which case the player is currently not logged in.
    /// </summary>
    [ViewVariables, Access(typeof(SharedMindSystem), typeof(SharedGameTicker))]
    // TODO remove this after moving IPlayerManager functions to shared
    public ICommonSession? Session { get; set; }
}
