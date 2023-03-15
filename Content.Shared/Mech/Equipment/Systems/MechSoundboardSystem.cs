using Content.Shared.Mech;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Mech.Equipment.Systems;
using Content.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using System.Linq;

namespace Content.Shared.Mech.Equipment.Systems;

/// <summary>
/// Handles everything for mech soundboard.
/// </summary>
public sealed class SharedMechSoundboardSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechSoundboardComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<MechSoundboardComponent, ComponentHandleState>(OnHandleState);

        SubscribeLocalEvent<MechSoundboardComponent, MechEquipmentUiStateReadyEvent>(OnUiStateReady);
        SubscribeLocalEvent<MechSoundboardComponent, MechEquipmentUiMessageRelayEvent>(OnSoundboardMessage);
    }

    private void OnGetState(EntityUid uid, MechSoundboardComponent comp, ref ComponentGetState args)
    {
        args.State = new MechSoundboardComponentState(comp.Sounds);
    }

    private void OnHandleState(EntityUid uid, MechSoundboardComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not MechSoundboardComponentState state)
            return;

        comp.Sounds = state.Sounds;
    }

    private void OnUiStateReady(EntityUid uid, MechSoundboardComponent comp, MechEquipmentUiStateReadyEvent args)
    {
        var sounds = comp.Sounds.Select(sound => Loc.GetString($"mech-soundboard-{sound.Collection}"));
        var state = new MechSoundboardUiState
        {
            Sounds = sounds.ToList()
        };
        args.States.Add(uid, state);
    }

    private void OnSoundboardMessage(EntityUid uid, MechSoundboardComponent comp, MechEquipmentUiMessageRelayEvent args)
    {
        if (args.Message is not MechSoundboardPlayMessage msg)
            return;

        if (!TryComp<MechEquipmentComponent>(uid, out var equipment) ||
            equipment.EquipmentOwner == null)
            return;

        if (msg.Sound >= comp.Sounds.Count)
            return;

        if (_useDelay.ActiveDelay(uid))
            return;

        // TODO: add usedelay to honk
        // honk!!!!!
        var mech = equipment.EquipmentOwner.Value;
        _useDelay.BeginDelay(uid);
        _audio.PlayPvs(comp.Sounds[msg.Sound], uid);
    }
}
