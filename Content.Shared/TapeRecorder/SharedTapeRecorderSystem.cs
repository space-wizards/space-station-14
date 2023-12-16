using Content.Shared.Chat;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.TapeRecorder.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.TapeRecorder;

public abstract class SharedTapeRecorderSystem : EntitySystem
{

    [Dependency] protected readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] protected readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public readonly string SlotName = "cassette_tape";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TapeRecorderComponent, EntRemovedFromContainerMessage>(OnCassetteRemoved);
        SubscribeLocalEvent<TapeRecorderComponent, EntInsertedIntoContainerMessage>(OnCassetteInserted);
        SubscribeLocalEvent<TapeRecorderComponent, UseInHandEvent>(OnUseInHand);

        SubscribeLocalEvent<TapeCassetteComponent, ExaminedEvent>(OnTapeExamined);
        SubscribeLocalEvent<TapeRecorderComponent, ExaminedEvent>(OnRecorderExamined);
    }

    private void OnUseInHand(EntityUid uid, TapeRecorderComponent component, UseInHandEvent args)
    {
        switch (component.Mode)
        {
            case TapeRecorderMode.Recording:
                if (HasComp<RecordingTapeRecorderComponent>(uid))
                {
                    StopRecording(uid, component, args.User);
                }
                else
                {
                    StartRecording(uid, component, args.User);
                }
                break;
        }
    }

    protected virtual void OnTapeExamined(EntityUid uid, TapeCassetteComponent tapeCassetteComponent, ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            var positionPercentage = Math.Floor(tapeCassetteComponent.CurrentPosition / tapeCassetteComponent.MaxCapacity * 100);
            var tapePosMsg = Loc.GetString("tape-cassette-position",
                ("position", positionPercentage)
                );
            args.PushMarkup(tapePosMsg);
        }
    }
    protected virtual void OnRecorderExamined(EntityUid uid, TapeRecorderComponent component, ExaminedEvent args)
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
                case TapeRecorderMode.Empty:
                    args.PushMarkup(Loc.GetString("tape-recorder-empty"));
                    break;
            }

            var cassette = _itemSlotsSystem.GetItemOrNull(uid, SlotName);
            if (cassette != null)
            {
                if (TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
                {
                    var positionPercentage = Math.Floor(tapeCassetteComponent.CurrentPosition / tapeCassetteComponent.MaxCapacity * 100);
                    var tapePosMsg = Loc.GetString("tape-cassette-position",
                        ("position", positionPercentage)
                        );
                    args.PushMarkup(tapePosMsg);
                }
            }
        }
    }

    protected virtual void OnCassetteRemoved(EntityUid uid, TapeRecorderComponent component, EntRemovedFromContainerMessage args)
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

        component.Mode = TapeRecorderMode.Empty;
        UpdateAppearance(uid, component);
    }



    protected virtual void UpdateAppearance(EntityUid uid, TapeRecorderComponent component)
    {
        _appearanceSystem.SetData(uid, TapeRecorderVisuals.Status, component.Mode);
    }

    protected virtual void OnCassetteInserted(EntityUid uid, TapeRecorderComponent component, EntInsertedIntoContainerMessage args)
    {
        component.Mode = TapeRecorderMode.Stopped;
        UpdateAppearance(uid, component);
    }

    protected virtual void StartRecording(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (HasComp<RecordingTapeRecorderComponent>(tapeRecorder))
            return;

        AddComp<RecordingTapeRecorderComponent>(tapeRecorder);
        component.Mode = TapeRecorderMode.Recording;

        UpdateAppearance(tapeRecorder, component);

        if (user.HasValue)
        {
            _audioSystem.PlayPredicted(component.PlaySound, tapeRecorder, user);
        }
        else
        {
            _audioSystem.PlayPvs(component.PlaySound, tapeRecorder);
        }
    }
    protected virtual void StopRecording(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (!HasComp<RecordingTapeRecorderComponent>(tapeRecorder))
            return;

        RemComp<RecordingTapeRecorderComponent>(tapeRecorder);
        component.Mode = TapeRecorderMode.Stopped;

        UpdateAppearance(tapeRecorder, component);

        if (user.HasValue)
        {
            _audioSystem.PlayPredicted(component.StopSound, tapeRecorder, user);
        }
        else
        {
            _audioSystem.PlayPvs(component.StopSound, tapeRecorder);
        }
    }
    protected virtual void StartPlayback(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (HasComp<PlayingTapeRecorderComponent>(tapeRecorder))
            return;

        AddComp<PlayingTapeRecorderComponent>(tapeRecorder);
        component.Mode = TapeRecorderMode.Playing;

        UpdateAppearance(tapeRecorder, component);

        if (user.HasValue)
        {
            _audioSystem.PlayPredicted(component.PlaySound, tapeRecorder, user);
        }
        else
        {
            _audioSystem.PlayPvs(component.PlaySound, tapeRecorder);
        }
    }
    protected virtual void StopPlayback(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (!HasComp<PlayingTapeRecorderComponent>(tapeRecorder))
            return;

        RemComp<PlayingTapeRecorderComponent>(tapeRecorder);
        component.Mode = TapeRecorderMode.Stopped;

        UpdateAppearance(tapeRecorder, component);

        if (user.HasValue)
        {
            _audioSystem.PlayPredicted(component.StopSound, tapeRecorder, user);
        }
        else
        {
            _audioSystem.PlayPvs(component.StopSound, tapeRecorder);
        }
    }
    protected virtual void StartRewinding(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (HasComp<RewindingTapeRecorderComponent>(tapeRecorder))
            return;

        AddComp<RewindingTapeRecorderComponent>(tapeRecorder);
        component.Mode = TapeRecorderMode.Rewinding;

        UpdateAppearance(tapeRecorder, component);

        if (user.HasValue)
        {
            _audioSystem.PlayPredicted(component.RewindSound, tapeRecorder, user);
        }
        else
        {
            _audioSystem.PlayPvs(component.RewindSound, tapeRecorder);
        }
    }
    protected virtual void StopRewinding(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (!HasComp<RewindingTapeRecorderComponent>(tapeRecorder))
            return;

        RemComp<RewindingTapeRecorderComponent>(tapeRecorder);
        component.Mode = TapeRecorderMode.Stopped;

        UpdateAppearance(tapeRecorder, component);

        if (user.HasValue)
        {
            _audioSystem.PlayPredicted(component.StopSound, tapeRecorder, user);
        }
        else
        {
            _audioSystem.PlayPvs(component.StopSound, tapeRecorder);
        }
    }
}
