using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Tag;
using Content.Shared.TapeRecorder.Components;
using Content.Shared.TapeRecorder.Events;
using Content.Shared.Timing;
using Content.Shared.Toggleable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using System.Linq;
using System.Text;

namespace Content.Shared.TapeRecorder;

public abstract class SharedTapeRecorderSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] protected readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] protected readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] protected readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] protected readonly IRobustRandom _robustRandom = default!;
    [Dependency] protected readonly TagSystem _tagSystem = default!;
    [Dependency] protected readonly UseDelaySystem _useDelay = default!;

    public readonly string SlotName = "cassette_tape";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TapeRecorderComponent, EntRemovedFromContainerMessage>(OnCassetteRemoved);
        SubscribeLocalEvent<TapeRecorderComponent, EntInsertedIntoContainerMessage>(OnCassetteInserted);
        SubscribeLocalEvent<TapeRecorderComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<TapeRecorderComponent, ExaminedEvent>(OnRecorderExamined);

        SubscribeLocalEvent<TapeCassetteComponent, ExaminedEvent>(OnTapeExamined);
        SubscribeLocalEvent<TapeCassetteComponent, DamageChangedEvent>(OnDamagedChanged);
        SubscribeLocalEvent<TapeCassetteComponent, InteractUsingEvent>(OnInteractingWithCassette);
        SubscribeLocalEvent<TapeCassetteComponent, TapeCassetteRepairDoAfterEvent>(OnTapeCassetteRepair);
    }

    /// <summary>
    /// Enumerate all playing, recording and rewinding tape recorders and processed them
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        //Get all recording tape recorders, increment the cassette position
        var recorderQuery = EntityQueryEnumerator<RecordingTapeRecorderComponent, TapeRecorderComponent>();
        while (recorderQuery.MoveNext(out var uid, out var component, out var tapeRecorderComponent))
        {
            if (!ProcessRecordingTapeRecorder(uid, tapeRecorderComponent, frameTime))
                StopRecording(uid, tapeRecorderComponent);
        }

        //Get all playing tape recorders, increment cassette position and play any messages from the interval
        var playerQuery = EntityQueryEnumerator<PlayingTapeRecorderComponent, TapeRecorderComponent>();
        while (playerQuery.MoveNext(out var uid, out var component, out var tapeRecorderComponent))
        {
            if (!ProcessPlayingTapeRecorder(uid, tapeRecorderComponent, frameTime))
                StopPlayback(uid, tapeRecorderComponent);
        }

        //Get all rewinding tape recorders
        var rewindingQuery = EntityQueryEnumerator<RewindingTapeRecorderComponent, TapeRecorderComponent>();
        while (rewindingQuery.MoveNext(out var uid, out var component, out var tapeRecorderComponent))
        {
            if (!ProcessRewindingTapeRecorder(uid, tapeRecorderComponent, frameTime))
                StopRewinding(uid, tapeRecorderComponent);
        }
    }

    protected bool ProcessRecordingTapeRecorder(EntityUid uid, TapeRecorderComponent tapeRecorderComponent, float frameTime)
    {
        //Check if this recorder has a tape inserted
        //It shouldnt be possible to trigger this check, but i have seen stranger things
        var cassette = _itemSlotsSystem.GetItemOrNull(uid, SlotName);
        if (cassette == null)
            return false;

        //Sanity check, this is a tape yea?
        if (!TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
            return false;

        var currentTime = tapeCassetteComponent.CurrentPosition + frameTime;

        //'Flushed' in this context is a mark indicating the message was not added between the last update and this update
        //Remove any flushed messages in the segment we just recorded over (ie old messages)
        tapeCassetteComponent.RecordedData.RemoveAll(x => x.Timestamp > tapeCassetteComponent.CurrentPosition && x.Timestamp <= currentTime && x.Flushed);

        //Mark all messages as flushed so they can be overwritten next time
        tapeCassetteComponent.RecordedData.ForEach(x => x.Flushed = true);

        //Update the tape's current time
        tapeCassetteComponent.CurrentPosition = currentTime;

        //If we have reached the end of the tape - stop
        if (tapeCassetteComponent.CurrentPosition >= tapeCassetteComponent.MaxCapacity.TotalSeconds)
            return false;

        return true;
    }

    protected bool ProcessPlayingTapeRecorder(EntityUid uid, TapeRecorderComponent tapeRecorderComponent, float frameTime)
    {
        //Check if this recorder has a tape inserted
        //It shouldnt be possible to trigger this check, but i have seen stranger things
        var cassette = _itemSlotsSystem.GetItemOrNull(uid, SlotName);
        if (cassette == null)
            return false;

        //Sanity check, this is a tape yea?
        if (!TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
            return false;

        //Get the segment of the tape to be played
        //And any messages within that time period
        var currentTime = tapeCassetteComponent.CurrentPosition + frameTime;
        ReplayMessagesInSegment(uid, tapeRecorderComponent, tapeCassetteComponent, tapeCassetteComponent.CurrentPosition, currentTime);

        //Update the tape's position
        tapeCassetteComponent.CurrentPosition = currentTime;

        //Stop when we reach the end of the tape
        if (tapeCassetteComponent.CurrentPosition >= tapeCassetteComponent.MaxCapacity.TotalSeconds)
            return false;

        return true;
    }

    protected bool ProcessRewindingTapeRecorder(EntityUid uid, TapeRecorderComponent tapeRecorderComponent, float frameTime)
    {
        //Check if this recorder has a tape inserted
        //It shouldnt be possible to trigger this check, but i have seen stranger things
        var cassette = _itemSlotsSystem.GetItemOrNull(uid, SlotName);
        if (cassette == null)
            return false;

        //Sanity check, this is a tape yea?
        if (!TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
            return false;

        //Calculate how far we have rewound
        var rewindTime = frameTime * tapeRecorderComponent.RewindSpeed;
        //Update the current time, clamp to 0
        var currentTime = Math.Max(0, tapeCassetteComponent.CurrentPosition - rewindTime);
        tapeCassetteComponent.CurrentPosition = currentTime;

        //If we have reached the beginning of the tape, stop
        if (tapeCassetteComponent.CurrentPosition <= float.Epsilon)
            return false;

        return true;
    }

    /// <summary>
    /// Stub this as shared doesnt have access to ChatSystem
    /// </summary>
    protected virtual void ReplayMessagesInSegment(EntityUid uid, TapeRecorderComponent tapeRecorderComponent, TapeCassetteComponent tapeCassetteComponent, float segmentStart, float segmentEnd)
    {

    }

    protected void OnInteractingWithCassette(EntityUid uid, TapeCassetteComponent component, InteractUsingEvent args)
    {
        //Is the tape damaged?
        if (HasComp<FitsInTapeRecorderComponent>(uid))
            return;

        //Are we using a pen or screwdriver?
        if (!_tagSystem.HasAnyTag(args.Used, "Screwdriver", "Write"))
            return;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.RepairDelay, new TapeCassetteRepairDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = true
        });
    }

    protected void OnTapeCassetteRepair(EntityUid uid, TapeCassetteComponent component, TapeCassetteRepairDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        //Already repaired?
        if (HasComp<FitsInTapeRecorderComponent>(uid))
            return;

        _appearanceSystem.SetData(uid, ToggleVisuals.Toggled, false);
        AddComp<FitsInTapeRecorderComponent>(uid);
        args.Handled = true;
    }

    /// <summary>
    /// When the cassette has been damaged, corrupt and entry and unspool it
    /// </summary>
    protected void OnDamagedChanged(EntityUid uid, TapeCassetteComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta == null || args.DamageDelta.GetTotal() < 5)
            return;

        _appearanceSystem.SetData(uid, ToggleVisuals.Toggled, true);

        RemComp<FitsInTapeRecorderComponent>(uid);
        CorruptRandomEntry(component);
    }

    /// <summary>
    /// When used in hand, activate/deactivate the current mode
    /// </summary>
    protected void OnUseInHand(EntityUid uid, TapeRecorderComponent component, UseInHandEvent args)
    {
        if (args.Handled || _useDelay.ActiveDelay(uid))
            return;

        switch (component.Mode)
        {
            case TapeRecorderMode.Recording:
                ToggleRecording(uid, component, args.User);
                break;

            case TapeRecorderMode.Playing:
                TogglePlayback(uid, component, args.User);
                break;

            case TapeRecorderMode.Rewinding:
                ToggleRewinding(uid, component, args.User);
                break;
            case TapeRecorderMode.Stopped:
            case TapeRecorderMode.Empty:
                return;
        }

        args.Handled = true;
        _useDelay.BeginDelay(uid);
    }

    /// <summary>
    /// When examining the tape, show current position
    /// </summary>
    protected void OnTapeExamined(EntityUid uid, TapeCassetteComponent tapeCassetteComponent, ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            if (HasComp<FitsInTapeRecorderComponent>(uid))
            {
                var positionPercentage = Math.Floor(tapeCassetteComponent.CurrentPosition / tapeCassetteComponent.MaxCapacity.TotalSeconds * 100);
                var tapePosMsg = Loc.GetString("tape-cassette-position",
                    ("position", positionPercentage)
                    );
                args.PushMarkup(tapePosMsg);
            }
           else
            {
                args.PushMarkup(Loc.GetString("tape-cassette-damaged"));
            }
        }
    }

    /// <summary>
    /// When examining the tape recorder, show current mode and tape position (if a tape is inserted)
    /// </summary>
    protected void OnRecorderExamined(EntityUid uid, TapeRecorderComponent component, ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            switch (component.Mode)
            {
                case TapeRecorderMode.Playing:
                    args.PushMarkup(Loc.GetString("tape-recorder-playing"));
                    break;
                case TapeRecorderMode.Stopped:
                    args.PushMarkup(Loc.GetString("tape-recorder-stopped"));
                    break;
                case TapeRecorderMode.Recording:
                    args.PushMarkup(Loc.GetString("tape-recorder-recording"));
                    break;
                case TapeRecorderMode.Rewinding:
                    args.PushMarkup(Loc.GetString("tape-recorder-rewinding"));
                    break;
                case TapeRecorderMode.Empty:
                    args.PushMarkup(Loc.GetString("tape-recorder-empty"));
                    break;
            }

            //Check if we have a tape cassette inserted
            var cassette = _itemSlotsSystem.GetItemOrNull(uid, SlotName);
            if (cassette != null)
            {
                //Sanity check, this IS a tape cassette right?
                if (TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
                {
                    var positionPercentage = Math.Floor(tapeCassetteComponent.CurrentPosition / tapeCassetteComponent.MaxCapacity.TotalSeconds * 100);
                    var tapePosMsg = Loc.GetString("tape-cassette-position",
                        ("position", positionPercentage)
                        );
                    args.PushMarkup(tapePosMsg);
                }
            }
        }
    }

    /// <summary>
    /// Stop whatever we are doing and swap to empty sprite.
    /// </summary>
    protected void OnCassetteRemoved(EntityUid uid, TapeRecorderComponent component, EntRemovedFromContainerMessage args)
    {
        switch (component.Mode)
        {
            case TapeRecorderMode.Playing:
                StopPlayback(uid, component);
                break;
            case TapeRecorderMode.Recording:
                StopRecording(uid, component);
                break;
            case TapeRecorderMode.Rewinding:
                StopRewinding(uid, component);
                break;
        }

        SetMode(uid, component, TapeRecorderMode.Empty, false);
    }

    /// <summary>
    /// Swap to stopped mode, update appearance
    /// </summary>
    protected void OnCassetteInserted(EntityUid uid, TapeRecorderComponent component, EntInsertedIntoContainerMessage args)
    {
        SetMode(uid, component, TapeRecorderMode.Stopped);
    }

    /// <summary>
    /// Update the appearance of the tape recorder, optionally ignoring the components mode
    /// </summary>
    protected void UpdateAppearance(EntityUid uid, TapeRecorderComponent component, TapeRecorderMode? modeOverride = null)
    {
        _appearanceSystem.SetData(uid, TapeRecorderVisuals.Status, modeOverride.HasValue ? modeOverride : component.Mode);
    }

    /// <summary>
    /// Choose a random recorded entry on the cassette, replace some of the letters to make it unintellegable - also change the name
    /// </summary>
    protected void CorruptRandomEntry(TapeCassetteComponent component)
    {
        if (component.RecordedData.Count == 0)
            return;

        var index = _robustRandom.Next(0, component.RecordedData.Count-1);
        var entryToCorrupt = component.RecordedData[index];

        var corruptedMessage = new StringBuilder();
        foreach (char character in entryToCorrupt.Message)
        {
            //25% chance for each character to be corrupted
            if (_robustRandom.GetRandom().Prob(0.25))
            {
                corruptedMessage.Append("#");
            }
            else
            {
                corruptedMessage.Append(character);
            }
        }

        var corruptedEntry = new TapeCassetteRecordedMessage(entryToCorrupt.Timestamp, "Unintelligible", corruptedMessage.ToString());
        corruptedEntry.Flushed = entryToCorrupt.Flushed;
        component.RecordedData[index] = corruptedEntry;
    }

    protected bool ToggleRecording(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (component.Active)
        {
            return StopRecording(tapeRecorder, component, user);
        }
        else
        {
            return StartRecording(tapeRecorder, component, user);
        }
    }
    /// <summary>
    /// Start recording if we are not already recording
    /// </summary>
    protected virtual bool StartRecording(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        //Are we already recording? if yes then abort
        if (component.Mode != TapeRecorderMode.Recording || component.Active)
            return false;

        //Mark tape recorder as recording
        AddComp<RecordingTapeRecorderComponent>(tapeRecorder);
        component.Active = true;

        UpdateAppearance(tapeRecorder, component);

        //Play predicted if we know which user triggered this method
        //Auto stop when reaching the end of the tape doesnt have a user for example
        _audioSystem.PlayPredicted(component.PlaySound, tapeRecorder, user);

        return true;
    }

    /// <summary>
    /// Stop recording if we are currently recording
    /// </summary>
    protected virtual bool StopRecording(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        //Are we recording? if not then abort
        if (component.Mode != TapeRecorderMode.Recording || !component.Active)
            return false;

        RemCompDeferred<RecordingTapeRecorderComponent>(tapeRecorder);
        component.Active = false;

        UpdateAppearance(tapeRecorder, component, TapeRecorderMode.Stopped);

        _audioSystem.PlayPredicted(component.StopSound, tapeRecorder, user);

        return true;
    }

    protected bool TogglePlayback(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (component.Active)
        {
            return StopPlayback(tapeRecorder, component, user);
        }
        else
        {
            return StartPlayback(tapeRecorder, component, user);
        }
    }
    /// <summary>
    /// Start playback if we are not already playing
    /// </summary>
    protected virtual bool StartPlayback(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (component.Mode != TapeRecorderMode.Playing || component.Active)
            return false;

        AddComp<PlayingTapeRecorderComponent>(tapeRecorder);
        component.Active = true;

        UpdateAppearance(tapeRecorder, component);

        _audioSystem.PlayPredicted(component.PlaySound, tapeRecorder, user);

        return true;
    }

    /// <summary>
    /// Stop playback if we are playing
    /// </summary>
    protected virtual bool StopPlayback(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (component.Mode != TapeRecorderMode.Playing || !component.Active)
            return false;

        RemCompDeferred<PlayingTapeRecorderComponent>(tapeRecorder);
        component.Active = false;

        UpdateAppearance(tapeRecorder, component, TapeRecorderMode.Stopped);

        _audioSystem.PlayPredicted(component.StopSound, tapeRecorder, user);

        return true;
    }

    protected bool ToggleRewinding(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (component.Active)
        {
            return StopRewinding(tapeRecorder, component, user);
        }
        else
        {
            return StartRewinding(tapeRecorder, component, user);
        }
    }
    /// <summary>
    /// Start rewinding the tape if we are not already rewinding
    /// </summary>
    protected virtual bool StartRewinding(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (component.Mode != TapeRecorderMode.Rewinding || component.Active)
            return false;

        AddComp<RewindingTapeRecorderComponent>(tapeRecorder);
        component.Active = true;

        UpdateAppearance(tapeRecorder, component);

        _audioSystem.PlayPredicted(component.RewindSound, tapeRecorder, user);

        return true;
    }

    /// <summary>
    /// Stop rewinding if we are rewinding
    /// </summary>
    protected virtual bool StopRewinding(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (component.Mode != TapeRecorderMode.Rewinding || !component.Active)
            return false;

        RemCompDeferred<RewindingTapeRecorderComponent>(tapeRecorder);
        component.Active = false;

        UpdateAppearance(tapeRecorder, component, TapeRecorderMode.Stopped);

        _audioSystem.PlayPredicted(component.StopSound, tapeRecorder, user);

        return true;
    }

    protected void SetMode(EntityUid tapeRecorder, TapeRecorderComponent component, TapeRecorderMode mode, bool updateAppearance = true)
    {
        if (mode == component.Mode)
            return;

        component.Mode = mode;
        Dirty(tapeRecorder, component);
        if (updateAppearance) UpdateAppearance(tapeRecorder, component);
    }
}
