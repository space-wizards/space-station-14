using Content.Server.Access;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Prying.Components;
using Content.Shared.Prying.Systems;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server.Doors.Systems;

public sealed class DoorSystem : SharedDoorSystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly DoorBoltSystem _bolts = default!;
    [Dependency] private readonly AirtightSystem _airtightSystem = default!;
    [Dependency] private readonly PryingSystem _pryingSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorComponent, WeldableAttemptEvent>(OnWeldAttempt);
        SubscribeLocalEvent<DoorComponent, WeldableChangedEvent>(OnWeldChanged);
        SubscribeLocalEvent<DoorComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<DoorComponent, PriedEvent>(OnAfterPry);
    }

    protected override void OnActivate(EntityUid uid, DoorComponent door, ActivateInWorldEvent args)
    {
        // TODO once access permissions are shared, move this back to shared.
        if (args.Handled || !door.ClickOpen)
            return;

        if (!TryToggleDoor(uid, door, args.User))
            _pryingSystem.TryPry(uid, args.User, out _);

        args.Handled = true;
    }

    protected override void SetCollidable(
        EntityUid uid,
        bool collidable,
        DoorComponent? door = null,
        PhysicsComponent? physics = null,
        OccluderComponent? occluder = null)
    {
        if (!Resolve(uid, ref door))
            return;

        if (door.ChangeAirtight && TryComp(uid, out AirtightComponent? airtight))
            _airtightSystem.SetAirblocked((uid, airtight), collidable);

        // Pathfinding / AI stuff.
        RaiseLocalEvent(new AccessReaderChangeEvent(uid, collidable));

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
    /// <param name="soundSpecifier">The sound</param>
    /// <param name="audioParams">The audio parameters.</param>
    /// <param name="predictingPlayer">The user (if any) that instigated an interaction</param>
    /// <param name="predicted">Whether this interaction would have been predicted. If the predicting player is null,
    /// this assumes it would have been predicted by all players in PVS range.</param>
    protected override void PlaySound(EntityUid uid, SoundSpecifier soundSpecifier, AudioParams audioParams, EntityUid? predictingPlayer, bool predicted)
    {
        // If this sound would have been predicted by all clients, do not play any audio.
        if (predicted && predictingPlayer == null)
            return;

        if (predicted)
            Audio.PlayPredicted(soundSpecifier, uid, predictingPlayer, audioParams);
        else
            Audio.PlayPvs(soundSpecifier, uid, audioParams);
    }

    #region DoAfters
    private void OnWeldAttempt(EntityUid uid, DoorComponent component, WeldableAttemptEvent args)
    {
        if (component.CurrentlyCrushing.Count > 0)
        {
            args.Cancel();
            return;
        }
        if (component.State != DoorState.Closed && component.State != DoorState.Welded)
        {
            args.Cancel();
        }
    }

    private void OnWeldChanged(EntityUid uid, DoorComponent component, ref WeldableChangedEvent args)
    {
        if (component.State == DoorState.Closed)
            SetState(uid, DoorState.Welded, component);
        else if (component.State == DoorState.Welded)
            SetState(uid, DoorState.Closed, component);
    }
    #endregion


    /// <summary>
    ///     Open a door if a player or door-bumper (PDA, ID-card) collide with the door. Sadly, bullets no longer
    ///     generate "access denied" sounds as you fire at a door.
    /// </summary>
    protected override void HandleCollide(EntityUid uid, DoorComponent door, ref StartCollideEvent args)
    {
        // TODO ACCESS READER move access reader to shared and predict door opening/closing
        // Then this can be moved to the shared system without mispredicting.
        if (!door.BumpOpen)
            return;

        if (door.State is not (DoorState.Closed or DoorState.Denying))
            return;

        var otherUid = args.OtherEntity;

        if (Tags.HasTag(otherUid, "DoorBumpOpener"))
            TryOpen(uid, door, otherUid, quiet: door.State == DoorState.Denying);
    }
    private void OnEmagged(EntityUid uid, DoorComponent door, ref GotEmaggedEvent args)
    {
        if (TryComp<AirlockComponent>(uid, out var airlockComponent))
        {
            if (_bolts.IsBolted(uid) || !this.IsPowered(uid, EntityManager))
                return;

            if (door.State == DoorState.Closed)
            {
                SetState(uid, DoorState.Emagging, door);
                PlaySound(uid, door.SparkSound, AudioParams.Default.WithVolume(8), args.UserUid, false);
                args.Handled = true;
            }
        }
    }

    public override void StartOpening(EntityUid uid, DoorComponent? door = null, EntityUid? user = null, bool predicted = false)
    {
        if (!Resolve(uid, ref door))
            return;

        var lastState = door.State;

        SetState(uid, DoorState.Opening, door);

        if (door.OpenSound != null)
            PlaySound(uid, door.OpenSound, AudioParams.Default.WithVolume(-5), user, predicted);

        if (lastState == DoorState.Emagging && TryComp<DoorBoltComponent>(uid, out var doorBoltComponent))
            _bolts.SetBoltsWithAudio(uid, doorBoltComponent, !doorBoltComponent.BoltsDown);
    }

    /// <summary>
    ///     Open or close a door after it has been successfuly pried.
    /// </summary>
    private void OnAfterPry(EntityUid uid, DoorComponent door, ref PriedEvent args)
    {
        if (door.State == DoorState.Closed)
        {
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User)} pried {ToPrettyString(uid)} open");
            StartOpening(uid, door, args.User);
        }
        else if (door.State == DoorState.Open)
        {
            _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(args.User)} pried {ToPrettyString(uid)} closed");
            StartClosing(uid, door, args.User);
        }
    }

    protected override void CheckDoorBump(Entity<DoorComponent, PhysicsComponent> ent)
    {
        var (uid, door, physics) = ent;
        if (door.BumpOpen)
        {
            foreach (var other in PhysicsSystem.GetContactingEntities(uid, physics, approximate: true))
            {
                if (Tags.HasTag(other, "DoorBumpOpener") && TryOpen(uid, door, other, quiet: true))
                    break;
            }
        }
    }
}
