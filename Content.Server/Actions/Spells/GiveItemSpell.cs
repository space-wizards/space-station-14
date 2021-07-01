using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Notification;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions.Behaviors;
using Content.Shared.Actions.Components;
using Content.Shared.Cooldown;
using Content.Shared.Notification.Managers;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Actions.Spells
{
    [UsedImplicitly]
    [DataDefinition]
    public class GiveItemSpell : IInstantAction
    {
        [ViewVariables] [DataField("castMessage")] public string? CastMessage { get; set; } = default!;
        [ViewVariables] [DataField("cooldown")] public float CoolDown { get; set; } = 1f;
        [ViewVariables] [DataField("spellItem")] public string ItemProto { get; set; } = default!;

        [ViewVariables] [DataField("castSound")] public string? CastSound { get; set; } = default!;

        //Rubber-band snapping items into player's hands, originally was a workaround, later found it works quite well with stuns
        //Not sure if needs fixing

        public void DoInstantAction(InstantActionEventArgs args)
        {
            if (!args.Performer.HasComponent<SharedActionsComponent>()) return;
            var caster = args.Performer;
            var casterCoords = caster.Transform.MapPosition;
            //Checks if caster can perform the action
            if (!caster.HasComponent<HandsComponent>())
            {
                args.Performer.PopupMessage(Loc.GetString("spell-fail-no-hands"));
                return;
            }
            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(caster)) return;
            //Perform the action
            args.PerformerActions?.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(CoolDown));
            //Spawn the item once it's validated
            var spawnedProto = caster.EntityManager.SpawnEntity(ItemProto, casterCoords); 
            if (CastMessage != null) caster.PopupMessageEveryone(CastMessage);
            caster.GetComponent<HandsComponent>().PutInHandOrDrop(spawnedProto.GetComponent<ItemComponent>(), true);
            if (CastSound != null)
            {
                SoundSystem.Play(Filter.Pvs(caster), CastSound, caster);
            }
        }
    } 
}
