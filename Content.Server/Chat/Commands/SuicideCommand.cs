using System.Linq;
using Content.Server.Act;
using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Notification;
using Content.Server.Players;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Notification;
using Content.Shared.Notification.Managers;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server.Chat.Commands
{
    [AnyCommand]
    internal class SuicideCommand : IConsoleCommand
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public string Command => "suicide";

        public string Description => Loc.GetString("suicide-command-description");

        public string Help => Loc.GetString("suicide-command-help-text");

        private void DealDamage(ISuicideAct suicide, IChatManager chat, IDamageableComponent damageableComponent, IEntity source, IEntity target)
        {
            var kind = suicide.Suicide(target, chat);
            if (kind != SuicideKind.Special)
            {
                damageableComponent.TrySetDamage(kind switch
                    {
                        SuicideKind.Blunt => _prototypeManager.Index<DamageTypePrototype>("Blunt"),
                        SuicideKind.Slash => _prototypeManager.Index<DamageTypePrototype>("Slash"),
                        SuicideKind.Piercing => _prototypeManager.Index<DamageTypePrototype>("Piercing"),
                        SuicideKind.Heat => _prototypeManager.Index<DamageTypePrototype>("Heat"),
                        SuicideKind.Shock => _prototypeManager.Index<DamageTypePrototype>("Shock"),
                        SuicideKind.Cold => _prototypeManager.Index<DamageTypePrototype>("Cold"),
                        SuicideKind.Poison => _prototypeManager.Index<DamageTypePrototype>("Poison"),
                        SuicideKind.Radiation => _prototypeManager.Index<DamageTypePrototype>("Radiation"),
                        SuicideKind.Asphyxiation => _prototypeManager.Index<DamageTypePrototype>("Asphyxiation"),
                        SuicideKind.Bloodloss => _prototypeManager.Index<DamageTypePrototype>("Bloodloss"),
                        _ => _prototypeManager.Index<DamageTypePrototype>("Blunt")
                        // TODO QUESTION/SUGGESTION we should probably just remove SuicideKind altogether? Just make
                        // each function that implements SuicideAct directly return a damage type (or null for
                        // "Special"?) Adding yaml datafields is uneccesary, as currently it requires hardcoding a
                        // suicide function, with suicide messages anyway.
                    },
                200, source);
            }
        }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            if (player.Status != SessionStatus.InGame)
                return;

            var chat = IoCManager.Resolve<IChatManager>();
            var mind = player.ContentData()?.Mind;
            var owner = mind?.OwnedComponent?.Owner;

            // This check also proves mind not-null for at the end when the mob is ghosted.
            if (owner == null)
            {
                shell.WriteLine("You don't have a mind!");
                return;
            }

            var dmgComponent = owner.GetComponent<IDamageableComponent>();
            //TODO: needs to check if the mob is actually alive
            //TODO: maybe set a suicided flag to prevent resurrection?

            // Held item suicide
            var handsComponent = owner.GetComponent<HandsComponent>();
            var itemComponent = handsComponent.GetActiveHand;
            if (itemComponent != null)
            {
                var suicide = itemComponent.Owner.GetAllComponents<ISuicideAct>().FirstOrDefault();

                if (suicide != null)
                {
                    DealDamage(suicide, chat, dmgComponent, itemComponent.Owner, owner);
                    return;
                }
            }
            // Get all entities in range of the suicider
            var entities = IoCManager.Resolve<IEntityLookup>().GetEntitiesInRange(owner, 1, true).ToArray();

            if (entities.Length > 0)
            {
                foreach (var entity in entities)
                {
                    if (entity.HasComponent<ItemComponent>())
                        continue;
                    var suicide = entity.GetAllComponents<ISuicideAct>().FirstOrDefault();
                    if (suicide != null)
                    {
                        DealDamage(suicide, chat, dmgComponent, entity, owner);
                        return;
                    }
                }
            }

            // Default suicide, bite your tongue
            var othersMessage = Loc.GetString("suicide-command-default-text-others",("name", owner));
            owner.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("suicide-command-default-text-self");
            owner.PopupMessage(selfMessage);

            dmgComponent.TrySetDamage(_prototypeManager.Index<DamageTypePrototype>("Piercing"), 200, owner);

            // Prevent the player from returning to the body.
            // Note that mind cannot be null because otherwise owner would be null.
            EntitySystem.Get<GameTicker>().OnGhostAttempt(mind!, false);
        }
    }
}
