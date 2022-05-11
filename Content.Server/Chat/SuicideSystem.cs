using Content.Server.Act;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Hands.Components;
using Content.Server.Players;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Chat
{
    public sealed class SuicideSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
        [Dependency] private readonly AdminLogSystem _adminLogSystem = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public void Suicide(IConsoleShell shell)
        {
            //TODO: Make this work without the console shell

            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            if (player.Status != SessionStatus.InGame || player.AttachedEntity == null)
                return;
            var mind = player.ContentData()?.Mind;

            // This check also proves mind not-null for at the end when the mob is ghosted.
            if (mind?.OwnedComponent?.Owner is not { Valid: true } owner)
            {
                shell.WriteLine("You don't have a mind!");
                return;
            }

            //Checks to see if the player is dead.
            if (EntityManager.TryGetComponent<MobStateComponent>(owner, out var mobState) && mobState.IsDead())
            {
                shell.WriteLine(Loc.GetString("suicide-command-already-dead"));
                return;
            }

            //Checks to see if the CannotSuicide tag exits, ghosts instead.
            if (_tagSystem.HasTag(owner, "CannotSuicide"))
            {
                if (!_gameTicker.OnGhostAttempt(mind, true))
                {
                    shell?.WriteLine("You can't ghost right now.");
                    return;
                }
                return;
            }

            //TODO: needs to check if the mob is actually alive
            //TODO: maybe set a suicided flag to prevent resurrection?

            _adminLogSystem.Add(LogType.Suicide,
                $"{EntityManager.ToPrettyString(player.AttachedEntity.Value):player} is committing suicide");

            var suicideEvent = new SuicideEvent(owner);
            // Held item suicide
            if (EntityManager.TryGetComponent(owner, out HandsComponent handsComponent)
                && handsComponent.ActiveHandEntity is EntityUid item)
            {
                RaiseLocalEvent(item, suicideEvent, false);

                if (suicideEvent.Handled)
                {
                    ApplyDeath(owner, suicideEvent.Kind!.Value);
                    return;
                }
            }

            // Get all entities in range of the suicider
            var entities = _entityLookupSystem.GetEntitiesInRange(owner, 1, LookupFlags.Approximate | LookupFlags.Anchored).ToArray();

            if (entities.Length > 0)
            {
                foreach (var entity in entities)
                {
                    if (EntityManager.HasComponent<SharedItemComponent>(entity))
                        continue;
                    RaiseLocalEvent(entity, suicideEvent, false);

                    if (suicideEvent.Handled)
                    {
                        ApplyDeath(owner, suicideEvent.Kind!.Value);
                        return;
                    }
                }
            }

            // Default suicide, bite your tongue
            var othersMessage = Loc.GetString("suicide-command-default-text-others", ("name", owner));
            owner.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("suicide-command-default-text-self");
            owner.PopupMessage(selfMessage);

            ApplyDeath(owner, SuicideKind.Bloodloss);

            // Prevent the player from returning to the body.
            // Note that mind cannot be null because otherwise owner would be null.
            _gameTicker.OnGhostAttempt(mind!, false);
        }

        private void ApplyDeath(EntityUid target, SuicideKind kind)
        {
            if (kind == SuicideKind.Special) return;
            // TODO SUICIDE ..heh.. anyway, someone should fix this mess.
            DamageSpecifier damage = new(kind switch
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
            },
                200);

            _damageableSystem.TryChangeDamage(target, damage, true);
        }
    }
}
