using System;
using Content.Server.Utility;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedWindowComponent))]
    public class WindowComponent : SharedWindowComponent, IExamine, IInteractHand
    {
        private int? Damage
        {
            get
            {
                if (!Owner.TryGetComponent(out IDamageableComponent damageableComponent)) return null;
                return damageableComponent.TotalDamage;
            }
        }

        private int? MaxDamage
        {
            get
            {
                if (!Owner.TryGetComponent(out IDamageableComponent damageableComponent)) return null;
                return damageableComponent.Thresholds[DamageState.Dead];
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            if (Owner.TryGetComponent(out IDamageableComponent damageableComponent))
            {
                damageableComponent.HealthChangedEvent += OnDamage;
            }
        }

        private void OnDamage(HealthChangedEventArgs eventArgs)
        {
            int current = eventArgs.Damageable.TotalDamage;
            int max = eventArgs.Damageable.Thresholds[DamageState.Dead];
            if (eventArgs.Damageable.CurrentState == DamageState.Dead) return;
            UpdateVisuals(current, max);
        }

        private void UpdateVisuals(int currentDamage, int maxDamage)
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(WindowVisuals.Damage, (float) currentDamage / maxDamage);
            }
        }


        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            int? damage = Damage;
            int? maxDamage = MaxDamage;
            if (damage == null || maxDamage == null) return;
            float fraction = ((damage == 0 || maxDamage == 0) ? 0f : (float) damage / maxDamage) ?? 0f;
            int level = Math.Min(ContentHelpers.RoundToLevels(fraction, 1, 7), 5);
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
