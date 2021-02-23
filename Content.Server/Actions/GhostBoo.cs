#nullable enable
using System.Linq;
using Content.Server.GameObjects.Components.Observer;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Server.Actions
{
    /// <summary>
    ///     Blink lights and scare livings
    /// </summary>
    [UsedImplicitly]
    public class GhostBoo : IInstantAction
    {
        private float _radius;
        private float _cooldown;
        private int _maxTargets;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _radius, "radius", 3);
            serializer.DataField(ref _cooldown, "cooldown", 120);
            serializer.DataField(ref _maxTargets, "maxTargets", 3);
        }

        public void DoInstantAction(InstantActionEventArgs args)
        {
            if (!args.Performer.TryGetComponent<SharedActionsComponent>(out var actions)) return;

            // find all IGhostBooAffected nearby and do boo on them
            var entityMan = args.Performer.EntityManager;
            var ents = entityMan.GetEntitiesInRange(args.Performer, _radius, false).ToList();

            var booCounter = 0;
            foreach (var ent in ents)
            {
                var boos = ent.GetAllComponents<IGhostBooAffected>().ToList();
                foreach (var boo in boos)
                {
                    boo.AffectedByGhostBoo(args);
                    booCounter++;

                    // yes, this is goto
                    // the quickest escape from nested loops
                    if (booCounter >= _maxTargets)
                        goto EnoughBoo;
                }

            }

            EnoughBoo:
            actions.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(_cooldown));
        }
    }
}
