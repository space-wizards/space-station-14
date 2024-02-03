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
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
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
        SubscribeLocalEvent<TapeRecorderComponent, GetVerbsEvent<AlternativeVerb>>(GetAltVerbs);

        SubscribeLocalEvent<TapeCassetteComponent, ExaminedEvent>(OnTapeExamined);
        SubscribeLocalEvent<TapeCassetteComponent, DamageChangedEvent>(OnDamagedChanged);
        SubscribeLocalEvent<TapeCassetteComponent, InteractUsingEvent>(OnInteractingWithCassette);
        SubscribeLocalEvent<TapeCassetteComponent, TapeCassetteRepairDoAfterEvent>(OnTapeCassetteRepair);
    }

    /// <summary>
    /// Process active tape recorder modes
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        //Get all recording tape recorders, increment the cassette position
        var recorderQuery = EntityQueryEnumerator<RecordingTapeRecorderComponent, TapeRecorderComponent>();
        while (recorderQuery.MoveNext(out var uid, out _, out var tapeRecorderComponent))
        {
            ProcessRecordingTapeRecorder((uid, tapeRecorderComponent), frameTime);
        }

        //Get all playing tape recorders, increment cassette position and play any messages from the interval
        var playerQuery = EntityQueryEnumerator<PlayingTapeRecorderComponent, TapeRecorderComponent>();
        while (playerQuery.MoveNext(out var uid, out _, out var tapeRecorderComponent))
        {
            ProcessPlayingTapeRecorder((uid, tapeRecorderComponent), frameTime);
        }

        //Get all rewinding tape recorders
        var rewindingQuery = EntityQueryEnumerator<RewindingTapeRecorderComponent, TapeRecorderComponent>();
        while (rewindingQuery.MoveNext(out var uid, out _, out var tapeRecorderComponent))
        {
            ProcessRewindingTapeRecorder((uid, tapeRecorderComponent), frameTime);
        }
    }

    /// <summary>
    /// Right click menu to swap mode
    /// </summary>
    private void GetAltVerbs(Entity<TapeRecorderComponent> tapeRecorder, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        //Dont allow mode changes when the mode is active
        if (tapeRecorder.Comp.Active)
            return;

        //If no tape is loaded, show no options
        var cassette = _itemSlotsSystem.GetItemOrNull(tapeRecorder, SlotName);
        if (cassette == null)
            return;

        //Sanity check, this is a tape? right?
        if (!TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
            return;

        //If we have tape capacity remaining
        if (tapeCassetteComponent.MaxCapacity.TotalSeconds > tapeCassetteComponent.CurrentPosition)
        {

            if (tapeRecorder.Comp.Mode != TapeRecorderMode.Recording)
            {
                args.Verbs.Add(new AlternativeVerb()
                {
                    Text = Loc.GetString("verb-tape-recorder-record"),
                    Act = () =>
                    {
                        SetMode(tapeRecorder, TapeRecorderMode.Recording, false);
                    },
                    Icon = tapeRecorder.Comp.RecordIcon,
                    Priority = 1
                });
            }

            if (tapeRecorder.Comp.Mode != TapeRecorderMode.Playing)
            {
                args.Verbs.Add(new AlternativeVerb()
                {
                    Text = Loc.GetString("verb-tape-recorder-playback"),
                    Act = () =>
                    {
                        SetMode(tapeRecorder, TapeRecorderMode.Playing, false);
                    },
                    Icon = tapeRecorder.Comp.PlayIcon,
                    Priority = 2
                });
            }
        }

        //If there is tape to rewind and we are not already rewinding
        if (tapeCassetteComponent.CurrentPosition > float.Epsilon && tapeRecorder.Comp.Mode != TapeRecorderMode.Rewinding)
        {
            args.Verbs.Add(new AlternativeVerb()
            {
                Text = Loc.GetString("verb-tape-recorder-rewind"),
                Act = () =>
                {
                    SetMode(tapeRecorder, TapeRecorderMode.Rewinding, false);
                },
                Icon = tapeRecorder.Comp.RewindIcon,
                Priority = 3
            });
        }
    }

    /// <summary>
    /// Update the tape position and overwrite any messages between the previous and new position
    /// </summary>
    /// <param name="tapeRecorder">The tape recorder to process</param>
    /// <param name="frameTime">Number of seconds that have passed since the last call</param>
    /// <returns>True if the tape recorder should continue in the current mode, False if it should switch to the Stopped mode</returns>
    protected virtual bool ProcessRecordingTapeRecorder(Entity<TapeRecorderComponent> tapeRecorder, float frameTime)
    {
        var cassette = _itemSlotsSystem.GetItemOrNull(tapeRecorder, SlotName);
        if (cassette == null)
            return false;

        if (!TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
            return false;

        var currentTime = tapeCassetteComponent.CurrentPosition + frameTime;

        //'Flushed' in this context is a mark indicating the message was not added between the last update and this update
        //Remove any flushed messages in the segment we just recorded over (ie old messages)
        tapeCassetteComponent.RecordedData.RemoveAll(x => x.Timestamp > tapeCassetteComponent.CurrentPosition && x.Timestamp <= currentTime && x.Flushed);

        //Mark all messages as flushed so they can be overwritten next time
        tapeCassetteComponent.RecordedData.ForEach(x => x.Flushed = true);

        //Update the tape's current time
        tapeCassetteComponent.CurrentPosition = (float) Math.Min(currentTime, tapeCassetteComponent.MaxCapacity.TotalSeconds);

        //If we have reached the end of the tape - stop
        if (tapeCassetteComponent.CurrentPosition >= tapeCassetteComponent.MaxCapacity.TotalSeconds)
            return false;

        return true;
    }

    /// <summary>
    /// Update the tape position and play any messages with timestamps between the previous and new position
    /// </summary>
    /// <param name="tapeRecorder">The tape recorder to process</param>
    /// <param name="frameTime">Number of seconds that have passed since the last call</param>
    /// <returns>True if the tape recorder should continue in the current mode, False if it should switch to the Stopped mode</returns>
    protected virtual bool ProcessPlayingTapeRecorder(Entity<TapeRecorderComponent> tapeRecorder, float frameTime)
    {
        var cassette = _itemSlotsSystem.GetItemOrNull(tapeRecorder, SlotName);
        if (cassette == null)
            return false;

        if (!TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
            return false;

        //Get the segment of the tape to be played
        //And any messages within that time period
        var currentTime = tapeCassetteComponent.CurrentPosition + frameTime;
        ReplayMessagesInSegment(tapeRecorder, tapeCassetteComponent, tapeCassetteComponent.CurrentPosition, currentTime);

        //Update the tape's position
        tapeCassetteComponent.CurrentPosition = (float) Math.Min(currentTime, tapeCassetteComponent.MaxCapacity.TotalSeconds);

        //Stop when we reach the end of the tape
        if (tapeCassetteComponent.CurrentPosition >= tapeCassetteComponent.MaxCapacity.TotalSeconds)
            return false;

        return true;
    }

    /// <summary>
    /// Update the tape position in reverse
    /// </summary>
    /// <param name="tapeRecorder">The tape recorder to process</param>
    /// <param name="frameTime">Number of seconds that have passed since the last call</param>
    /// <returns>True if the tape recorder should continue in the current mode, False if it should switch to the Stopped mode</returns>
    protected virtual bool ProcessRewindingTapeRecorder(Entity<TapeRecorderComponent> tapeRecorder, float frameTime)
    {
        var cassette = _itemSlotsSystem.GetItemOrNull(tapeRecorder, SlotName);
        if (cassette == null)
            return false;

        if (!TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
            return false;

        //Calculate how far we have rewound
        var rewindTime = frameTime * tapeRecorder.Comp.RewindSpeed;
        //Update the current time, clamp to 0
        var currentTime = Math.Max(0, tapeCassetteComponent.CurrentPosition - rewindTime);
        tapeCassetteComponent.CurrentPosition = currentTime;

        //If we have reached the beginning of the tape, stop
        if (tapeCassetteComponent.CurrentPosition <= float.Epsilon)
            return false;

        return true;
    }

    protected virtual void ReplayMessagesInSegment(Entity<TapeRecorderComponent> tapeRecorder, TapeCassetteComponent tapeCassetteComponent, float segmentStart, float segmentEnd)
    {

    }

    protected void OnInteractingWithCassette(Entity<TapeCassetteComponent> tapeCassette, ref InteractUsingEvent args)
    {
        //Is the tape damaged?
        if (HasComp<FitsInTapeRecorderComponent>(tapeCassette))
            return;

        //Are we using a pen or screwdriver?
        if (!_tagSystem.HasAnyTag(args.Used, "Screwdriver", "Write"))
            return;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, tapeCassette.Comp.RepairDelay, new TapeCassetteRepairDoAfterEvent(), tapeCassette, target: args.Target, used: args.Used)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = true
        });
    }

    protected void OnTapeCassetteRepair(Entity<TapeCassetteComponent> tapeCassette, ref TapeCassetteRepairDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        //Already repaired?
        if (HasComp<FitsInTapeRecorderComponent>(tapeCassette))
            return;

        _appearanceSystem.SetData(tapeCassette, ToggleVisuals.Toggled, false);
        AddComp<FitsInTapeRecorderComponent>(tapeCassette);
        args.Handled = true;
    }

    /// <summary>
    /// When the cassette has been damaged, corrupt and entry and unspool it
    /// </summary>
    protected void OnDamagedChanged(Entity<TapeCassetteComponent> tapeCassette, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null || args.DamageDelta.GetTotal() < 5)
            return;

        _appearanceSystem.SetData(tapeCassette, ToggleVisuals.Toggled, true);

        RemComp<FitsInTapeRecorderComponent>(tapeCassette);
        CorruptRandomEntry(tapeCassette);
    }

    /// <summary>
    /// When used in hand, activate/deactivate the current mode
    /// </summary>
    protected void OnUseInHand(Entity<TapeRecorderComponent> tapeRecorder, ref UseInHandEvent args)
    {
        if (!TryComp(tapeRecorder, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((tapeRecorder, useDelay)))
            return;

        switch (tapeRecorder.Comp.Mode)
        {
            case TapeRecorderMode.Recording:
                ToggleRecording(tapeRecorder, args.User);
                break;

            case TapeRecorderMode.Playing:
                TogglePlayback(tapeRecorder, args.User);
                break;

            case TapeRecorderMode.Rewinding:
                ToggleRewinding(tapeRecorder, args.User);
                break;

            default:
                return;
        }

        args.Handled = true;
        _useDelay.TryResetDelay((tapeRecorder, useDelay));
    }

    protected void OnTapeExamined(Entity<TapeCassetteComponent> tapeCassette, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            if (HasComp<FitsInTapeRecorderComponent>(tapeCassette))
            {
                var positionPercentage = Math.Floor(tapeCassette.Comp.CurrentPosition / tapeCassette.Comp.MaxCapacity.TotalSeconds * 100);
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

    protected void OnRecorderExamined(Entity<TapeRecorderComponent> tapeRecorder, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            switch (tapeRecorder.Comp.Mode)
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
            var cassette = _itemSlotsSystem.GetItemOrNull(tapeRecorder, SlotName);
            if (!cassette.HasValue)
                return;

            if (!TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
                return;

            OnTapeExamined((cassette.Value, tapeCassetteComponent), ref args);
        }
    }

    protected void OnCassetteRemoved(Entity<TapeRecorderComponent> tapeRecorder, ref EntRemovedFromContainerMessage args)
    {
        switch (tapeRecorder.Comp.Mode)
        {
            case TapeRecorderMode.Playing:
                StopPlayback(tapeRecorder);
                break;
            case TapeRecorderMode.Recording:
                StopRecording(tapeRecorder);
                break;
            case TapeRecorderMode.Rewinding:
                StopRewinding(tapeRecorder);
                break;
        }

        SetMode(tapeRecorder, TapeRecorderMode.Empty);
    }

    protected void OnCassetteInserted(Entity<TapeRecorderComponent> tapeRecorder, ref EntInsertedIntoContainerMessage args)
    {
        SetMode(tapeRecorder, TapeRecorderMode.Stopped);
    }

    /// <summary>
    /// Update the appearance of the tape recorder, optionally ignoring the components mode
    /// </summary>
    /// <param name="tapeRecorder">The tape recorder to update</param>
    /// <param name="modeOverride">If set, use this mode instead of the mode on the component</param>
    protected void UpdateAppearance(Entity<TapeRecorderComponent> tapeRecorder, TapeRecorderMode? modeOverride = null)
    {
        _appearanceSystem.SetData(tapeRecorder, TapeRecorderVisuals.Status, modeOverride.HasValue ? modeOverride : tapeRecorder.Comp.Mode);
    }

    /// <summary>
    /// Choose a random recorded entry on the cassette and replace some of the text with hashes
    /// </summary>
    /// <param name="component"></param>
    protected void CorruptRandomEntry(TapeCassetteComponent component)
    {
        if (component.RecordedData.Count == 0)
            return;

        var index = _robustRandom.Next(0, component.RecordedData.Count);
        var entryToCorrupt = component.RecordedData[index];

        var corruptedMessage = new StringBuilder();
        foreach (char character in entryToCorrupt.Message)
        {
            //25% chance for each character to be corrupted
            if (_robustRandom.GetRandom().Prob(0.25))
            {
                corruptedMessage.Append(Loc.GetString(component.CorruptionCharacter));
            }
            else
            {
                corruptedMessage.Append(character);
            }
        }

        var corruptedEntry = new TapeCassetteRecordedMessage(entryToCorrupt.Timestamp, Loc.GetString(component.Unintelligable), corruptedMessage.ToString());
        corruptedEntry.Flushed = entryToCorrupt.Flushed;
        component.RecordedData[index] = corruptedEntry;
    }

    protected bool ToggleRecording(Entity<TapeRecorderComponent> tapeRecorder, EntityUid? user = null)
    {
        if (tapeRecorder.Comp.Active)
        {
            return StopRecording(tapeRecorder, user);
        }
        else
        {
            return StartRecording(tapeRecorder, user);
        }
    }

    /// <summary>
    /// Start recording if we are not already recording
    /// </summary>
    protected virtual bool StartRecording(Entity<TapeRecorderComponent> tapeRecorder, EntityUid? user = null)
    {
        //Are we already recording? if yes then abort
        if (tapeRecorder.Comp.Mode != TapeRecorderMode.Recording || tapeRecorder.Comp.Active)
            return false;

        EnsureComp<RecordingTapeRecorderComponent>(tapeRecorder);

        SetActive(tapeRecorder, true);

        UpdateAppearance(tapeRecorder);

        //Play predicted if we know which user triggered this method
        //Auto stop when reaching the end of the tape doesnt have a user for example
        _audioSystem.PlayPredicted(tapeRecorder.Comp.PlaySound, tapeRecorder, user);

        return true;
    }

    /// <summary>
    /// Stop recording if we are currently recording
    /// </summary>
    protected virtual bool StopRecording(Entity<TapeRecorderComponent> tapeRecorder, EntityUid? user = null)
    {
        //Are we recording? if not then abort
        if (tapeRecorder.Comp.Mode != TapeRecorderMode.Recording || !tapeRecorder.Comp.Active)
            return false;

        RemCompDeferred<RecordingTapeRecorderComponent>(tapeRecorder);

        SetActive(tapeRecorder, false);

        UpdateAppearance(tapeRecorder, TapeRecorderMode.Stopped);

        _audioSystem.PlayPredicted(tapeRecorder.Comp.StopSound, tapeRecorder, user);

        return true;
    }

    protected bool TogglePlayback(Entity<TapeRecorderComponent> tapeRecorder, EntityUid? user = null)
    {
        if (tapeRecorder.Comp.Active)
        {
            return StopPlayback(tapeRecorder, user);
        }
        else
        {
            return StartPlayback(tapeRecorder, user);
        }
    }

    /// <summary>
    /// Start playback if we are not already playing
    /// </summary>
    protected virtual bool StartPlayback(Entity<TapeRecorderComponent> tapeRecorder, EntityUid? user = null)
    {
        if (tapeRecorder.Comp.Mode != TapeRecorderMode.Playing || tapeRecorder.Comp.Active)
            return false;

        EnsureComp<PlayingTapeRecorderComponent>(tapeRecorder);

        SetActive(tapeRecorder, true);

        UpdateAppearance(tapeRecorder);

        _audioSystem.PlayPredicted(tapeRecorder.Comp.PlaySound, tapeRecorder, user);

        return true;
    }

    /// <summary>
    /// Stop playback if we are playing
    /// </summary>
    protected virtual bool StopPlayback(Entity<TapeRecorderComponent> tapeRecorder, EntityUid? user = null)
    {
        if (tapeRecorder.Comp.Mode != TapeRecorderMode.Playing || !tapeRecorder.Comp.Active)
            return false;

        RemCompDeferred<PlayingTapeRecorderComponent>(tapeRecorder);

        SetActive(tapeRecorder, false);

        UpdateAppearance(tapeRecorder, TapeRecorderMode.Stopped);

        _audioSystem.PlayPredicted(tapeRecorder.Comp.StopSound, tapeRecorder, user);

        return true;
    }

    protected bool ToggleRewinding(Entity<TapeRecorderComponent> tapeRecorder, EntityUid? user = null)
    {
        if (tapeRecorder.Comp.Active)
        {
            return StopRewinding(tapeRecorder, user);
        }
        else
        {
            return StartRewinding(tapeRecorder, user);
        }
    }

    /// <summary>
    /// Start rewinding the tape if we are not already rewinding
    /// </summary>
    protected virtual bool StartRewinding(Entity<TapeRecorderComponent> tapeRecorder, EntityUid? user = null)
    {
        if (tapeRecorder.Comp.Mode != TapeRecorderMode.Rewinding || tapeRecorder.Comp.Active)
            return false;

        //Check if we have a tape cassette inserted
        var cassette = _itemSlotsSystem.GetItemOrNull(tapeRecorder, SlotName);
        if (!cassette.HasValue)
            return false;

        EnsureComp<RewindingTapeRecorderComponent>(tapeRecorder);

        SetActive(tapeRecorder, true);

        UpdateAppearance(tapeRecorder);

        _audioSystem.PlayPredicted(tapeRecorder.Comp.RewindSound, tapeRecorder, user);

        return true;
    }

    /// <summary>
    /// Stop rewinding if we are rewinding
    /// </summary>
    protected virtual bool StopRewinding(Entity<TapeRecorderComponent> tapeRecorder, EntityUid? user = null)
    {
        if (tapeRecorder.Comp.Mode != TapeRecorderMode.Rewinding || !tapeRecorder.Comp.Active)
            return false;

        RemCompDeferred<RewindingTapeRecorderComponent>(tapeRecorder);

        SetActive(tapeRecorder, false);

        UpdateAppearance(tapeRecorder, TapeRecorderMode.Stopped);

        _audioSystem.PlayPredicted(tapeRecorder.Comp.StopSound, tapeRecorder, user);

        return true;
    }

    /// <summary>
    /// Set the tape recorder mode and dirty if it is different from the previous mode
    /// </summary>
    /// <param name="tapeRecorder">The tape recorder to update</param>
    /// <param name="mode">The new mode</param>
    /// <param name="updateAppearance">Should the appearance of the tape recorder be updated</param>
    protected void SetMode(Entity<TapeRecorderComponent> tapeRecorder, TapeRecorderMode mode, bool updateAppearance = true)
    {
        if (mode == tapeRecorder.Comp.Mode)
            return;

        tapeRecorder.Comp.Mode = mode;
        tapeRecorder.Comp.Active = false;
        Dirty(tapeRecorder);
        if (updateAppearance) UpdateAppearance(tapeRecorder);
    }

    /// <summary>
    /// Set the tape recorder active state and dirty if it is different from the previous active state
    /// </summary>
    /// <param name="tapeRecorder"></param>
    /// <param name="active"></param>
    protected void SetActive(Entity<TapeRecorderComponent> tapeRecorder, bool active)
    {
        if (tapeRecorder.Comp.Active == active)
            return;

        tapeRecorder.Comp.Active = active;
        Dirty(tapeRecorder);

        //Only dirty the tape on stop
        if (!active) DirtyTape(tapeRecorder);
    }

    /// <summary>
    /// Dirty the tape position of the currently inserted tape
    /// </summary>
    /// <param name="tapeRecorder">The tape recorder containing the cassette to dirty</param>
    protected void DirtyTape(Entity<TapeRecorderComponent> tapeRecorder)
    {
        var cassette = _itemSlotsSystem.GetItemOrNull(tapeRecorder, SlotName);
        if (cassette == null)
            return;

        if (!TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
            return;

        //Dirty(cassette.Value, tapeCassetteComponent);
    }
}
