using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.Utility;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    public class InstantSpell : IInstantAction
    {
        [ViewVariables] [DataField("castmessage")] public string CastMessage { get; set; } = "I CAST SPELL";
        [ViewVariables] [DataField("cooldown")] public float CoolDown { get; set; } = 1f;
        [ViewVariables] [DataField("spellitem")] public string ItemProto { get; set; } = "FoodPieBananaCream";

        [ViewVariables] [DataField("castsound")] public string? _castsound { get; set; } = "Audio/Weapons/emitter.ogg";

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
            caster.GetComponent<HandsComponent>().PutInHandOrDrop(spawnedProto.GetComponent<ItemComponent>());
            if (_castsound != null)
            {
                SoundSystem.Play(Filter.Pvs(caster), _castsound, caster);
            }
            else return;
        }
    }
}
