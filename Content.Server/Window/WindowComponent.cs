using System;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Triggers;
using Content.Server.Popups;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Rounding;
using Content.Shared.Sound;
using Content.Shared.Window;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Window
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedWindowComponent))]
    public class WindowComponent : SharedWindowComponent, IExamine, IInteractHand
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        [ViewVariables(VVAccess.ReadWrite)] private TimeSpan _lastKnockTime;

        [DataField("knockDelay")]
        [ViewVariables(VVAccess.ReadWrite)]
        private TimeSpan _knockDelay = TimeSpan.FromSeconds(0.5);

        [DataField("rateLimitedKnocking")]
        [ViewVariables(VVAccess.ReadWrite)] private bool _rateLimitedKnocking = true;

        [DataField("knockSound")]
        private SoundSpecifier _knockSound = new SoundPathSpecifier("/Audio/Effects/glass_knock.ogg");

        public void UpdateVisuals(int currentDamage)
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance) &&
                Owner.TryGetComponent(out DestructibleComponent? destructible))
            {
                foreach (var threshold in destructible.Thresholds)
                {
                    if (threshold.Trigger is not DamageTrigger trigger)
                    {
                        continue;
                    }

                    appearance.SetData(WindowVisuals.Damage, (float) currentDamage / trigger.Damage);
                }
            }
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!Owner.TryGetComponent(out DamageableComponent? damageable) ||
                !Owner.TryGetComponent(out DestructibleComponent? destructible))
            {
                return;
            }

            var damage = damageable.TotalDamage;
            DamageTrigger? trigger = null;

            // TODO: Pretend this does not exist until https://github.com/space-wizards/space-station-14/pull/2783 is merged
            foreach (var threshold in destructible.Thresholds)
            {
                if ((trigger = threshold.Trigger as DamageTrigger) != null)
                {
                    break;
                }
            }

            if (trigger == null)
            {
                return;
            }

            var damageThreshold = trigger.Damage;
            var fraction = damage == 0 || damageThreshold == 0
                ? 0f
                : (float) damage / damageThreshold;
            var level = Math.Min(ContentHelpers.RoundToLevels((double) fraction, 1, 7), 5);

            switch (level)
            {
                case 0:
                    message.AddText(Loc.GetString("comp-window-damaged-1"));
                    break;
                case 1:
                    message.AddText(Loc.GetString("comp-window-damaged-2"));
                    break;
                case 2:
                    message.AddText(Loc.GetString("comp-window-damaged-3"));
                    break;
                case 3:
                    message.AddText(Loc.GetString("comp-window-damaged-4"));
                    break;
                case 4:
                    message.AddText(Loc.GetString("comp-window-damaged-5"));
                    break;
                case 5:
                    message.AddText(Loc.GetString("comp-window-damaged-6"));
                    break;
            }
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            if (_rateLimitedKnocking && _gameTiming.CurTime < _lastKnockTime + _knockDelay)
            {
                return false;
            }

            SoundSystem.Play(
                Filter.Pvs(eventArgs.Target), _knockSound.GetSound(),
                eventArgs.Target.Transform.Coordinates, AudioHelpers.WithVariation(0.05f));
            eventArgs.Target.PopupMessageEveryone(Loc.GetString("comp-window-knock"));

            _lastKnockTime = _gameTiming.CurTime;

            return true;
        }
    }
}
