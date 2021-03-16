#nullable enable
using System;
using System.Linq;
using Content.Server.Administration;
using Content.Server.Commands.Observer;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Players;
using Content.Server.Utility;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.Interfaces;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Commands.Chat
{
    [AnyCommand]
    internal class SuicideCommand : IConsoleCommand
    {
        public string Command => "suicide";

        public string Description => "Commits suicide";

        public string Help => "The suicide command gives you a quick way out of a round while remaining in-character.\n" +
            "The method varies, first it will attempt to use the held item in your active hand.\n" +
            "If that fails, it will attempt to use an object in the environment.\n" +
            "Finally, if neither of the above worked, you will die by biting your tongue.";

        private void DealDamage(ISuicideAct suicide, IChatManager chat, IDamageableComponent damageableComponent, IEntity source, IEntity target)
        {
            var kind = suicide.Suicide(target, chat);
            if (kind != SuicideKind.Special)
            {
                damageableComponent.SetDamage(kind switch
                    {
                        SuicideKind.Blunt => DamageType.Blunt,
                        SuicideKind.Slash => DamageType.Slash,
                        SuicideKind.Piercing => DamageType.Piercing,
                        SuicideKind.Heat => DamageType.Heat,
                        SuicideKind.Shock => DamageType.Shock,
                        SuicideKind.Cold => DamageType.Cold,
                        SuicideKind.Poison => DamageType.Poison,
                        SuicideKind.Radiation => DamageType.Radiation,
                        SuicideKind.Asphyxiation => DamageType.Asphyxiation,
                        SuicideKind.Bloodloss => DamageType.Bloodloss,
                        _ => DamageType.Blunt
                    },
                200, source);
            }
        }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("You cannot run this command from the server.");
                return;
            }

            if (player.Status != SessionStatus.InGame)
                return;

            var chat = IoCManager.Resolve<IChatManager>();
            var owner = player.ContentData()?.Mind?.OwnedComponent.Owner;

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
            var entities = owner.EntityManager.GetEntitiesInRange(owner, 1, true).ToArray();

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
            var othersMessage = Loc.GetString("{0:theName} is attempting to bite {0:their} own tongue!", owner);
            owner.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("You attempt to bite your own tongue!");
            owner.PopupMessage(selfMessage);

            dmgComponent.SetDamage(DamageType.Piercing, 200, owner);

            // Prevent the player from returning to the body. Yes, this is an ugly hack.
            var ghost = new Ghost(){CanReturn = false};
            ghost.Execute(shell, argStr, Array.Empty<string>());
        }
    }
}
