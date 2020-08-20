using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Mobs.Roles;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Suspicion
{
    [RegisterComponent]
    public class SuspicionRoleComponent : Component, IExamine
    {
        public override string Name => "SuspicionRole";

        public bool IsDead()
        {
            return Owner.TryGetComponent(out IDamageableComponent damageable) &&
                   damageable.CurrentDamageState == DamageState.Dead;
        }

        public bool IsTraitor()
        {
            return Owner.TryGetComponent(out MindComponent mind) &&
                   mind.HasMind &&
                   mind.Mind!.HasRole<SuspicionTraitorRole>();
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!IsDead())
            {
                return;
            }

            var tooltip = IsTraitor()
                ? Loc.GetString($"They were a [color=red]traitor[/color]!")
                : Loc.GetString($"They were an [color=green]innocent[/color]!");

            message.AddMarkup(tooltip);
        }
    }
}
