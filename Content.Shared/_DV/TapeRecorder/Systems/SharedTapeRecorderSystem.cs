using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared._DV.TapeRecorder.Components;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Toggleable;
using Content.Shared.UserInterface;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Content.Shared._DV.TapeRecorder.Systems;

public abstract class SharedTapeRecorderSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    protected const string SlotName = "cassette_tape";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TapeRecorderComponent, ItemSlotEjectAttemptEvent>(OnCassetteRemoveAttempt);
        SubscribeLocalEvent<TapeRecorderComponent, EntRemovedFromContainerMessage>(OnCassetteRemoved);
        SubscribeLocalEvent<TapeRecorderComponent, EntInsertedIntoContainerMessage>(OnCassetteInserted);
        SubscribeLocalEvent<TapeRecorderComponent, ExaminedEvent>(OnRecorderExamined);
        SubscribeLocalEvent<TapeRecorderComponent, ChangeModeTapeRecorderMessage>(OnChangeModeMessage);
        SubscribeLocalEvent<TapeRecorderComponent, AfterActivatableUIOpenEvent>(OnUIOpened);

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

        var query = EntityQueryEnumerator<ActiveTapeRecorderComponent, TapeRecorderComponent>();
        while (query.MoveNext(out var uid, out _, out var comp))
        {
            var ent = (uid, comp);
            if (!TryGetTapeCassette(uid, out var tape))
            {
                SetMode(ent, TapeRecorderMode.Stopped);
                continue;
            }

            var continuing = comp.Mode switch
            {
                TapeRecorderMode.Recording => ProcessRecordingTapeRecorder(ent, frameTime),
                TapeRecorderMode.Playing => ProcessPlayingTapeRecorder(ent, frameTime),
                TapeRecorderMode.Rewinding => ProcessRewindingTapeRecorder(ent, frameTime),
                _ => false
            };

            if (continuing)
                continue;

            SetMode(ent, TapeRecorderMode.Stopped);
            Dirty(tape); // make sure clients have the right value once it's stopped
        }
    }

    private void OnUIOpened(Entity<TapeRecorderComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUI(ent);
    }

    /// <summary>
    /// UI message when choosing between recorder modes
    /// </summary>
    private void OnChangeModeMessage(Entity<TapeRecorderComponent> ent, ref ChangeModeTapeRecorderMessage args)
    {
        SetMode(ent, args.Mode);
    }

    /// <summary>
    /// Update the tape position and overwrite any messages between the previous and new position
    /// </summary>
    /// <param name="ent">The tape recorder to process</param>
    /// <param name="frameTime">Number of seconds that have passed since the last call</param>
    /// <returns>True if the tape recorder should continue in the current mode, False if it should switch to the Stopped mode</returns>
    private bool ProcessRecordingTapeRecorder(Entity<TapeRecorderComponent> ent, float frameTime)
    {
        if (!TryGetTapeCassette(ent, out var tape))
            return false;

        var currentTime = tape.Comp.CurrentPosition + frameTime;

        //'Flushed' in this context is a mark indicating the message was not added between the last update and this update
        //Remove any flushed messages in the segment we just recorded over (ie old messages)
        tape.Comp.RecordedData.RemoveAll(x => x.Timestamp > tape.Comp.CurrentPosition && x.Timestamp <= currentTime);

        tape.Comp.RecordedData.AddRange(tape.Comp.Buffer);

        tape.Comp.Buffer.Clear();

        //Update the tape's current time
        tape.Comp.CurrentPosition = (float) Math.Min(currentTime, tape.Comp.MaxCapacity.TotalSeconds);

        //If we have reached the end of the tape - stop
        return tape.Comp.CurrentPosition < tape.Comp.MaxCapacity.TotalSeconds;
    }

    /// <summary>
    /// Update the tape position and play any messages with timestamps between the previous and new position
    /// </summary>
    /// <param name="ent">The tape recorder to process</param>
    /// <param name="frameTime">Number of seconds that have passed since the last call</param>
    /// <returns>True if the tape recorder should continue in the current mode, False if it should switch to the Stopped mode</returns>
    private bool ProcessPlayingTapeRecorder(Entity<TapeRecorderComponent> ent, float frameTime)
    {
        if (!TryGetTapeCassette(ent, out var tape))
            return false;

        //Get the segment of the tape to be played
        //And any messages within that time period
        var currentTime = tape.Comp.CurrentPosition + frameTime;

        ReplayMessagesInSegment(ent, tape.Comp, tape.Comp.CurrentPosition, currentTime);

        //Update the tape's position
        tape.Comp.CurrentPosition = (float) Math.Min(currentTime, tape.Comp.MaxCapacity.TotalSeconds);

        //Stop when we reach the end of the tape
        return tape.Comp.CurrentPosition < tape.Comp.MaxCapacity.TotalSeconds;
    }

    /// <summary>
    /// Update the tape position in reverse
    /// </summary>
    /// <param name="ent">The tape recorder to process</param>
    /// <param name="frameTime">Number of seconds that have passed since the last call</param>
    /// <returns>True if the tape recorder should continue in the current mode, False if it should switch to the Stopped mode</returns>
    private bool ProcessRewindingTapeRecorder(Entity<TapeRecorderComponent> ent, float frameTime)
    {
        if (!TryGetTapeCassette(ent, out var tape))
            return false;

        //Calculate how far we have rewound
        var rewindTime = frameTime * ent.Comp.RewindSpeed;
        //Update the current time, clamp to 0
        tape.Comp.CurrentPosition = Math.Max(0, tape.Comp.CurrentPosition - rewindTime);

        //If we have reached the beginning of the tape, stop
        return tape.Comp.CurrentPosition >= float.Epsilon;
    }

    /// <summary>
    /// Plays messages back on the server.
    /// Does nothing on the client.
    /// </summary>
    protected virtual void ReplayMessagesInSegment(Entity<TapeRecorderComponent> ent, TapeCassetteComponent tape, float segmentStart, float segmentEnd)
    {
    }

    /// <summary>
    /// Start repairing a damaged tape when using a screwdriver or pen on it
    /// </summary>
    protected void OnInteractingWithCassette(Entity<TapeCassetteComponent> ent, ref InteractUsingEvent args)
    {
        //Is the tape damaged?
        if (HasComp<FitsInTapeRecorderComponent>(ent))
            return;

        //Are we using a valid repair tool?
        if (_whitelist.IsWhitelistFail(ent.Comp.RepairWhitelist, args.Used))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.RepairDelay, new TapeCassetteRepairDoAfterEvent(), ent, target: ent, used: args.Used)
        {
            BreakOnMove = true,
            NeedHand = true
        });
    }

    /// <summary>
    /// Repair a damaged tape
    /// </summary>
    protected void OnTapeCassetteRepair(Entity<TapeCassetteComponent> ent, ref TapeCassetteRepairDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        //Cant repair if not damaged
        if (HasComp<FitsInTapeRecorderComponent>(ent))
            return;

        _appearance.SetData(ent, ToggleVisuals.Toggled, false);
        AddComp<FitsInTapeRecorderComponent>(ent);
        args.Handled = true;
    }

    /// <summary>
    /// When the cassette has been damaged, corrupt and entry and unspool it
    /// </summary>
    protected void OnDamagedChanged(Entity<TapeCassetteComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null || args.DamageDelta.GetTotal() < 5)
            return;

        _appearance.SetData(ent, ToggleVisuals.Toggled, true);

        RemComp<FitsInTapeRecorderComponent>(ent);
        CorruptRandomEntry(ent);
    }

    protected void OnTapeExamined(Entity<TapeCassetteComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!HasComp<FitsInTapeRecorderComponent>(ent))
        {
            args.PushMarkup(Loc.GetString("tape-cassette-damaged"));
            return;
        }

        var positionPercentage = Math.Floor(ent.Comp.CurrentPosition / ent.Comp.MaxCapacity.TotalSeconds * 100);
        var tapePosMsg = Loc.GetString("tape-cassette-position", ("position", positionPercentage));
        args.PushMarkup(tapePosMsg);
    }

    protected void OnRecorderExamined(Entity<TapeRecorderComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        //Check if we have a tape cassette inserted
        if (!TryGetTapeCassette(ent, out var tape))
        {
            args.PushMarkup(Loc.GetString("tape-recorder-empty"));
            return;
        }

        var state = ent.Comp.Mode.ToString().ToLower();
        args.PushMarkup(Loc.GetString("tape-recorder-" + state));

        OnTapeExamined(tape, ref args);
    }

    /// <summary>
    /// Prevent removing the tape cassette while the recorder is active
    /// </summary>
    protected void OnCassetteRemoveAttempt(Entity<TapeRecorderComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (!HasComp<ActiveTapeRecorderComponent>(ent))
            return;

        args.Cancelled = true;
    }

    protected void OnCassetteRemoved(Entity<TapeRecorderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        SetMode(ent, TapeRecorderMode.Stopped);
        UpdateAppearance(ent);
        UpdateUI(ent);
    }

    protected void OnCassetteInserted(Entity<TapeRecorderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateAppearance(ent);
        UpdateUI(ent);
    }

    /// <summary>
    /// Update the appearance of the tape recorder.
    /// </summary>
    /// <param name="ent">The tape recorder to update</param>
    protected void UpdateAppearance(Entity<TapeRecorderComponent> ent)
    {
        var hasCassette = TryGetTapeCassette(ent, out _);
        _appearance.SetData(ent, TapeRecorderVisuals.Mode, ent.Comp.Mode);
        _appearance.SetData(ent, TapeRecorderVisuals.TapeInserted, hasCassette);
    }

    /// <summary>
    /// Choose a random recorded entry on the cassette and replace some of the text with hashes
    /// </summary>
    /// <param name="component"></param>
    protected void CorruptRandomEntry(TapeCassetteComponent tape)
    {
        if (tape.RecordedData.Count == 0)
            return;

        var entry = _random.Pick(tape.RecordedData);

        var corruption = Loc.GetString("tape-recorder-message-corruption");

        var corruptedMessage = new StringBuilder();
        foreach (var character in entry.Message)
        {
            if (_random.Prob(tape.CorruptionChance))
                corruptedMessage.Append(corruption);
            else
                corruptedMessage.Append(character);
        }

        entry.Name = Loc.GetString("tape-recorder-voice-unintelligible");
        entry.Message = corruptedMessage.ToString();
    }

    /// <summary>
    /// Set the tape recorder mode and dirty if it is different from the previous mode
    /// </summary>
    /// <param name="ent">The tape recorder to update</param>
    /// <param name="mode">The new mode</param>
    private void SetMode(Entity<TapeRecorderComponent> ent, TapeRecorderMode mode)
    {
        if (mode == ent.Comp.Mode)
            return;

        if (mode == TapeRecorderMode.Stopped)
        {
            RemComp<ActiveTapeRecorderComponent>(ent);
        }
        else
        {
            // can't play without a tape in it...
            if (!TryGetTapeCassette(ent, out _))
                return;

            EnsureComp<ActiveTapeRecorderComponent>(ent);
        }

        var sound = ent.Comp.Mode switch
        {
            TapeRecorderMode.Stopped => ent.Comp.StopSound,
            TapeRecorderMode.Rewinding => ent.Comp.RewindSound,
            _ => ent.Comp.PlaySound
        };
        Audio.PlayPvs(sound, ent);

        ent.Comp.Mode = mode;
        Dirty(ent);

        UpdateUI(ent);
    }

    protected bool TryGetTapeCassette(EntityUid ent, [NotNullWhen(true)] out Entity<TapeCassetteComponent> tape)
    {
        if (_slots.GetItemOrNull(ent, SlotName) is not {} cassette)
        {
            tape = default!;
            return false;
        }

        if (!TryComp<TapeCassetteComponent>(cassette, out var comp))
        {
            tape = default!;
            return false;
        }

        tape = new(cassette, comp);
        return true;
    }

    private void UpdateUI(Entity<TapeRecorderComponent> ent)
    {
        var (uid, comp) = ent;
        if (!_ui.IsUiOpen(uid, TapeRecorderUIKey.Key))
            return;

        var hasCassette = TryGetTapeCassette(ent, out var tape);
        var hasData = false;
        var currentTime = 0f;
        var maxTime = 0f;
        var cassetteName = "Unnamed";
        var cooldown = comp.PrintCooldown;

        if (hasCassette)
        {
            hasData = tape.Comp.RecordedData.Count > 0;
            currentTime = tape.Comp.CurrentPosition;
            maxTime = (float) tape.Comp.MaxCapacity.TotalSeconds;

            if (TryComp<LabelComponent>(tape, out var labelComp))
                if (labelComp.CurrentLabel != null)
                    cassetteName = labelComp.CurrentLabel;
        }

        var state = new TapeRecorderState(
            hasCassette,
            hasData,
            currentTime,
            maxTime,
            cassetteName,
            cooldown);

        _ui.SetUiState(uid, TapeRecorderUIKey.Key, state);
    }
}

[Serializable, NetSerializable]
public sealed partial class TapeCassetteRepairDoAfterEvent : SimpleDoAfterEvent;
