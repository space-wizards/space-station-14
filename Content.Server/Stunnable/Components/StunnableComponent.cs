#nullable enable
using Content.Server.Act;
using Content.Server.Notification;
using Content.Shared.Audio;
using Content.Shared.MobState;
using Content.Shared.Notification.Managers;
using Content.Shared.Sound;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Stunnable.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStunnableComponent))]
    public class StunnableComponent : SharedStunnableComponent, IDisarmedAct
    {
        [DataField("stunAttemptSound")] private SoundSpecifier _stunAttemptSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

        protected override void OnKnockdown()
        {
            EntitySystem.Get<StandingStateSystem>().Down(Owner);
        }

        protected override void OnKnockdownEnd()
        {
            if(Owner.TryGetComponent(out IMobStateComponent? mobState) && !mobState.IsIncapacitated())
                EntitySystem.Get<StandingStateSystem>().Stand(Owner);
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
                EntitySystem.Get<StandingStateSystem>().Stand(Owner);
            }

            KnockdownTimer = null;
            Dirty();
        }

        protected override void OnInteractHand()
        {
            if(_stunAttemptSound.TryGetSound(out var sound))
                SoundSystem.Play(Filter.Pvs(Owner), sound, Owner, AudioHelpers.WithVariation(0.05f));
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
                if (_stunAttemptSound.TryGetSound(out var sound))
                    SoundSystem.Play(Filter.Pvs(source), sound, source, AudioHelpers.WithVariation(0.025f));
                if (target != null)
                {
                    source.PopupMessageOtherClients(Loc.GetString("stunnable-component-disarm-success-others", ("source", source.Name),("target", target.Name)));
                    source.PopupMessageCursor(Loc.GetString("stunnable-component-disarm-success",("target", target.Name)));
                }
            }

            return true;
        }
    }
}
