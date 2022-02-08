using System;
using System.Linq;
using Content.Server.Act;
using Content.Server.Administration.Logs;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Weapon.Melee;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Behaviors;
using Content.Shared.Actions.Components;
using Content.Shared.Audio;
using Content.Shared.Cooldown;
using Content.Shared.Database;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Popups;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Actions.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    public class DisarmAction : ITargetEntityAction
    {
        [DataField("failProb")] private float _failProb = 0.4f;
        [DataField("pushProb")] private float _pushProb = 0.4f;
        [DataField("cooldown")] private float _cooldown = 1.5f;

        [ViewVariables]
        [DataField("punchMissSound")]
        private SoundSpecifier PunchMissSound { get; } = new SoundPathSpecifier("/Audio/Weapons/punchmiss.ogg");

        [ViewVariables]
        [DataField("disarmSuccessSound")]
        private SoundSpecifier DisarmSuccessSound { get; } = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

        public void DoTargetEntityAction(TargetEntityActionEventArgs args)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var disarmedActs = entMan.GetComponents<IDisarmedAct>(args.Target).ToArray();

            if (!args.Performer.InRangeUnobstructed(args.Target)) return;

            if (disarmedActs.Length == 0)
            {
                if (entMan.TryGetComponent(args.Performer, out ActorComponent? actor))
                {
                    // Fall back to a normal interaction with the entity
                    var player = actor.PlayerSession;
                    var coordinates = entMan.GetComponent<TransformComponent>(args.Target).Coordinates;
                    var target = args.Target;
                    EntitySystem.Get<InteractionSystem>().HandleUseInteraction(player, coordinates, target);
                    return;
                }

                return;
            }

            if (!entMan.TryGetComponent<SharedActionsComponent?>(args.Performer, out var actions)) return;
            if (args.Target == args.Performer || !EntitySystem.Get<ActionBlockerSystem>().CanAttack(args.Performer)) return;

            var random = IoCManager.Resolve<IRobustRandom>();
            var system = EntitySystem.Get<MeleeWeaponSystem>();

            var diff = entMan.GetComponent<TransformComponent>(args.Target).MapPosition.Position - entMan.GetComponent<TransformComponent>(args.Performer).MapPosition.Position;
            var angle = Angle.FromWorldVec(diff);

            actions.Cooldown(ActionType.Disarm, Cooldowns.SecondsFromNow(_cooldown));

            if (random.Prob(_failProb))
            {
                SoundSystem.Play(Filter.Pvs(args.Performer), PunchMissSound.GetSound(), args.Performer, AudioHelpers.WithVariation(0.025f));

                args.Performer.PopupMessageOtherClients(Loc.GetString("disarm-action-popup-message-other-clients",
                                                                      ("performerName", entMan.GetComponent<MetaDataComponent>(args.Performer).EntityName),
                                                                      ("targetName", entMan.GetComponent<MetaDataComponent>(args.Target).EntityName)));
                args.Performer.PopupMessageCursor(Loc.GetString("disarm-action-popup-message-cursor",
                                                                ("targetName", entMan.GetComponent<MetaDataComponent>(args.Target).EntityName)));
                system.SendLunge(angle, args.Performer);
                return;
            }

            system.SendAnimation("disarm", angle, args.Performer, args.Performer, new[] { args.Target });

            var eventArgs = new DisarmedActEvent() { Target = args.Target, Source = args.Performer, PushProbability = _pushProb };

            entMan.EventBus.RaiseLocalEvent(args.Target, eventArgs);

            EntitySystem.Get<AdminLogSystem>().Add(LogType.DisarmedAction, LogImpact.Low, $"{entMan.ToPrettyString(args.Performer):user} used disarm on {entMan.ToPrettyString(args.Target):target}");

            // Check if the event has been handled, and if so, do nothing else!
            if (eventArgs.Handled)
                return;

            // Sort by priority.
            Array.Sort(disarmedActs, (a, b) => a.Priority.CompareTo(b.Priority));

            // TODO: Remove this shit.
            foreach (var disarmedAct in disarmedActs)
            {
                if (disarmedAct.Disarmed(eventArgs))
                    return;
            }

            SoundSystem.Play(Filter.Pvs(args.Performer), DisarmSuccessSound.GetSound(), entMan.GetComponent<TransformComponent>(args.Performer).Coordinates, AudioHelpers.WithVariation(0.025f));
        }
    }
}
