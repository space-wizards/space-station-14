#nullable enable
using System;
using Content.Server.Utility;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Server.GameObjects.Components.Destructible;
using Content.Server.GameObjects.Components.Destructible.Thresholds.Triggers;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedWindowComponent))]
    public class WindowComponent : SharedWindowComponent, IExamine, IInteractHand
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        [ViewVariables(VVAccess.ReadWrite)] private TimeSpan _lastKnockTime;

        [DataField("knockDelay")] [ViewVariables(VVAccess.ReadWrite)]
        private TimeSpan _knockDelay = TimeSpan.FromSeconds(0.5);

        [DataField("rateLimitedKnocking")]
        [ViewVariables(VVAccess.ReadWrite)] private bool _rateLimitedKnocking = true;

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case DamageChangedMessage msg:
                {
                    var current = msg.Damageable.TotalDamage;
                    UpdateVisuals(current);
                    break;
                }
            }
        }

        private void UpdateVisuals(int currentDamage)
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
            if (!Owner.TryGetComponent(out IDamageableComponent? damageable) ||
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

            EntitySystem.Get<AudioSystem>()
                .PlayAtCoords("/Audio/Effects/glass_knock.ogg", eventArgs.Target.Transform.Coordinates, AudioHelpers.WithVariation(0.05f));
            eventArgs.Target.PopupMessageEveryone(Loc.GetString("comp-window-knock"));

            _lastKnockTime = _gameTiming.CurTime;

            return true;
        }
    }
}
