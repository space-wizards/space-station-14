using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Systems;

/// <summary>
/// System to let an entity change its voice/sex, using an options UI.
/// </summary>
public sealed partial class VoiceSexModificationControllerSystem : EntitySystem
{
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<VoiceSexModificationControllerComponent> entity, ref MapInitEvent args)
    {
        if (!TryComp(entity, out ActionsComponent? comp))
            return;

        _actions.AddAction(entity, ref entity.Comp.ActionEntity, entity.Comp.Action, component: comp);
        Dirty(entity);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<VoiceSexModificationControllerComponent> entity, ref ComponentShutdown args)
    {
        _actions.RemoveAction(entity.Owner, entity.Comp.ActionEntity);
        Dirty(entity);
    }

    [SubscribeLocalEvent]
    private void OpenUI(VoiceSexModificationControllerToggleMenuEvent ev)
    {
        var source = ev.Action.Comp.Container;

        if (source == null)
            return;

        if (!_uiSystem.HasUi(source.Value, VoiceSexModificationControllerKey.Key))
            return;

        if (!HasComp<VoiceSexModificationControllerComponent>(source))
            return;

        if (!TryComp<HumanoidProfileComponent>(source, out var humanoidProfileComp))
        {
            _popup.PopupEntity(Loc.GetString("voice-sex-modification-not-humanoid"), source.Value, source.Value);
            return;
        }

        var species = humanoidProfileComp.Species;
        var currentSex = humanoidProfileComp.Sex;
        var currentVoice = humanoidProfileComp.Voice;

        if (ProtoMan.Resolve(species, out var speciesProto) && (speciesProto.Sexes.Count <= 1 && speciesProto.Voices.Count <= 1))
        {
            _popup.PopupEntity(Loc.GetString("voice-sex-modification-incompatible-species"), source.Value, source.Value);
            return;
        }

        _uiSystem.SetUiState(source.Value, VoiceSexModificationControllerKey.Key, new VoiceSexModificationControllerBuiState() { Species = species, CurrentSex = currentSex, CurrentVoice = currentVoice });
        _uiSystem.TryToggleUi(source.Value, VoiceSexModificationControllerKey.Key, ev.Performer);
    }

    [SubscribeLocalEvent]
    private void OnVoiceSexModificationEntityMessage(Entity<VoiceSexModificationControllerComponent> ent, ref VoiceSexModificationMessage args)
    {
        if (!TryComp<HumanoidProfileComponent>(ent, out var humanoidProfileComp))
            return;

        if (ent.Comp.ActionEntity == null || TryComp<ActionComponent>(ent.Comp.ActionEntity, out var actionComp) && _actions.IsCooldownActive(actionComp))
            return;

        if (TryApplyChanges((ent, humanoidProfileComp), args.Voice, args.Sex))
        {
            _audio.PlayPredicted(ent.Comp.Sound, ent, ent);
            _actions.StartUseDelay((ent.Comp.ActionEntity.Value, actionComp));
        }
    }

    private bool TryApplyChanges(Entity<HumanoidProfileComponent> ent, ProtoId<EmoteSoundsPrototype>? voice, Sex sex)
    {
        var changeMade = false;
        if (voice != null && voice != ent.Comp.Voice)
        {
            _humanoidProfile.ApplyVoice(ent.AsNullable(), voice.Value);
            changeMade = true;
        }

        if (sex != ent.Comp.Sex)
        {
            _humanoidProfile.ApplySex(ent.AsNullable(), sex);
            changeMade = true;
        }

        return changeMade;
    }
}
