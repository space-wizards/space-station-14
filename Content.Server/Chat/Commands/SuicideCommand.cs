using System.Linq;
using Content.Server.Act;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Hands.Components;
using Content.Server.Players;
using Content.Server.Popups;
using Content.Shared.Administration;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Database;
using Content.Shared.Item;
using Content.Shared.Popups;
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
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "suicide";

        public string Description => Loc.GetString("suicide-command-description");

        public string Help => Loc.GetString("suicide-command-help-text");

        private void DealDamage(ISuicideAct suicide, IChatManager chat, EntityUid target)
        {
            var kind = suicide.Suicide(target, chat);
            if (kind != SuicideKind.Special)
            {
                // TODO SUICIDE ..heh.. anyway, someone should fix this mess.
                var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
                DamageSpecifier damage = new(kind switch
                    {
                        SuicideKind.Blunt => prototypeManager.Index<DamageTypePrototype>("Blunt"),
                        SuicideKind.Slash => prototypeManager.Index<DamageTypePrototype>("Slash"),
                        SuicideKind.Piercing => prototypeManager.Index<DamageTypePrototype>("Piercing"),
                        SuicideKind.Heat => prototypeManager.Index<DamageTypePrototype>("Heat"),
                        SuicideKind.Shock => prototypeManager.Index<DamageTypePrototype>("Shock"),
                        SuicideKind.Cold => prototypeManager.Index<DamageTypePrototype>("Cold"),
                        SuicideKind.Poison => prototypeManager.Index<DamageTypePrototype>("Poison"),
                        SuicideKind.Radiation => prototypeManager.Index<DamageTypePrototype>("Radiation"),
                        SuicideKind.Asphyxiation => prototypeManager.Index<DamageTypePrototype>("Asphyxiation"),
                        SuicideKind.Bloodloss => prototypeManager.Index<DamageTypePrototype>("Bloodloss"),
                        _ => prototypeManager.Index<DamageTypePrototype>("Blunt")
                    },
                200);
                EntitySystem.Get<DamageableSystem>().TryChangeDamage(target, damage, true);
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

            if (player.Status != SessionStatus.InGame || player.AttachedEntity == null)
                return;

            var chat = IoCManager.Resolve<IChatManager>();
            var mind = player.ContentData()?.Mind;

            // This check also proves mind not-null for at the end when the mob is ghosted.
            if (mind?.OwnedComponent?.Owner is not {Valid: true} owner)
            {
                shell.WriteLine("You don't have a mind!");
                return;
            }

            //TODO: needs to check if the mob is actually alive
            //TODO: maybe set a suicided flag to prevent resurrection?

            EntitySystem.Get<AdminLogSystem>().Add(LogType.Suicide,
                $"{_entities.ToPrettyString(player.AttachedEntity.Value):player} is committing suicide");

            // Held item suicide
            var handsComponent = _entities.GetComponent<HandsComponent>(owner);
            var itemComponent = handsComponent.GetActiveHandItem;
            if (itemComponent != null)
            {
                var suicide = _entities.GetComponents<ISuicideAct>(itemComponent.Owner).FirstOrDefault();

                if (suicide != null)
                {
                    DealDamage(suicide, chat, owner);
                    return;
                }
            }
            // Get all entities in range of the suicider
            var entities = IoCManager.Resolve<IEntityLookup>().GetEntitiesInRange(owner, 1, LookupFlags.Approximate | LookupFlags.IncludeAnchored).ToArray();

            if (entities.Length > 0)
            {
                foreach (var entity in entities)
                {
                    if (_entities.HasComponent<SharedItemComponent>(entity))
                        continue;
                    var suicide = _entities.GetComponents<ISuicideAct>(entity).FirstOrDefault();
                    if (suicide != null)
                    {
                        DealDamage(suicide, chat, owner);
                        return;
                    }
                }
            }

            // Default suicide, bite your tongue
            var othersMessage = Loc.GetString("suicide-command-default-text-others",("name", owner));
            owner.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("suicide-command-default-text-self");
            owner.PopupMessage(selfMessage);

            DamageSpecifier damage = new(IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>("Bloodloss"), 200);
            EntitySystem.Get<DamageableSystem>().TryChangeDamage(owner, damage, true);

            // Prevent the player from returning to the body.
            // Note that mind cannot be null because otherwise owner would be null.
            EntitySystem.Get<GameTicker>().OnGhostAttempt(mind!, false);
        }
    }
}
