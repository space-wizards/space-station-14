using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Implants.Components;
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
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<VoiceSexModificationControllerComponent> entity, ref ComponentShutdown args)
    {
        _actions.RemoveAction(entity.Owner, entity.Comp.ActionEntity);
    }

    [SubscribeLocalEvent]
    private void OpenUI(VoiceSexModificationControllerToggleMenuEvent ev)
    {
        var source = ev.Action.Comp.Container;

        if (source == null)
            return;

        if (!_uiSystem.HasUi(source.Value, VoiceSexModificationControllerKey.Key))
            return;

        // Target may not be the same as the source
        EntityUid? targetEntity = null;
        if (HasComp<VoiceSexModificationControllerImplantComponent>(source))
        {
            if (!TryComp<SubdermalImplantComponent>(source, out var implantComp) || implantComp.ImplantedEntity == null)
                return;

            targetEntity = implantComp.ImplantedEntity;
        }
        else if (HasComp<VoiceSexModificationControllerComponent>(source))
        {
            targetEntity = source;
        }

        if (targetEntity == null)
            return;

        if (!TryComp<HumanoidProfileComponent>(targetEntity, out var humanoidProfileComp))
        {
            // Maybe one day we'll have a situation where you want to run this for an entity without HumanoidProfileComponent.
            _popup.PopupEntity(Loc.GetString("voice-sex-modification-not-humanoid"), targetEntity.Value, targetEntity.Value);
            return;
        }

        var species = humanoidProfileComp.Species;
        var currentSex = humanoidProfileComp.Sex;
        var currentVoice = humanoidProfileComp.Voice;

        if (ProtoMan.Resolve(species, out var speciesProto) && (speciesProto.Sexes.Count <= 1 && speciesProto.Voices.Count <= 1))
        {
            _popup.PopupEntity(Loc.GetString("voice-sex-modification-incompatible-species"), targetEntity.Value, targetEntity.Value);
            return;
        }

        _uiSystem.SetUiState(source.Value, VoiceSexModificationControllerKey.Key, new VoiceSexModificationControllerBuiState() { Species = species, CurrentSex = currentSex, CurrentVoice = currentVoice });
        _uiSystem.TryToggleUi(source.Value, VoiceSexModificationControllerKey.Key, ev.Performer);
    }

    [SubscribeLocalEvent]
    private void OnVoiceSexModificationImplantMessage(Entity<VoiceSexModificationControllerImplantComponent> ent, ref VoiceSexModificationMessage args)
    {
        if (!TryComp<SubdermalImplantComponent>(ent, out var implantComp) || implantComp.ImplantedEntity == null || !TryComp<HumanoidProfileComponent>(implantComp.ImplantedEntity, out var humanoidProfileComp))
            return;

        if (TryApplyChanges((implantComp.ImplantedEntity.Value, humanoidProfileComp), args.Voice, args.Sex))
        {
            _audio.PlayPredicted(ent.Comp.Sound, implantComp.ImplantedEntity.Value, implantComp.ImplantedEntity.Value);
            if (ent.Comp.DeleteOnUse)
                QueueDel(ent);
        }
    }

    [SubscribeLocalEvent]
    private void OnVoiceSexModificationEntityMessage(Entity<VoiceSexModificationControllerComponent> ent, ref VoiceSexModificationMessage args)
    {
        if (!TryComp<HumanoidProfileComponent>(ent, out var humanoidProfileComp))
            return;

        if (TryApplyChanges((ent, humanoidProfileComp), args.Voice, args.Sex))
        {
            _audio.PlayPredicted(ent.Comp.Sound, ent, ent);
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
