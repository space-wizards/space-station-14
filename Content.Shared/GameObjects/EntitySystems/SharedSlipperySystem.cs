#nullable enable
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems.EffectBlocker;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedSlipperySystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SlipperyComponent, SteppedOnEvent>(HandleSlip);
        }

        private void HandleSlip(EntityUid uid, SlipperyComponent component, SteppedOnEvent args)
        {
            if (!component.Slippery || component.Owner.GetComponent<IPhysBody>().LinearVelocity.LengthSquared > 0.0f) return;
            TrySlip(args.By, component);
        }

        private void TrySlip(IEntity slipped, SlipperyComponent slippery)
        {
            if (!slipped.TryGetComponent(out IPhysBody? body) ||
                !slipped.TryGetComponent(out SharedStunnableComponent? stun) ||
                slipped.IsInContainer())
            {
                return;
            }

            if (body.LinearVelocity.Length < slippery.RequiredSlipSpeed || stun.KnockedDown)
            {
                return;
            }

            if (!EffectBlockerSystem.CanSlip(slipped))
            {
                return;
            }

            body.LinearVelocity *= slippery.LaunchForwardsMultiplier;
            stun.Paralyze(5);

            if (!string.IsNullOrEmpty(slippery.SlipSound) && _gameTiming.IsFirstTimePredicted)
            {
                SoundSystem.Play(GetFilter(slipped), slippery.SlipSound, slipped, AudioHelpers.WithVariation(0.2f));
            }
        }

        protected abstract Filter GetFilter(IEntity entity);
    }
}
