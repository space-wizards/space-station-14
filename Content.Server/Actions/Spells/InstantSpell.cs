using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.Utility;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
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
        [ViewVariables] [DataField("spellitem")] public string ItemProto { get; set; } = "FoodCreamPie";

        [ViewVariables] [DataField("castSound")] public string CastSound { get; set; } = "/Audio/Effects/Fluids/slosh.ogg";

        //TODO: this semi-teleportation rubber-snap method of an item on the ground teleporting into the player's hands is utter ass,
        //I urge you to make a function instantly giving an item instead, and updating this shitcode if possible. Many thanks!

        public void DoInstantAction(InstantActionEventArgs args)
        {
            if (!args.Performer.TryGetComponent<SharedActionsComponent>(out var actions)) return;
            var caster = args.Performer;
            var casterCoords = caster.Transform.Coordinates;
            var spawnedProto = caster.EntityManager.SpawnEntity(ItemProto, casterCoords);
            caster.PopupMessageEveryone(CastMessage);
            args.PerformerActions?.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(CoolDown));
            caster.GetComponent<HandsComponent>().PutInHandOrDrop(spawnedProto.GetComponent<ItemComponent>());
        }
    }
}
