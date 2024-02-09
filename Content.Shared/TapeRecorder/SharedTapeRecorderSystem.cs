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
using Robust.Shared.Timing;
using Robust.Shared.Player;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata.Ecma335;
using Content.Shared.Body.Components;
using Content.Shared.Popups;

namespace Content.Shared.TapeRecorder;

public abstract class SharedTapeRecorderSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    protected const string SlotName = "cassette_tape";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TapeRecorderComponent, ItemSlotEjectAttemptEvent>(OnCassetteRemoveAttempt);
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

        var tapeRecorders = EntityQueryEnumerator<TapeRecorderComponent>();
        while (tapeRecorders.MoveNext(out var uid, out var tapeRecorderComponent))
        {
            if (!tapeRecorderComponent.Active)
                continue;

            switch (tapeRecorderComponent.Mode)
            {
                case TapeRecorderMode.Recording:
                    {
                        ProcessRecordingTapeRecorder((uid, tapeRecorderComponent), frameTime);
                        break;
                    }
                case TapeRecorderMode.Playing:
                    {
                        ProcessPlayingTapeRecorder((uid, tapeRecorderComponent), frameTime);
                        break;
                    }
                case TapeRecorderMode.Rewinding:
                    {
                        ProcessRewindingTapeRecorder((uid, tapeRecorderComponent), frameTime);
                        break;
                    }
                default:
                    continue;
            }
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

        if (!TryGetTapeCassette(tapeRecorder, out var tapeCassette))
            return;

        //If we have tape capacity remaining
        if (tapeCassette.Comp.MaxCapacity.TotalSeconds > tapeCassette.Comp.CurrentPosition)
        {

            if (tapeRecorder.Comp.Mode != TapeRecorderMode.Recording)
            {
                args.Verbs.Add(new AlternativeVerb()
                {
                    Text = Loc.GetString(tapeRecorder.Comp.TextVerbRecord),
                    Act = () =>
                    {
                        SetMode(tapeRecorder, TapeRecorderMode.Recording);
                    },
                    Icon = tapeRecorder.Comp.RecordIcon,
                    Priority = 1
                });
            }

            if (tapeRecorder.Comp.Mode != TapeRecorderMode.Playing)
            {
                args.Verbs.Add(new AlternativeVerb()
                {
                    Text = Loc.GetString(tapeRecorder.Comp.TextVerbPlay),
                    Act = () =>
                    {
                        SetMode(tapeRecorder, TapeRecorderMode.Playing);
                    },
                    Icon = tapeRecorder.Comp.PlayIcon,
                    Priority = 2
                });
            }
        }

        //If there is tape to rewind and we are not already rewinding
        if (tapeCassette.Comp.CurrentPosition > float.Epsilon && tapeRecorder.Comp.Mode != TapeRecorderMode.Rewinding)
        {
            args.Verbs.Add(new AlternativeVerb()
            {
                Text = Loc.GetString(tapeRecorder.Comp.TextVerbRewind),
                Act = () =>
                {
                    SetMode(tapeRecorder, TapeRecorderMode.Rewinding);
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
        if (!TryGetTapeCassette(tapeRecorder, out var tapeCassette))
            return false;

        var currentTime = tapeCassette.Comp.CurrentPosition + frameTime;

        //'Flushed' in this context is a mark indicating the message was not added between the last update and this update
        //Remove any flushed messages in the segment we just recorded over (ie old messages)
        tapeCassette.Comp.RecordedData.RemoveAll(x => x.Timestamp > tapeCassette.Comp.CurrentPosition && x.Timestamp <= currentTime);

        tapeCassette.Comp.RecordedData.AddRange(tapeCassette.Comp.Buffer);

        tapeCassette.Comp.Buffer.Clear();

        //Update the tape's current time
        tapeCassette.Comp.CurrentPosition = (float) Math.Min(currentTime, tapeCassette.Comp.MaxCapacity.TotalSeconds);

        //If we have reached the end of the tape - stop
        if (tapeCassette.Comp.CurrentPosition >= tapeCassette.Comp.MaxCapacity.TotalSeconds)
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
        if (!TryGetTapeCassette(tapeRecorder, out var tapeCassette))
            return false;

        //Get the segment of the tape to be played
        //And any messages within that time period
        var currentTime = tapeCassette.Comp.CurrentPosition + frameTime;

        ReplayMessagesInSegment(tapeRecorder, tapeCassette.Comp, tapeCassette.Comp.CurrentPosition, currentTime);

        //Update the tape's position
        tapeCassette.Comp.CurrentPosition = (float) Math.Min(currentTime, tapeCassette.Comp.MaxCapacity.TotalSeconds);

        //Stop when we reach the end of the tape
        if (tapeCassette.Comp.CurrentPosition >= tapeCassette.Comp.MaxCapacity.TotalSeconds)
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
        if (!TryGetTapeCassette(tapeRecorder, out var tapeCassette))
            return false;

        //Calculate how far we have rewound
        var rewindTime = frameTime * tapeRecorder.Comp.RewindSpeed;
        //Update the current time, clamp to 0
        tapeCassette.Comp.CurrentPosition = Math.Max(0, tapeCassette.Comp.CurrentPosition - rewindTime);

        //If we have reached the beginning of the tape, stop
        if (tapeCassette.Comp.CurrentPosition < float.Epsilon)
            return false;

        return true;
    }

    protected virtual void ReplayMessagesInSegment(Entity<TapeRecorderComponent> tapeRecorder, TapeCassetteComponent tapeCassetteComponent, float segmentStart, float segmentEnd)
    {

    }

    /// <summary>
    /// Start repairing a damaged tape when using a screwdriver or pen on it
    /// </summary>
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

    /// <summary>
    /// Repair a damaged tape
    /// </summary>
    protected void OnTapeCassetteRepair(Entity<TapeCassetteComponent> tapeCassette, ref TapeCassetteRepairDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        //Cant repair if not damaged
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
    /// Activate/deactivate the current mode
    /// </summary>
    protected void OnUseInHand(Entity<TapeRecorderComponent> tapeRecorder, ref UseInHandEvent args)
    {
        if (!TryComp(tapeRecorder, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((tapeRecorder, useDelay)))
            return;

        if (!TryGetTapeCassette(tapeRecorder, out _))
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
                var tapePosMsg = Loc.GetString(tapeCassette.Comp.TextExamine,
                    ("position", positionPercentage)
                    );
                args.PushMarkup(tapePosMsg);
            }
           else
            {
                args.PushMarkup(Loc.GetString(tapeCassette.Comp.TextDamaged));
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
                    args.PushMarkup(Loc.GetString(tapeRecorder.Comp.TextModePlaying));
                    break;
                case TapeRecorderMode.Stopped:
                    args.PushMarkup(Loc.GetString(tapeRecorder.Comp.TextModeStopped));
                    break;
                case TapeRecorderMode.Recording:
                    args.PushMarkup(Loc.GetString(tapeRecorder.Comp.TextModeRecording));
                    break;
                case TapeRecorderMode.Rewinding:
                    args.PushMarkup(Loc.GetString(tapeRecorder.Comp.TextModeRewinding));
                    break;
                case TapeRecorderMode.Empty:
                    args.PushMarkup(Loc.GetString(tapeRecorder.Comp.TextModeEmpty));
                    break;
            }

            //Check if we have a tape cassette inserted
            if (!TryGetTapeCassette(tapeRecorder, out var tapeCassette))
                return;

            OnTapeExamined(tapeCassette, ref args);
        }
    }

    /// <summary>
    /// Prevent removing the tape cassette while the recorder is active
    /// Prevents the stop sound from playing twice (as we dont know who is ejecting the cassette in EntRemovedFromContainerMessage)
    /// </summary>
    protected void OnCassetteRemoveAttempt(Entity<TapeRecorderComponent> tapeRecorder, ref ItemSlotEjectAttemptEvent args)
    {
        if (tapeRecorder.Comp.Active)
        {
            args.Cancelled = true;

            if (args.User.HasValue)
                _popupSystem.PopupClient(Loc.GetString(tapeRecorder.Comp.TextCantEject), tapeRecorder, args.User.Value);
        }
    }
    protected void OnCassetteRemoved(Entity<TapeRecorderComponent> tapeRecorder, ref EntRemovedFromContainerMessage args)
    {
        //TODO: Allow removing tapes while running, requires getting the user who is ejecting
        SetMode(tapeRecorder, TapeRecorderMode.Empty);
        UpdateAppearance(tapeRecorder);
    }

    protected void OnCassetteInserted(Entity<TapeRecorderComponent> tapeRecorder, ref EntInsertedIntoContainerMessage args)
    {
        SetMode(tapeRecorder, TapeRecorderMode.Stopped);
        UpdateAppearance(tapeRecorder);
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
        foreach (var character in entryToCorrupt.Message)
        {
            //25% chance for each character to be corrupted
            if (_robustRandom.GetRandom().Prob(0.25))
            {
                corruptedMessage.Append(Loc.GetString(component.TextCorruptionCharacter));
            }
            else
            {
                corruptedMessage.Append(character);
            }
        }

        entryToCorrupt.Name = Loc.GetString(component.TextUnintelligable);
        entryToCorrupt.Message = corruptedMessage.ToString();
    }

    protected bool ToggleRecording(Entity<TapeRecorderComponent> tapeRecorder, EntityUid? user = null)
    {
        if (tapeRecorder.Comp.Active)
        {
            return Stop(tapeRecorder, user);
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

        SetActive(tapeRecorder, true);

        UpdateAppearance(tapeRecorder);

        _audioSystem.PlayPredicted(tapeRecorder.Comp.PlaySound, tapeRecorder, user);

        return true;
    }

    protected bool TogglePlayback(Entity<TapeRecorderComponent> tapeRecorder, EntityUid? user = null)
    {
        if (tapeRecorder.Comp.Active)
        {
            return Stop(tapeRecorder, user);
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

        SetActive(tapeRecorder, true);

        UpdateAppearance(tapeRecorder);

        _audioSystem.PlayPredicted(tapeRecorder.Comp.PlaySound, tapeRecorder, user);

        return true;
    }

    protected bool ToggleRewinding(Entity<TapeRecorderComponent> tapeRecorder, EntityUid? user = null)
    {
        if (tapeRecorder.Comp.Active)
        {
            return Stop(tapeRecorder, user);
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
        var cassetteInSlot = _itemSlotsSystem.GetItemOrNull(tapeRecorder, SlotName);
        if (cassetteInSlot is null)
            return false;

        SetActive(tapeRecorder, true);

        UpdateAppearance(tapeRecorder);

        _audioSystem.PlayPredicted(tapeRecorder.Comp.RewindSound, tapeRecorder, user);

        return true;
    }

    /// <summary>
    /// Stop rewinding if we are rewinding
    /// </summary>
    protected virtual bool Stop(Entity<TapeRecorderComponent> tapeRecorder, EntityUid? user = null, bool updateAppearance = true, bool changeMode = false)
    {
        if (!tapeRecorder.Comp.Active)
            return false;

        if (changeMode)
            SetMode(tapeRecorder, TapeRecorderMode.Stopped);

        SetActive(tapeRecorder, false);

        if (updateAppearance)
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
    protected void SetMode(Entity<TapeRecorderComponent> tapeRecorder, TapeRecorderMode mode)
    {
        if (mode == tapeRecorder.Comp.Mode)
            return;

        tapeRecorder.Comp.Mode = mode;

        Dirty(tapeRecorder);
    }

    protected void SetActive(Entity<TapeRecorderComponent> tapeRecorder, bool active)
    {
        if (active == tapeRecorder.Comp.Active)
            return;

        tapeRecorder.Comp.Active = active;

        Dirty(tapeRecorder);
    }

    protected bool TryGetTapeCassette(EntityUid tapeRecorder, [NotNullWhen(true)] out Entity<TapeCassetteComponent> tapeCassette)
    {
        var cassette = _itemSlotsSystem.GetItemOrNull(tapeRecorder, SlotName);
        if (cassette == null)
        {
            tapeCassette = default!;
            return false;
        }

        if (!TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
        {
            tapeCassette = default!;
            return false;
        }

        tapeCassette = new(cassette.Value, tapeCassetteComponent);

        return true;
    }
}
