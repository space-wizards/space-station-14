using System.Linq;
using Content.Shared.PDA.Ringer;
using Content.Shared.Popups;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.PDA;

public abstract class SharedRingerSystem : EntitySystem
{
    public const int RingtoneLength = 6;
    public const int NoteTempo = 300;
    public const float NoteDelay = 60f / NoteTempo;

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPdaSystem _pda = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RingerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RingerUplinkComponent, ComponentInit>(OnUplinkInit);

        // RingerBoundUserInterface Subscriptions
        SubscribeLocalEvent<RingerUplinkComponent, BeforeRingtoneSetEvent>(OnSetUplinkRingtone);
        SubscribeLocalEvent<RingerComponent, RingerSetRingtoneMessage>(OnSetRingtone);
        SubscribeLocalEvent<RingerComponent, RingerPlayRingtoneMessage>(OnRingerPlayRingtone);
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

    private void OnSetRingtone(Entity<RingerComponent> ent, ref RingerSetRingtoneMessage args)
    {
        // Prevent ringtone spam by checking the last time this ringtone was set
        var curTime = _timing.CurTime;
        if (ent.Comp.LastRingtoneSetTime > curTime - ent.Comp.Cooldown)
            return;

        ent.Comp.LastRingtoneSetTime = curTime;

        // Client sent us an updated ringtone so set it to that.
        if (args.Ringtone.Length != RingtoneLength)
            return;

        var ev = new BeforeRingtoneSetEvent(args.Ringtone);
        RaiseLocalEvent(ent, ref ev);
        if (ev.Handled)
            return;

        DirtyField(ent.AsNullable(), nameof(RingerComponent.LastRingtoneSetTime));
        UpdateRingerRingtone(ent, args.Ringtone);
    }

    private void OnRingerPlayRingtone(Entity<RingerComponent> ent, ref RingerPlayRingtoneMessage args)
    {
        StartRingtone(ent);
    }

    private void OnSetUplinkRingtone(Entity<RingerUplinkComponent> ent, ref BeforeRingtoneSetEvent args)
    {
        if (ent.Comp.Code.SequenceEqual(args.Ringtone) && HasComp<StoreComponent>(ent))
        {
            ent.Comp.Unlocked = !ent.Comp.Unlocked;
            if (TryComp<PdaComponent>(ent, out var pda))
                _pda.UpdatePdaUi(ent, pda);

            // can't keep store open after locking it
            if (!ent.Comp.Unlocked)
                UI.CloseUi(ent.Owner, StoreUiKey.Key);

            // no saving the code to prevent meta click set on sus guys pda -> wewlad
            args.Handled = true;
        }
    }

    public void RingerPlayRingtone(Entity<RingerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        StartRingtone((ent, ent.Comp));
    }

    private void StartRingtone(Entity<RingerComponent> ent)
    {
        // Already active? Don't start it again
        if (ent.Comp.Active)
            return;

        ent.Comp.Active = true;
        ent.Comp.NoteCount = 0;
        ent.Comp.NextNoteTime = _timing.CurTime;

        // No predicted popups with PVS filtering so we do this to avoid duplication
        if (_timing.IsFirstTimePredicted)
        {
            _popup.PopupPredicted(Loc.GetString("comp-ringer-vibration-popup"), ent, ent.Owner, PopupType.Medium);
        }
        else if (!_net.IsClient)
        {
            _popup.PopupEntity(Loc.GetString("comp-ringer-vibration-popup"), ent, Filter.Pvs(ent, 0.05f), false, PopupType.Medium);
        }

        DirtyFields(ent.Owner,
            ent.Comp,
            null,
            nameof(RingerComponent.NextNoteTime),
            nameof(RingerComponent.Active),
            nameof(RingerComponent.NoteCount));
    }

    /// <summary>
    /// Locks the uplink and closes the window, if its open
    /// </summary>
    /// <remarks>
    /// Will not update the PDA ui so you must do that yourself if needed
    /// </remarks>
    public void LockUplink(Entity<RingerUplinkComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Unlocked = false;
        UI.CloseUi(ent.Owner, StoreUiKey.Key);
    }

    private void OnMapInit(Entity<RingerComponent> ent, ref MapInitEvent args)
    {
        UpdateRingerRingtone(ent, GenerateRingtone());
    }

    private void OnUplinkInit(Entity<RingerUplinkComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Code = GenerateRingtone();
    }

    //Non Event Functions

    private Note[] GenerateRingtone()
    {
        // Default to using C pentatonic so it at least sounds not terrible.
        return GenerateRingtone(new[]
        {
            Note.C,
            Note.D,
            Note.E,
            Note.G,
            Note.A
        });
    }

    private Note[] GenerateRingtone(Note[] notes)
    {
        var ringtone = new Note[RingtoneLength];

        for (var i = 0; i < RingtoneLength; i++)
        {
            ringtone[i] = _random.Pick(notes);
        }

        return ringtone;
    }

    private void UpdateRingerRingtone(Entity<RingerComponent> ent, Note[] ringtone)
    {
        // Assume validation has already happened.
        ent.Comp.Ringtone = ringtone;
        DirtyField(ent.AsNullable(), nameof(RingerComponent.Ringtone));
        UpdateRingerUi(ent);
    }

    protected virtual void UpdateRingerUi(Entity<RingerComponent> ent)
    {
    }

    public bool ToggleRingerUi(EntityUid uid, EntityUid actor)
    {
        UI.TryToggleUi(uid, RingerUiKey.Key, actor);
        return true;
    }

    private static SoundPathSpecifier GetSound(Note note)
    {
        return new SoundPathSpecifier($"/Audio/Effects/RingtoneNotes/{note.ToString().ToLower()}.ogg");
    }
}

[ByRefEvent]
public record struct BeforeRingtoneSetEvent(Note[] Ringtone, bool Handled = false);

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
