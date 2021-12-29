using Content.Server.Hands.Components;
using Content.Server.Popups;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions.Behaviors;
using Content.Shared.Cooldown;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Actions.Spells
{
    [UsedImplicitly]
    [DataDefinition]
    public class GiveItemSpell : IInstantAction
    {   //TODO: Needs to be an EntityPrototype for proper validation
        [ViewVariables] [DataField("castMessage")] public string? CastMessage { get; set; } = default!;
        [ViewVariables] [DataField("cooldown")] public float CoolDown { get; set; } = 1f;
        [ViewVariables] [DataField("spellItem", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))] public string ItemProto { get; set; } = default!;

        [ViewVariables] [DataField("castSound", required: true)] public SoundSpecifier CastSound { get; set; } = default!;

        //Rubber-band snapping items into player's hands, originally was a workaround, later found it works quite well with stuns
        //Not sure if needs fixing

        public void DoInstantAction(InstantActionEventArgs args)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();

            var caster = args.Performer;

            if (!entMan.TryGetComponent(caster, out HandsComponent? handsComponent))
            {
                caster.PopupMessage(Loc.GetString("spell-fail-no-hands"));
                return;
            }

            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(caster)) return;

            // TODO: Nix when we get EntityPrototype serializers
            if (!IoCManager.Resolve<IPrototypeManager>().HasIndex<EntityPrototype>(ItemProto))
            {
                Logger.Error($"Invalid prototype {ItemProto} supplied for {nameof(GiveItemSpell)}");
                return;
            }

            // TODO: Look this is shitty and ideally a test would do it
            var spawnedProto = entMan.SpawnEntity(ItemProto, entMan.GetComponent<TransformComponent>(caster).MapPosition);

            if (!entMan.TryGetComponent(spawnedProto, out SharedItemComponent? itemComponent))
            {
                Logger.Error($"Tried to use {nameof(GiveItemSpell)} but prototype has no {nameof(SharedItemComponent)}?");
                entMan.DeleteEntity(spawnedProto);
                return;
            }

            args.PerformerActions?.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(CoolDown));

            if (CastMessage != null)
                caster.PopupMessageEveryone(CastMessage);

            handsComponent.PutInHandOrDrop(itemComponent);

            SoundSystem.Play(Filter.Pvs(caster), CastSound.GetSound(), caster);
        }
    }
}
