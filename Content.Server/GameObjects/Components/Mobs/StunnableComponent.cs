#nullable enable
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Utility;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStunnableComponent))]
    public class StunnableComponent : SharedStunnableComponent, IDisarmedAct
    {
        protected override void OnKnockdown()
        {
            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new AttemptDownEvent());
        }

        protected override void OnKnockdownEnd()
        {
            if(Owner.TryGetComponent(out IMobStateComponent? mobState) && !mobState.IsIncapacitated())
                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new AttemptStandEvent());
        }

        public void CancelAll()
        {
            KnockdownTimer = null;
            StunnedTimer = null;
            Dirty();
        }

        public void ResetStuns()
        {
            StunnedTimer = null;
            SlowdownTimer = null;

            if (KnockedDown &&
                Owner.TryGetComponent(out IMobStateComponent? mobState) && !mobState.IsIncapacitated())
            {
                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new AttemptStandEvent());
            }

            KnockdownTimer = null;
            Dirty();
        }

        protected override void OnInteractHand()
        {
            SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Effects/thudswoosh.ogg", Owner, AudioHelpers.WithVariation(0.05f));
        }

        bool IDisarmedAct.Disarmed(DisarmedActEventArgs eventArgs)
        {
            if (!IoCManager.Resolve<IRobustRandom>().Prob(eventArgs.PushProbability))
                return false;

            Paralyze(4f);

            var source = eventArgs.Source;
            var target = eventArgs.Target;

            if (source != null)
            {
                SoundSystem.Play(Filter.Pvs(source), "/Audio/Effects/thudswoosh.ogg", source,
                    AudioHelpers.WithVariation(0.025f));
                if (target != null)
                {
                    source.PopupMessageOtherClients(Loc.GetString("{0} pushes {1}!", source.Name, target.Name));
                    source.PopupMessageCursor(Loc.GetString("You push {0}!", target.Name));
                }
            }

            return true;
        }
    }
}
