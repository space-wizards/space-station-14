using Content.Shared.Mind;
using Content.Shared.PDA.Ringer;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Store;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.PDA;

/// <summary>
/// Handles the shared functionality for PDA ringtones.
/// </summary>
public abstract class SharedRingerSystem : EntitySystem
{
    public const int RingtoneLength = 6;
    public const int NoteTempo = 300;
    public const float NoteDelay = 60f / NoteTempo;

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPdaSystem _pda = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        // RingerBoundUserInterface Subscriptions
        SubscribeLocalEvent<RingerComponent, RingerSetRingtoneMessage>(OnSetRingtone);
        SubscribeLocalEvent<RingerComponent, RingerPlayRingtoneMessage>(OnPlayRingtone);
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        var ringerQuery = EntityQueryEnumerator<RingerComponent>();
        while (ringerQuery.MoveNext(out var uid, out var ringer))
        {
            if (!ringer.Active || !ringer.NextNoteTime.HasValue)
                continue;

            var curTime = _timing.CurTime;

            // Check if it's time to play the next note
            if (curTime < ringer.NextNoteTime.Value)
                continue;

            // Play the note
            // We only do this on the server because otherwise the sound either dupes or blends into a mess
            // There's no easy way to figure out which player started it, so that we can exclude them from the list
            // and play it separately with PlayLocal, so that it's actually predicted
            if (_net.IsServer)
            {
                var ringerXform = Transform(uid);
                _audio.PlayEntity(
                    GetSound(ringer.Ringtone[ringer.NoteCount]),
                    Filter.Empty().AddInRange(_xform.GetMapCoordinates(uid, ringerXform), ringer.Range),
                    uid,
                    true,
                    AudioParams.Default.WithMaxDistance(ringer.Range).WithVolume(ringer.Volume)
                );
            }

            // Schedule next note
            ringer.NextNoteTime = curTime + TimeSpan.FromSeconds(NoteDelay);
            ringer.NoteCount++;

            // Dirty the fields we just changed
            DirtyFields(uid,
                ringer,
                null,
                nameof(RingerComponent.NextNoteTime),
                nameof(RingerComponent.NoteCount));

            // Check if we've finished playing all notes
            if (ringer.NoteCount >= RingtoneLength)
            {
                ringer.Active = false;
                ringer.NextNoteTime = null;
                ringer.NoteCount = 0;

                DirtyFields(uid,
                    ringer,
                    null,
                    nameof(RingerComponent.Active),
                    nameof(RingerComponent.NextNoteTime),
                    nameof(RingerComponent.NoteCount));

                UpdateRingerUi((uid, ringer));
            }
        }
    }

    #region Public API

    /// <summary>
    /// Plays the ringtone on the device with the given RingerComponent.
    /// </summary>
    public void RingerPlayRingtone(Entity<RingerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        StartRingtone((ent, ent.Comp));
    }

    /// <summary>
    /// Toggles the ringer UI for the given entity.
    /// </summary>
    /// <param name="uid">The entity containing the ringer UI.</param>
    /// <param name="actor">The entity that's interacting with the UI.</param>
    /// <returns>True if the UI toggle was successful.</returns>
    public bool TryToggleRingerUi(EntityUid uid, EntityUid actor)
    {
        UI.TryToggleUi(uid, RingerUiKey.Key, actor);
        return true;
    }

    /// <summary>
    /// Locks the uplink and closes the window, if its open.
    /// </summary>
    /// <remarks>
    /// Will not update the PDA ui so you must do that yourself if needed.
    /// </remarks>
    public void LockUplink(Entity<RingerUplinkComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Unlocked = false;
        UI.CloseUi(ent.Owner, StoreUiKey.Key);
    }

    /// <summary>
    /// Attempts to unlock or lock the uplink by checking the provided ringtone against the uplink code.
    /// On the client side, it does nothing since the client cannot know the code in advance.
    /// On the server side, the code is verified.
    /// </summary>
    /// <param name="uid">The entity with the RingerUplinkComponent.</param>
    /// <param name="ringtone">The ringtone to check against the uplink code.</param>
    /// <param name="user">The entity attempting to toggle the uplink.</param>
    /// <returns>True if the uplink state was toggled, false otherwise.</returns>
    [PublicAPI]
    public virtual bool TryToggleUplink(EntityUid uid, Note[] ringtone, EntityUid? user = null)
    {
        return false;
    }

    #endregion

    // UI Message event handlers

    /// <summary>
    /// Handles the <see cref="RingerSetRingtoneMessage"/> from the client UI.
    /// </summary>
    private void OnSetRingtone(Entity<RingerComponent> ent, ref RingerSetRingtoneMessage args)
    {
        // Prevent ringtone spam by checking the last time this ringtone was set
        var curTime = _timing.CurTime;
        if (ent.Comp.NextRingtoneSetTime > curTime)
            return;

        ent.Comp.NextRingtoneSetTime = curTime + ent.Comp.Cooldown;
        DirtyField(ent.AsNullable(), nameof(RingerComponent.NextRingtoneSetTime));

        // Client sent us an updated ringtone so set it to that.
        if (args.Ringtone.Length != RingtoneLength)
            return;

        // Try to toggle the uplink first
        if (TryToggleUplink(ent, args.Ringtone))
            return; // Don't save the uplink code as the ringtone

        UpdateRingerRingtone(ent, args.Ringtone);
    }

    /// <summary>
    /// Handles the <see cref="RingerPlayRingtoneMessage"/> from the client UI.
    /// </summary>
    private void OnPlayRingtone(Entity<RingerComponent> ent, ref RingerPlayRingtoneMessage args)
    {
        StartRingtone(ent);
    }

    // Helper methods

    /// <summary>
    /// Starts playing the ringtone on the device.
    /// </summary>
    private void StartRingtone(Entity<RingerComponent> ent)
    {
        // Already active? Don't start it again
        if (ent.Comp.Active)
            return;

        ent.Comp.Active = true;
        ent.Comp.NoteCount = 0;
        ent.Comp.NextNoteTime = _timing.CurTime;

        UpdateRingerUi(ent);

        _popup.PopupPredicted(Loc.GetString("comp-ringer-vibration-popup"),
            ent,
            ent.Owner,
            Filter.Pvs(ent, 0.05f),
            false,
            PopupType.Medium);

        DirtyFields(ent.AsNullable(),
            null,
            nameof(RingerComponent.NextNoteTime),
            nameof(RingerComponent.Active),
            nameof(RingerComponent.NoteCount));
    }

    /// <summary>
    /// Updates the ringer's ringtone and notifies clients.
    /// </summary>
    /// <param name="ent">Entity with RingerComponent to update.</param>
    /// <param name="ringtone">The new ringtone to set.</param>
    protected void UpdateRingerRingtone(Entity<RingerComponent> ent, Note[] ringtone)
    {
        // Assume validation has already happened.
        ent.Comp.Ringtone = ringtone;
        DirtyField(ent.AsNullable(), nameof(RingerComponent.Ringtone));
        UpdateRingerUi(ent);
    }

    /// <summary>
    /// Base implementation for toggle uplink processing after verification.
    /// </summary>
    protected bool ToggleUplinkInternal(Entity<RingerUplinkComponent> ent)
    {
        // Toggle the unlock state
        ent.Comp.Unlocked = !ent.Comp.Unlocked;

        // Update PDA UI if needed
        if (TryComp<PdaComponent>(ent, out var pda))
            _pda.UpdatePdaUi(ent, pda);

        // Close store UI if we're locking
        if (!ent.Comp.Unlocked)
            UI.CloseUi(ent.Owner, StoreUiKey.Key);

        return true;
    }

    /// <summary>
    /// Helper method to determine if the mind is an antagonist.
    /// </summary>
    protected bool IsAntagonist(EntityUid? user)
    {
        return user != null && _mind.TryGetMind(user.Value, out var mindId, out _) && _role.MindIsAntagonist(mindId);
    }

    /// <summary>
    /// Gets the sound path for a specific note.
    /// </summary>
    /// <param name="note">The note to get the sound for.</param>
    /// <returns>A SoundPathSpecifier pointing to the sound file for the note.</returns>
    private static SoundPathSpecifier GetSound(Note note)
    {
        return new SoundPathSpecifier($"/Audio/Effects/RingtoneNotes/{note.ToString().ToLower()}.ogg");
    }

    /// <summary>
    /// Updates the RingerBoundUserInterface.
    /// </summary>
    protected virtual void UpdateRingerUi(Entity<RingerComponent> ent)
    {
    }
}

/// <summary>
/// Enum representing musical notes for ringtones.
/// </summary>
[Serializable, NetSerializable]
public enum Note : byte
{
    A,
    Asharp,
    B,
    C,
    Csharp,
    D,
    Dsharp,
    E,
    F,
    Fsharp,
    G,
    Gsharp
}
