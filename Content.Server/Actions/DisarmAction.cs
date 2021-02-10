#nullable enable
using System;
using System.Linq;
using Content.Server.GameObjects.EntitySystems;
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
using Robust.Shared.GameObjects;
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

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _failProb, "failProb", 0.4f);
            serializer.DataField(ref _pushProb, "pushProb", 0.4f);
            serializer.DataField(ref _cooldown, "cooldown", 1.5f);
        }

        public void DoTargetEntityAction(TargetEntityActionEventArgs args)
        {
            var disarmedActs = args.Target.GetAllComponents<IDisarmedAct>().ToArray();

            if (disarmedActs.Length == 0 || !args.Performer.InRangeUnobstructed(args.Target)) return;
            if (!args.Performer.TryGetComponent<SharedActionsComponent>(out var actions)) return;
            if (args.Target == args.Performer || !args.Performer.CanAttack()) return;

            var random = IoCManager.Resolve<IRobustRandom>();
            var audio = EntitySystem.Get<AudioSystem>();
            var system = EntitySystem.Get<MeleeWeaponSystem>();

            var angle = new Angle(args.Target.Transform.MapPosition.Position - args.Performer.Transform.MapPosition.Position);

            actions.Cooldown(ActionType.Disarm, Cooldowns.SecondsFromNow(_cooldown));

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

            var eventArgs = new DisarmedActEventArgs() {Target = args.Target, Source = args.Performer, PushProbability = _pushProb};

            // Sort by priority.
            Array.Sort(disarmedActs, (a, b) => a.Priority.CompareTo(b.Priority));

            foreach (var disarmedAct in disarmedActs)
            {
                if (disarmedAct.Disarmed(eventArgs))
                    return;
            }

            audio.PlayFromEntity("/Audio/Effects/thudswoosh.ogg", args.Performer,
                AudioHelpers.WithVariation(0.025f));
        }
    }
}
