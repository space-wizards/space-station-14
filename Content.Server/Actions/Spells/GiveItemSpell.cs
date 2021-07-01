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
    {   //TODO: Needs to be an EntityPrototype for proper validation
        [ViewVariables] [DataField("castMessage")] public string? CastMessage { get; set; } = default!;
        [ViewVariables] [DataField("cooldown")] public float CoolDown { get; set; } = 1f;
        [ViewVariables] [DataField("spellItem")] public string ItemProto { get; set; } = default!;

        [ViewVariables] [DataField("castSound")] public string? CastSound { get; set; } = default!;

        //Rubber-band snapping items into player's hands, originally was a workaround, later found it works quite well with stuns
        //Not sure if needs fixing

        public void DoInstantAction(InstantActionEventArgs args)
        {
            //Checks if caster can perform the action
            if (!args.Performer.HasComponent<HandsComponent>())
            {
                args.Performer.PopupMessage(Loc.GetString("spell-fail-no-hands"));
                return;
            }
            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(args.Performer)) return;
            //UPON COMPLETING VALIDATION execute code related to the spell
            args.PerformerActions?.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(CoolDown));
            var caster = args.Performer; //From now on we'll reffer to args.Performer as caster to make it easier to read
            var casterCoords = caster.Transform.MapPosition;
            var spawnedProto = caster.EntityManager.SpawnEntity(ItemProto, casterCoords); 
            if (CastMessage != null) caster.PopupMessageEveryone(CastMessage);
            //Re-check that caster still has hands to hold the item
            if (caster.TryGetComponent<HandsComponent>(out var handscomp)) 
            {
               handscomp.PutInHandOrDrop(spawnedProto.GetComponent<ItemComponent>(), true);
            }
            if (CastSound != null) SoundSystem.Play(Filter.Pvs(caster), CastSound, caster);
        }
    } 
}
