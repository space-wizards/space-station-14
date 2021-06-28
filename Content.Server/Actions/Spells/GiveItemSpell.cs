using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Notification;
using Content.Shared.Actions.Behaviors;
using Content.Shared.Actions.Components;
using Content.Shared.Cooldown;
using Content.Shared.Notification.Managers;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    public class GiveItemSpell : IInstantAction
    {
        [ViewVariables] [DataField("castMessage")] public string CastMessage { get; set; } = "I CAST SPELL";
        [ViewVariables] [DataField("coolDown")] public float CoolDown { get; set; } = 1f;
        [ViewVariables] [DataField("spellItem")] public string ItemProto { get; set; } = default!;

        [ViewVariables] [DataField("castSound")] public string? CastSound { get; set; } = default!;

        //Rubber-band snapping items into player's hands, originally was a workaround, later found it works quite well with stuns
        //Not sure if needs fixing

        public void DoInstantAction(InstantActionEventArgs args)
        {
            if (!args.Performer.TryGetComponent<SharedActionsComponent>(out var actions)) return;
            var caster = args.Performer;
            var casterCoords = caster.Transform.Coordinates;
            var spawnedProto = caster.EntityManager.SpawnEntity(ItemProto, casterCoords);
            caster.PopupMessageEveryone(CastMessage);
            args.PerformerActions?.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(CoolDown));
            if (!caster.TryGetComponent<HandsComponent>(out var hands))
            {
                caster.PopupMessage("You don't have hands!");
                return;
            }
            caster.GetComponent<HandsComponent>().PutInHandOrDrop(spawnedProto.GetComponent<ItemComponent>());
            if (CastSound != null)
            {
                SoundSystem.Play(Filter.Pvs(caster), CastSound, caster);
            }
            else return;
        }
    }
}
