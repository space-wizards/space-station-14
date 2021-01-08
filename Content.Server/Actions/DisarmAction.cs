#nullable enable
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Pulling;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Utility;
using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Pulling;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    public class DisarmAction : ITargetEntityAction
    {
        private float _failProb;
        private float _pushProb;
        private float _cooldown;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _failProb, "failProb", 0.4f);
            serializer.DataField(ref _pushProb, "pushProb", 0.4f);
            serializer.DataField(ref _cooldown, "cooldown", 1.5f);
        }

        public void DoTargetEntityAction(TargetEntityActionEventArgs args)
        {
            if (!ValidTarget(args.Target) || !args.Performer.InRangeUnobstructed(args.Target)) return;
            if (!args.Performer.TryGetComponent<SharedActionsComponent>(out var actions)) return;
            if (args.Target == args.Performer || !args.Performer.CanAttack()) return;

            var eventArgs = new DisarmedActEventArgs() {Target = args.Target, Source = args.Performer};

            foreach (var disarmedAct in args.Target.GetAllComponents<IDisarmedAct>())
            {
                if (disarmedAct.Disarmed(eventArgs))
                    return;
            }

            var random = IoCManager.Resolve<IRobustRandom>();
            var audio = EntitySystem.Get<AudioSystem>();
            var system = EntitySystem.Get<MeleeWeaponSystem>();

            actions.Cooldown(ActionType.Disarm, Cooldowns.SecondsFromNow(_cooldown));

            var angle = new Angle(args.Target.Transform.MapPosition.Position - args.Performer.Transform.MapPosition.Position);

            if (random.Prob(_failProb))
            {
                audio.PlayFromEntity("/Audio/Weapons/punchmiss.ogg", args.Performer,
                    AudioHelpers.WithVariation(0.025f));
                args.Performer.PopupMessageOtherClients(Loc.GetString("{0} fails to disarm {1}!", args.Performer.Name, args.Target.Name));
                args.Performer.PopupMessageCursor(Loc.GetString("You fail to disarm {0}!", args.Target.Name));
                system.SendLunge(angle, args.Performer);
                return;
            }

            system.SendAnimation("disarm", angle, args.Performer, args.Performer, new []{ args.Target });

            if (args.Target.TryGetComponent(out StunnableComponent? stunnable) && random.Prob(_pushProb))
            {
                stunnable.Paralyze(4f);

                audio.PlayFromEntity("/Audio/Effects/thudswoosh.ogg", args.Performer,
                    AudioHelpers.WithVariation(0.025f));

                args.Performer.PopupMessageOtherClients(Loc.GetString("{0} pushes {1}!", args.Performer.Name, args.Target.Name));
                args.Performer.PopupMessageCursor(Loc.GetString("You push {0}!", args.Target.Name));

                return;
            }

            if (!BreakPulls(args.Target) && args.Target.TryGetComponent(out HandsComponent? hands))
            {
                if (hands.ActiveHand != null && hands.Drop(hands.ActiveHand, false))
                {
                    args.Performer.PopupMessageOtherClients(Loc.GetString("{0} disarms {1}!", args.Performer.Name, args.Target.Name));
                    args.Performer.PopupMessageCursor(Loc.GetString("You disarm {0}!", args.Target.Name));
                }
                else
                {
                    args.Performer.PopupMessageOtherClients(Loc.GetString("{0} shoves {1}!", args.Performer.Name, args.Target.Name));
                    args.Performer.PopupMessageCursor(Loc.GetString("You shove {0}!", args.Target.Name));
                }
            }

            audio.PlayFromEntity("/Audio/Effects/thudswoosh.ogg", args.Performer,
                AudioHelpers.WithVariation(0.025f));
        }

        private bool BreakPulls(IEntity target)
        {
            // What is this API??
            if (!target.TryGetComponent(out SharedPullerComponent? puller) || puller.Pulling == null
            || !puller.Pulling.TryGetComponent(out PullableComponent? pullable)) return false;

            return pullable.TryStopPull();
        }

        private bool ValidTarget(IEntity target)
        {
            return target.HasComponent<HandsComponent>() || target.HasComponent<StunnableComponent>();
        }
    }
}
