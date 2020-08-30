using System;
using System.Linq;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Interfaces;
using Content.Server.Observer;
using Content.Server.Players;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.Interfaces;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Content.Shared.Damage;

namespace Content.Server.Chat
{
    internal class SayCommand : IClientCommand
    {
        public string Command => "say";
        public string Description => "Send chat messages to the local channel or a specified radio channel.";
        public string Help => "say <text>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player.Status != SessionStatus.InGame || !player.AttachedEntityUid.HasValue)
                return;

            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            var chat = IoCManager.Resolve<IChatManager>();

            if (player.AttachedEntity.HasComponent<GhostComponent>())
                chat.SendDeadChat(player, message);
            else
            {
                var mindComponent = player.ContentData().Mind;
                chat.EntitySay(mindComponent.OwnedEntity, message);
            }

        }
    }

    internal class MeCommand : IClientCommand
    {
        public string Command => "me";
        public string Description => "Perform an action.";
        public string Help => "me <text>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player.Status != SessionStatus.InGame || !player.AttachedEntityUid.HasValue)
                return;

            if (args.Length < 1)
                return;

            var action = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(action))
                return;

            var chat = IoCManager.Resolve<IChatManager>();

            var mindComponent = player.ContentData().Mind;
            chat.EntityMe(mindComponent.OwnedEntity, action);
        }
    }

    internal class OOCCommand : IClientCommand
    {
        public string Command => "ooc";
        public string Description => "Send Out Of Character chat messages.";
        public string Help => "ooc <text>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            var chat = IoCManager.Resolve<IChatManager>();
            chat.SendOOC(player, message);
        }
    }

    internal class AdminChatCommand : IClientCommand
    {
        public string Command => "asay";
        public string Description => "Send chat messages to the private admin chat channel.";
        public string Help => "asay <text>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            var chat = IoCManager.Resolve<IChatManager>();
            chat.SendAdminChat(player, message);
        }
    }

    internal class SuicideCommand : IClientCommand
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;

        public string Command => "suicide";

        public string Description => "Commits suicide";

        public string Help => "The suicide command gives you a quick way out of a round while remaining in-character.\n" +
            "The method varies, first it will attempt to use the held item in your active hand.\n" +
            "If that fails, it will attempt to use an object in the environment.\n" +
            "Finally, if neither of the above worked, you will die by biting your tongue.";

        private void DealDamage(ISuicideAct suicide, IChatManager chat, IDamageableComponent damageableComponent, IEntity source, IEntity target)
        {
            SuicideKind kind = suicide.Suicide(target, chat);
            if (kind != SuicideKind.Special)
            {
                damageableComponent.ChangeDamage(kind switch
                    {
                        SuicideKind.Blunt => DamageType.Blunt,
                        SuicideKind.Piercing => DamageType.Piercing,
                        SuicideKind.Heat => DamageType.Heat,
                        SuicideKind.Disintegration => DamageType.Disintegration,
                        SuicideKind.Cellular => DamageType.Cellular,
                        SuicideKind.DNA => DamageType.DNA,
                        SuicideKind.Asphyxiation => DamageType.Asphyxiation,
                        _ => DamageType.Blunt
                    },
                500,
                true, source);
            }
        }

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player.Status != SessionStatus.InGame)
                return;

            var chat = IoCManager.Resolve<IChatManager>();
            var owner = player.ContentData().Mind.OwnedMob.Owner;
            var dmgComponent = owner.GetComponent<IDamageableComponent>();
            //TODO: needs to check if the mob is actually alive
            //TODO: maybe set a suicided flag to prevent ressurection?

            // Held item suicide
            var handsComponent = owner.GetComponent<HandsComponent>();
            var itemComponent = handsComponent.GetActiveHand;
            if (itemComponent != null)
            {
                ISuicideAct suicide = itemComponent.Owner.GetAllComponents<ISuicideAct>().FirstOrDefault();
                if (suicide != null)
                {
                    DealDamage(suicide, chat, dmgComponent, itemComponent.Owner, owner);
                    return;
                }
            }
            // Get all entities in range of the suicider
            var entities = owner.EntityManager.GetEntitiesInRange(owner, 1, true);
            if (entities.Count() > 0)
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
            PopupMessageOtherClientsInRange(owner, Loc.GetString("{0:theName} is attempting to bite {0:their} own tongue!", owner), 15);
            _notifyManager.PopupMessage(owner, owner, Loc.GetString("You attempt to bite your own tongue!"));
            dmgComponent.ChangeDamage(DamageType.Piercing, 500, true, owner);

            // Prevent the player from returning to the body. Yes, this is an ugly hack.
            var ghost = new Ghost(){CanReturn = false};
            ghost.Execute(shell, player, Array.Empty<string>());
        }
        private void PopupMessageOtherClientsInRange(IEntity source, string message, int maxReceiveDistance)
        {
            var viewers = _playerManager.GetPlayersInRange(source.Transform.GridPosition, maxReceiveDistance);

            foreach (var viewer in viewers)
            {
                var viewerEntity = viewer.AttachedEntity;

                if (viewerEntity == null || source == viewerEntity)
                {
                    continue;
                }

                source.PopupMessage(viewer.AttachedEntity, message);
            }
        }
    }
}
