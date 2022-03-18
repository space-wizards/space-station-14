using Content.Server.Throwing;
using Content.Shared.Input;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Players;

namespace Content.Server.Pulling
{
    [UsedImplicitly]
    public sealed class PullingSystem : SharedPullingSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(PhysicsSystem));

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.ReleasePulledObject, InputCmdHandler.FromDelegate(HandleReleasePulledObject))
                .Register<PullingSystem>();
        }

        private void HandleReleasePulledObject(ICommonSession? session)
        {
            if (session?.AttachedEntity is not {Valid: true} player)
            {
                return;
            }

            if (!TryGetPulled(player, out var pulled))
            {
                return;
            }

            if (!EntityManager.TryGetComponent(pulled.Value, out SharedPullableComponent? pullable))
            {
                return;
            }

            TryStopPull(pullable);
        }

        public override bool TryMoveTo(SharedPullableComponent pullable, EntityCoordinates to)
        {
            if (!base.TryMoveTo(pullable, to)) return false;

            var xformQuery = GetEntityQuery<TransformComponent>();
            var pullableXform = xformQuery.GetComponent(pullable.Owner);
            var targetPos = to.ToMap(EntityManager);

            if (targetPos.MapId != pullableXform.MapID) return false;

            var pullablePos = pullableXform.WorldPosition;

            if (pullablePos.EqualsApprox(targetPos.Position, 0.01f)) return false;

            var vec = (targetPos.Position - pullablePos);

            // Just move it to the spot for finetuning
            if (vec.Length < 0.15f)
            {
                pullableXform.WorldPosition = targetPos.Position;
                return true;
            }

            pullable.Owner.TryThrow(vec, 3f);
            return true;
        }
    }
}
