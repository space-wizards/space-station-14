using System.Linq;
using Content.Shared.PDA.Ringer;
using Content.Shared.Popups;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
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

    [Dependency] private readonly SharedPdaSystem _pda = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        // RingerBoundUserInterface Subscriptions
        SubscribeLocalEvent<RingerUplinkComponent, BeforeRingtoneSetEvent>(OnSetUplinkRingtone);
        SubscribeLocalEvent<RingerComponent, RingerSetRingtoneMessage>(OnSetRingtone);
        SubscribeLocalEvent<RingerComponent, RingerPlayRingtoneMessage>(RingerPlayRingtone);
        SubscribeLocalEvent<RingerComponent, RingerRequestUpdateInterfaceMessage>(UpdateRingerUserInterfaceDriver);
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        var curTime = _timing.CurTime;

        var ringerQuery = EntityQueryEnumerator<RingerComponent>();
        while (ringerQuery.MoveNext(out var uid, out var ringer))
        {
            if (!ringer.Active || !ringer.NextNoteTime.HasValue)
                continue;

            // Check if it's time to play the next note
            if (curTime < ringer.NextNoteTime.Value)
                continue;

            // Play current note
            var ringerXform = Transform(uid);
            _audio.PlayEntity(
                GetSound(ringer.Ringtone[ringer.NoteCount]),
                Filter.Empty().AddInRange(_xform.GetMapCoordinates(uid, ringerXform), ringer.Range),
                uid,
                true,
                AudioParams.Default.WithMaxDistance(ringer.Range).WithVolume(ringer.Volume)
            );

            // Schedule next note
            ringer.NextNoteTime = curTime + TimeSpan.FromSeconds(NoteDelay);
            ringer.NoteCount++;

            // Check if we've finished playing all notes
            if (ringer.NoteCount >= RingtoneLength)
            {
                ringer.Active = false;
                ringer.NextNoteTime = null;
                ringer.NoteCount = 0;
                UpdateRingerUi((uid, ringer), false);
            }
        }
    }

    private void UpdateRingerUserInterfaceDriver(Entity<RingerComponent> ent, ref RingerRequestUpdateInterfaceMessage args)
    {
        UpdateRingerUi(ent, ent.Comp.Active);
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

        UpdateRingerRingtone(ent, args.Ringtone);
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
                _ui.CloseUi(ent.Owner, StoreUiKey.Key);

            // no saving the code to prevent meta click set on sus guys pda -> wewlad
            args.Handled = true;
        }
    }

    private void RingerPlayRingtone(Entity<RingerComponent> ent, ref RingerPlayRingtoneMessage args)
    {
        StartRingtone(ent);
    }

    public void RingerPlayRingtone(Entity<RingerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        StartRingtone((ent, ent.Comp));
    }

    private void StartRingtone(Entity<RingerComponent> ent)
    {
        ent.Comp.Active = true;
        ent.Comp.NoteCount = 0;
        ent.Comp.NextNoteTime = _timing.CurTime;

        _popup.PopupEntity(Loc.GetString("comp-ringer-vibration-popup"), ent, Filter.Pvs(ent, 0.05f), false, PopupType.Medium);

        Dirty(ent);
        UpdateRingerUi(ent, true);
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
        _ui.CloseUi(ent.Owner, StoreUiKey.Key);
    }

    protected void RandomizeRingtone(Entity<RingerComponent> ent, ref MapInitEvent args)
    {
        UpdateRingerRingtone(ent, GenerateRingtone());
    }

    protected void RandomizeUplinkCode(Entity<RingerUplinkComponent> ent, ref ComponentInit args)
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
        Dirty(ent);
        UpdateRingerUi(ent, ent.Comp.Active);
    }

    private void UpdateRingerUi(Entity<RingerComponent> ent, bool isPlaying)
    {
        _ui.SetUiState(ent.Owner, RingerUiKey.Key, new RingerUpdateState(isPlaying, ent.Comp.Ringtone));
    }

    public bool ToggleRingerUi(EntityUid uid, EntityUid actor)
    {
        _ui.TryToggleUi(uid, RingerUiKey.Key, actor);
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
