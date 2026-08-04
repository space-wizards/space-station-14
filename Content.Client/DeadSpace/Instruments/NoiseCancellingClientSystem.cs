// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Client.Audio;
using Content.Client.Instruments;
using Content.Shared.DeadSpace.Instruments;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Shared.Audio.Components;

namespace Content.Client.DeadSpace.Instruments;

public sealed class NoiseCancellingClientSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly InstrumentSystem _instrumentSystem = default!;

    private const float MutedMultiplier = 0.5f;
    private const byte MidiVolumeBoost = 110;
    private const float GainTolerance = 0.001f;

    private bool _isActive;
    private static readonly string[] HeadphonesSlots = ["ears", "head", "neck"];
    private readonly Dictionary<EntityUid, AppliedAudioGain> _applied = [];

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(AudioSystem));
        SubscribeLocalEvent<AudioComponent, ComponentInit>(OnAudioInit, before: [typeof(AudioSystem)]);
        SubscribeLocalEvent<HeadphonesInstrumentComponent, GotUnequippedEvent>(OnHeadphonesUnequipped);
    }

    private void OnHeadphonesUnequipped(EntityUid uid, HeadphonesInstrumentComponent comp, ref GotUnequippedEvent args)
    {
        var local = _playerManager.LocalEntity;
        if (local == null || args.Equipee != local.Value)
            return;

        if (TryComp<InstrumentComponent>(uid, out var instr))
            _instrumentSystem.EndRenderer(uid, false, instr);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var localPlayer = _playerManager.LocalPlayer?.ControlledEntity;
        var shouldBeActive = localPlayer != null && HasActiveHeadphones(localPlayer.Value);

        if (shouldBeActive != _isActive)
        {
            _isActive = shouldBeActive;

            if (!_isActive)
            {
                RestoreAudioGains();

                if (localPlayer != null)
                {
                    foreach (var headphones in GetEquippedHeadphones(localPlayer.Value))
                        RemoveMidiBoost(headphones);
                }
            }
        }

        if (!_isActive)
            return;

        if (localPlayer != null)
        {
            foreach (var headphones in GetEquippedHeadphones(localPlayer.Value))
            {
                if (IsHeadphonesActive(headphones))
                    ApplyMidiBoost(headphones);
                else
                    RemoveMidiBoost(headphones);
            }
        }

        var query = EntityQueryEnumerator<AudioComponent>();
        while (query.MoveNext(out var uid, out var audioComp))
        {
            TryApplyReduction(uid, audioComp);
        }

        if (_applied.Count > 0)
        {
            var toRemove = new List<EntityUid>();
            foreach (var uid in _applied.Keys)
            {
                if (TerminatingOrDeleted(uid))
                    toRemove.Add(uid);
            }
            foreach (var uid in toRemove)
                _applied.Remove(uid);
        }
    }

    private void OnAudioInit(EntityUid uid, AudioComponent audioComp, ComponentInit args)
    {
        var initializeEvent = new BeforeAudioSourceInitializeEvent();
        RaiseLocalEvent(uid, ref initializeEvent);

        if (!_isActive)
            return;

        TryApplyReduction(uid, audioComp);
    }

    private void TryApplyReduction(EntityUid uid, AudioComponent audioComp)
    {
        var currentGain = audioComp.Gain;

        if (_applied.TryGetValue(uid, out var applied) &&
            MathF.Abs(currentGain - applied.ReducedGain) < GainTolerance)
        {
            return;
        }

        var reduced = currentGain * MutedMultiplier;
        audioComp.Gain = reduced;
        _applied[uid] = new AppliedAudioGain(currentGain, reduced);
    }

    private void RestoreAudioGains()
    {
        foreach (var (uid, applied) in _applied)
        {
            if (!TryComp<AudioComponent>(uid, out var audioComp))
                continue;

            if (MathF.Abs(audioComp.Gain - applied.ReducedGain) < GainTolerance)
                audioComp.Gain = applied.OriginalGain;
        }

        _applied.Clear();
    }

    private bool HasActiveHeadphones(EntityUid wearer)
    {
        foreach (var _ in GetActiveHeadphones(wearer))
            return true;

        return false;
    }

    private IEnumerable<EntityUid> GetActiveHeadphones(EntityUid wearer)
    {
        foreach (var headphones in GetEquippedHeadphones(wearer))
        {
            if (IsHeadphonesActive(headphones))
                yield return headphones;
        }
    }

    private bool IsHeadphonesActive(EntityUid headphones)
    {
        return TryComp<ItemToggleComponent>(headphones, out var toggle) && toggle.Activated;
    }

    private IEnumerable<EntityUid> GetEquippedHeadphones(EntityUid wearer)
    {
        foreach (var slot in HeadphonesSlots)
        {
            if (_inventory.TryGetSlotEntity(wearer, slot, out var headphones) &&
                HasComp<HeadphonesInstrumentComponent>(headphones.Value))
            {
                yield return headphones.Value;
            }
        }
    }

    private void ApplyMidiBoost(EntityUid headphones)
    {
        if (!TryComp<InstrumentComponent>(headphones, out var instr) || instr.Renderer == null)
            return;

        instr.Renderer.MinVolume = MidiVolumeBoost;
    }

    private void RemoveMidiBoost(EntityUid headphones)
    {
        if (!TryComp<InstrumentComponent>(headphones, out var instr) || instr.Renderer == null)
            return;

        instr.Renderer.MinVolume = 0;
    }

    private readonly record struct AppliedAudioGain(float OriginalGain, float ReducedGain);
}
