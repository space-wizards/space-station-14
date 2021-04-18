#nullable enable
using System;
using System.Linq;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Utility;
using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Content.Shared.Utility;
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

namespace Content.Server.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    public class DisarmAction : ITargetEntityAction
    {
        [DataField("failProb")] private float _failProb = 0.4f;
        [DataField("pushProb")] private float _pushProb = 0.4f;
        [DataField("cooldown")] private float _cooldown = 1.5f;

        public void DoTargetEntityAction(TargetEntityActionEventArgs args)
        {
            var disarmedActs = args.Target.GetAllComponents<IDisarmedAct>().ToArray();

            if (!args.Performer.InRangeUnobstructed(args.Target)) return;

            if (disarmedActs.Length == 0)
            {
                if (args.Performer.TryGetComponent(out IActorComponent? actor))
                {
                    // Fall back to a normal interaction with the entity
                    var player = actor.playerSession;
                    var coordinates = args.Target.Transform.Coordinates;
                    var target = args.Target.Uid;
                    EntitySystem.Get<InteractionSystem>().HandleClientUseItemInHand(player, coordinates, target);
                    return;
                }

                return;
            }

            if (!args.Performer.TryGetComponent<SharedActionsComponent>(out var actions)) return;
            if (args.Target == args.Performer || !args.Performer.CanAttack()) return;

            var random = IoCManager.Resolve<IRobustRandom>();
            var audio = EntitySystem.Get<AudioSystem>();
            var system = EntitySystem.Get<MeleeWeaponSystem>();

            var diff = args.Target.Transform.MapPosition.Position - args.Performer.Transform.MapPosition.Position;
            var angle = Angle.FromWorldVec(diff);

            actions.Cooldown(ActionType.Disarm, Cooldowns.SecondsFromNow(_cooldown));

            if (random.Prob(_failProb))
            {
                SoundSystem.Play(Filter.Pvs(args.Performer), "/Audio/Weapons/punchmiss.ogg", args.Performer,
                    AudioHelpers.WithVariation(0.025f));
                args.Performer.PopupMessageOtherClients(Loc.GetString("{0} fails to disarm {1}!", args.Performer.Name, args.Target.Name));
                args.Performer.PopupMessageCursor(Loc.GetString("You fail to disarm {0}!", args.Target.Name));
                system.SendLunge(angle, args.Performer);
                return;
            }

            system.SendAnimation("disarm", angle, args.Performer, args.Performer, new []{ args.Target });

            var eventArgs = new DisarmedActEventArgs() {Target = args.Target, Source = args.Performer, PushProbability = _pushProb};

            // Sort by priority.
            Array.Sort(disarmedActs, (a, b) => a.Priority.CompareTo(b.Priority));

            foreach (var disarmedAct in disarmedActs)
            {
                if (disarmedAct.Disarmed(eventArgs))
                    return;
            }

            SoundSystem.Play(Filter.Pvs(args.Performer), "/Audio/Effects/thudswoosh.ogg", args.Performer,
                AudioHelpers.WithVariation(0.025f));
        }
    }
}
