using Content.Server.Access;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Tools;
using Content.Server.Tools.Components;
using Content.Server.Doors.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using System.Linq;

namespace Content.Server.Doors.Systems;

public sealed class DoorSystem : SharedDoorSystem
{
    [Dependency] private readonly ConstructionSystem _constructionSystem = default!;
    [Dependency] private readonly ToolSystem _toolSystem = default!;
    [Dependency] private readonly AirtightSystem _airtightSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DoorComponent, InteractUsingEvent>(OnInteractUsing, after: new[] { typeof(ConstructionSystem) });

        SubscribeLocalEvent<DoorComponent, PryFinishedEvent>(OnPryFinished);
        SubscribeLocalEvent<DoorComponent, PryCancelledEvent>(OnPryCancelled);
        SubscribeLocalEvent<DoorComponent, WeldFinishedEvent>(OnWeldFinished);
        SubscribeLocalEvent<DoorComponent, WeldCancelledEvent>(OnWeldCancelled);
        SubscribeLocalEvent<DoorComponent, GotEmaggedEvent>(OnEmagged);
    }

    protected override void SetCollidable(EntityUid uid, bool collidable,
        DoorComponent? door = null,
        PhysicsComponent? physics = null,
        OccluderComponent? occluder = null)
    {
        if (!Resolve(uid, ref door))
            return;

        if (door.ChangeAirtight && TryComp(uid, out AirtightComponent? airtight))
            _airtightSystem.SetAirblocked(airtight, collidable);

        // Pathfinding / AI stuff.
        RaiseLocalEvent(new AccessReaderChangeMessage(uid, collidable));

        base.SetCollidable(uid, collidable, door, physics, occluder);
    }

    // TODO AUDIO PREDICT Figure out a better way to handle sound and prediction. For now, this works well enough?
    //
    // Currently a client will predict when a door is going to close automatically. So any client in PVS range can just
    // play their audio locally. Playing it server-side causes an odd delay, while in shared it causes double-audio.
    //
    // But if we just do that, then if a door is closed prematurely as the result of an interaction (i.e., using "E" on
    // an open door), then the audio would only be played for the client performing the interaction.
    //
    // So we do this:
    // - Play audio client-side IF the closing is being predicted (auto-close or predicted interaction)
    // - Server assumes automated closing is predicted by clients and does not play audio unless otherwise specified.
    // - Major exception is player interactions, which other players cannot predict
    // - In that case, send audio to all players, except possibly the interacting player if it was a predicted
    //   interaction.

    /// <summary>
    /// Selectively send sound to clients, taking care to not send the double-audio.
    /// </summary>
    /// <param name="uid">The audio source</param>
    /// <param name="sound">The sound</param>
    /// <param name="predictingPlayer">The user (if any) that instigated an interaction</param>
    /// <param name="predicted">Whether this interaction would have been predicted. If the predicting player is null,
    /// this assumes it would have been predicted by all players in PVS range.</param>
    protected override void PlaySound(EntityUid uid, string sound, AudioParams audioParams, EntityUid? predictingPlayer, bool predicted)
    {
        // If this sound would have been predicted by all clients, do not play any audio.
        if (predicted && predictingPlayer == null)
            return;

        var filter = Filter.Pvs(uid);

        if (predicted)
        {
            // This interaction is predicted, but only by the instigating user, who will have played their own sounds.
            filter.RemoveWhereAttachedEntity(e => e == predictingPlayer);
        }

        // send the sound to players.
        SoundSystem.Play(filter, sound, uid, audioParams);
    }

#region DoAfters
    /// <summary>
    ///     Weld or pry open a door.
    /// </summary>
    private void OnInteractUsing(EntityUid uid, DoorComponent door, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.Used, out ToolComponent? tool))
            return;

        if (tool.Qualities.Contains(door.PryingQuality))
        {
            args.Handled = TryPryDoor(uid, args.Used, args.User, door);
            return;
        }

        if (door.Weldable && tool.Qualities.Contains(door.WeldingQuality))
        {
            args.Handled = TryWeldDoor(uid, args.Used, args.User, door);
        }
    }

    /// <summary>
    ///     Attempt to weld a door shut, or unweld it if it is already welded. This does not actually check if the user
    ///     is holding the correct tool.
    /// </summary>
    private bool TryWeldDoor(EntityUid target, EntityUid used, EntityUid user, DoorComponent door)
    {
        if (!door.Weldable || door.BeingWelded || door.CurrentlyCrushing.Count > 0)
            return false;

        // is the door in a weld-able state?
        if (door.State != DoorState.Closed && door.State != DoorState.Welded)
            return false;

        // perform a do-after delay
        door.BeingWelded = true;
        _toolSystem.UseTool(used, user, target, 3f, 3f, door.WeldingQuality,
                new WeldFinishedEvent(), new WeldCancelledEvent(), target);

        return true; // we might not actually succeeded, but a do-after has started
    }

    /// <summary>
    ///     Pry open a door. This does not check if the user is holding the required tool.
    /// </summary>
    private bool TryPryDoor(EntityUid target, EntityUid tool, EntityUid user, DoorComponent door)
    {
        if (door.State == DoorState.Welded)
            return false;

        var canEv = new BeforeDoorPryEvent(user);
        RaiseLocalEvent(target, canEv, false);

        if (canEv.Cancelled)
            // mark handled, as airlock component will cancel after generating a pop-up & you don't want to pry a tile
            // under a windoor.
            return true;

        var modEv = new DoorGetPryTimeModifierEvent();
        RaiseLocalEvent(target, modEv, false);

        _toolSystem.UseTool(tool, user, target, 0f, modEv.PryTimeModifier * door.PryTime, door.PryingQuality,
                new PryFinishedEvent(), new PryCancelledEvent(), target);

        return true; // we might not actually succeeded, but a do-after has started
    }

    private void OnWeldCancelled(EntityUid uid, DoorComponent door, WeldCancelledEvent args)
    {
        door.BeingWelded = false;
    }

    private void OnWeldFinished(EntityUid uid, DoorComponent door, WeldFinishedEvent args)
    {
        door.BeingWelded = false;

        if (!door.Weldable)
            return;

        if (door.State == DoorState.Closed)
            SetState(uid, DoorState.Welded, door);
        else if (door.State == DoorState.Welded)
            SetState(uid, DoorState.Closed, door);
    }

    private void OnPryCancelled(EntityUid uid, DoorComponent door, PryCancelledEvent args)
    {
        door.BeingPried = false;
    }

    private void OnPryFinished(EntityUid uid, DoorComponent door, PryFinishedEvent args)
    {
        door.BeingPried = false;

        if (door.State == DoorState.Closed)
            StartOpening(uid, door);
        else if (door.State == DoorState.Open)
            StartClosing(uid, door);
    }
#endregion

    /// <summary>
    ///     Does the user have the permissions required to open this door?
    /// </summary>
    public override bool HasAccess(EntityUid uid, EntityUid? user = null, AccessReaderComponent? access = null)
    {
        // TODO network AccessComponent for predicting doors

        // if there is no "user" we skip the access checks. Access is also ignored in some game-modes.
        if (user == null || AccessType == AccessTypes.AllowAll)
            return true;

        // If the door is on emergency access we skip the checks.
        if (TryComp<SharedAirlockComponent>(uid, out var airlock) && airlock.EmergencyAccess)
            return true;

        if (!Resolve(uid, ref access, false))
            return true;

        var isExternal = access.AccessLists.Any(list => list.Contains("External"));

        return AccessType switch
        {
            // Some game modes modify access rules.
            AccessTypes.AllowAllIdExternal => !isExternal || _accessReaderSystem.IsAllowed(access, user.Value),
            AccessTypes.AllowAllNoExternal => !isExternal,
            _ => _accessReaderSystem.IsAllowed(access, user.Value)
        };
    }

    /// <summary>
    ///     Open a door if a player or door-bumper (PDA, ID-card) collide with the door. Sadly, bullets no longer
    ///     generate "access denied" sounds as you fire at a door.
    /// </summary>
    protected override void HandleCollide(EntityUid uid, DoorComponent door, StartCollideEvent args)
    {
        // TODO ACCESS READER move access reader to shared and predict door opening/closing
        // Then this can be moved to the shared system without mispredicting.
        if (!door.BumpOpen)
            return;

        if (door.State != DoorState.Closed)
            return;

        var otherUid = args.OtherFixture.Body.Owner;

        if (_tagSystem.HasTag(otherUid, "DoorBumpOpener"))
            TryOpen(uid, door, otherUid);
    }

    private void OnMapInit(EntityUid uid, DoorComponent door, MapInitEvent args)
    {
        // Ensure that the construction component is aware of the board container.
        if (TryComp(uid, out ConstructionComponent? construction))
            _constructionSystem.AddContainer(uid, "board", construction);

        // We don't do anything if this is null or empty.
        if (string.IsNullOrEmpty(door.BoardPrototype))
            return;

        var container = uid.EnsureContainer<Container>("board", out var existed);

        if (existed & container.ContainedEntities.Count != 0)
        {
            // We already contain a board. Note: We don't check if it's the right one!
            return;
        }

        var board = EntityManager.SpawnEntity(door.BoardPrototype, Transform(uid).Coordinates);

        if(!container.Insert(board))
            Logger.Warning($"Couldn't insert board {ToPrettyString(board)} into door {ToPrettyString(uid)}!");
    }
    private void OnEmagged(EntityUid uid, DoorComponent door, GotEmaggedEvent args)
    {
        if(TryComp<AirlockComponent>(uid, out var airlockComponent))
        {
            if (door.State == DoorState.Closed)
            {
                StartOpening(uid);
                airlockComponent?.SetBoltsWithAudio(!airlockComponent.IsBolted());
                args.Handled = true;
            }
        }
    }
}

public sealed class PryFinishedEvent : EntityEventArgs { }
public sealed class PryCancelledEvent : EntityEventArgs { }
public sealed class WeldFinishedEvent : EntityEventArgs { }
public sealed class WeldCancelledEvent : EntityEventArgs { }
