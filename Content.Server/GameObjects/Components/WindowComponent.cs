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
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedWindowComponent))]
    public class WindowComponent : SharedWindowComponent, IExamine, IInteractHand
    {
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
                    message.AddText(Loc.GetString("It looks fully intact."));
                    break;
                case 1:
                    message.AddText(Loc.GetString("It has a few scratches."));
                    break;
                case 2:
                    message.AddText(Loc.GetString("It has a few small cracks."));
                    break;
                case 3:
                    message.AddText(Loc.GetString("It has several big cracks running along its surface."));
                    break;
                case 4:
                    message.AddText(Loc.GetString("It has deep cracks across multiple layers."));
                    break;
                case 5:
                    message.AddText(Loc.GetString("It is extremely badly cracked and on the verge of shattering."));
                    break;
            }
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            EntitySystem.Get<AudioSystem>()
                .PlayAtCoords("/Audio/Effects/glass_knock.ogg", eventArgs.Target.Transform.Coordinates, AudioHelpers.WithVariation(0.05f));
            eventArgs.Target.PopupMessageEveryone(Loc.GetString("*knock knock*"));
            return true;
        }
    }
}
