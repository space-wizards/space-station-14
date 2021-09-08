using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Shared.Verbs
{
    public class SharedVerbSystem : EntitySystem
    {
        /// <summary>
        ///     Raises a number of events in order to get all verbs of the given type(s)
        /// </summary>
        public Dictionary<VerbType, List<Verb>> GetVerbs(IEntity target, IEntity user, VerbType verbTypes)
        {
            Dictionary<VerbType, List<Verb>> verbs = new();

            if (verbTypes.HasFlag(VerbType.Activation))
            {
                GetInteractionVerbsEvent getVerbEvent = new(user, target, prepareGUI: true);
                RaiseLocalEvent(target.Uid, getVerbEvent);
                verbs.Add(VerbType.Activation, getVerbEvent.Verbs);
            }

            if (verbTypes.HasFlag(VerbType.Interaction))
            {
                GetActivationVerbsEvent getVerbEvent = new(user, target, prepareGUI: true);
                RaiseLocalEvent(target.Uid, getVerbEvent);
                verbs.Add(VerbType.Interaction, getVerbEvent.Verbs);
            }

            if (verbTypes.HasFlag(VerbType.Alternative))
            {
                GetAlternativeVerbsEvent getVerbEvent = new(user, target, prepareGUI: true);
                RaiseLocalEvent(target.Uid, getVerbEvent);
                verbs.Add(VerbType.Alternative, getVerbEvent.Verbs);
            }

            if (verbTypes.HasFlag(VerbType.Other))
            {
                GetOtherVerbsEvent getVerbEvent = new(user, target, prepareGUI: true);
                RaiseLocalEvent(target.Uid, getVerbEvent);
                verbs.Add(VerbType.Other, getVerbEvent.Verbs);
            }

            return verbs;
        }

        /// <summary>
        ///     Get all of the entities relevant for the context menu
        /// </summary>
        /// <param name="player"></param>
        /// <param name="targetPos"></param>
        /// <param name="contextEntities"></param>
        /// <param name="buffer">Whether we should slightly extend out the ignored range for the ray predicated</param>
        /// <returns></returns>
        public bool TryGetContextEntities(IEntity player, MapCoordinates targetPos, [NotNullWhen(true)] out List<IEntity>? contextEntities, bool buffer = false)
        {
            contextEntities = null;
            var length = buffer ? 1.0f : 0.5f;

            var entities = IoCManager.Resolve<IEntityLookup>().
                GetEntitiesIntersecting(targetPos.MapId, Box2.CenteredAround(targetPos.Position, (length, length))).ToList();

            if (entities.Count == 0)
            {
                return false;
            }

            // Check if we have LOS to the clicked-location, otherwise no popup.
            var vectorDiff = player.Transform.MapPosition.Position - targetPos.Position;
            var distance = vectorDiff.Length + 0.01f;
            bool Ignored(IEntity entity)
            {
                return entities.Contains(entity) ||
                       entity == player ||
                       !entity.TryGetComponent(out OccluderComponent? occluder) ||
                       !occluder.Enabled;
            }

            var mask = player.TryGetComponent(out SharedEyeComponent? eye) && eye.DrawFov
                ? CollisionGroup.Opaque
                : CollisionGroup.None;

            var result = player.InRangeUnobstructed(targetPos, distance, mask, Ignored);

            if (!result)
            {
                return false;
            }

            contextEntities = entities;
            return true;
        }


        /// <summary>
        ///     Execute actions associated with the given verb.
        /// </summary>
        /// <remarks>
        ///     This will try to call delegates and raises any events. If all of these are null for the given verb, this
        ///     is likely because this was a verb that was defined server-side and was sent to the client. In that case, ask to
        ///     run the verb over the network.
        /// </remarks>
        /// <param name="verb"> The verb to run</param>
        /// <param name="networkFallback"> Whether or not to ask to run the verb over the network. This is needed
        /// to stop an infinite loop in case a verb truly has no associated actions</param>
        /// <param name="target">The verb target. Needed when executing over the network</param>
        /// <param name="verbType">The verb type. Needed when executing over the network</param>
        public void TryExecuteVerb(Verb verb, bool networkFallback = false, EntityUid target = default, VerbType verbType = 0)
        {
            // Run the delegate
            verb.Act?.Invoke();

            // Raise the local event
            if (verb.LocalVerbEventArgs != null)
            {
                if (verb.LocalEventTarget.IsValid())
                {
                    RaiseLocalEvent(verb.LocalEventTarget, verb.LocalVerbEventArgs);
                }
                else
                {
                    RaiseLocalEvent(verb.LocalVerbEventArgs);
                }
            }

            // Network event
            if (verb.NetworkVerbEventArgs != null)
            {
                RaiseNetworkEvent(verb.NetworkVerbEventArgs);
            }

            if (verb.Act == null &&
                verb.LocalVerbEventArgs == null &&
                verb.NetworkVerbEventArgs == null)
            {
                // There was nothing to do. This verb was probably defined server-side and sent to the client. Ask to run over the network.
                if (networkFallback &&
                    target != EntityUid.Invalid &&
                    verbType != 0)
                {
                    RaiseNetworkEvent(new TryExecuteVerbEvent(target, verb.Key, verbType));
                }
                else
                {
                    Logger.Warning($"Tried to execute verb ({verb.Key}) with no associated action or network fall-back options.");
                }
            }
        }
    }
}
