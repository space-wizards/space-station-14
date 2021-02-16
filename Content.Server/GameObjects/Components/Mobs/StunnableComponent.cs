using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Utility;
using Content.Shared.Alert;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStunnableComponent))]
    public class StunnableComponent : SharedStunnableComponent, IDisarmedAct
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        protected override void OnKnockdown()
        {
            EntitySystem.Get<StandingStateSystem>().Down(Owner);
        }

        public void CancelAll()
        {
            KnockdownTimer = 0f;
            StunnedTimer = 0f;
            Dirty();
        }

        public void ResetStuns()
        {
            StunnedTimer = 0f;
            SlowdownTimer = 0f;

            if (KnockedDown)
            {
                EntitySystem.Get<StandingStateSystem>().Standing(Owner);
            }

            KnockdownTimer = 0f;
            Dirty();
        }

        public void Update(float delta)
        {
            if (Stunned)
            {
                StunnedTimer -= delta;

                if (StunnedTimer <= 0)
                {
                    StunnedTimer = 0f;
                    Dirty();
                }
            }

            if (KnockedDown)
            {
                KnockdownTimer -= delta;

                if (KnockdownTimer <= 0f)
                {
                    EntitySystem.Get<StandingStateSystem>().Standing(Owner);

                    KnockdownTimer = 0f;
                    Dirty();
                }
            }

            if (SlowedDown)
            {
                SlowdownTimer -= delta;

                if (SlowdownTimer <= 0f)
                {
                    SlowdownTimer = 0f;

                    if (Owner.TryGetComponent(out MovementSpeedModifierComponent movement))
                    {
                        movement.RefreshMovementSpeedModifiers();
                    }

                    Dirty();
                }
            }

            if (!StunStart.HasValue || !StunEnd.HasValue ||
                !Owner.TryGetComponent(out ServerAlertsComponent status))
            {
                return;
            }

            var start = StunStart.Value;
            var end = StunEnd.Value;

            var length = (end - start).TotalSeconds;
            var progress = (_gameTiming.CurTime - start).TotalSeconds;

            if (progress >= length)
            {
                Owner.SpawnTimer(250, () => status.ClearAlert(AlertType.Stun), StatusRemoveCancellation.Token);
                LastStun = null;
            }
        }

        protected override void OnInteractHand()
        {
            EntitySystem.Get<AudioSystem>()
                .PlayFromEntity("/Audio/Effects/thudswoosh.ogg", Owner, AudioHelpers.WithVariation(0.05f));
        }

        public override ComponentState GetComponentState()
        {
            return new StunnableComponentState(StunnedTimer, KnockdownTimer, SlowdownTimer, WalkModifierOverride,
                RunModifierOverride);
        }

        bool IDisarmedAct.Disarmed(DisarmedActEventArgs eventArgs)
        {
            if (!IoCManager.Resolve<IRobustRandom>().Prob(eventArgs.PushProbability))
                return false;

            Paralyze(4f);

            var source = eventArgs.Source;

            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/thudswoosh.ogg", source,
                AudioHelpers.WithVariation(0.025f));

            source.PopupMessageOtherClients(Loc.GetString("{0} pushes {1}!", source.Name, eventArgs.Target.Name));
            source.PopupMessageCursor(Loc.GetString("You push {0}!", eventArgs.Target.Name));

            return true;
        }
    }
}
