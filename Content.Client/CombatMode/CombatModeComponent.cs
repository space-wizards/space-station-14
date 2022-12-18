using Content.Client.Verbs;
using Content.Shared.CombatMode;
using Content.Shared.Targeting;
using Robust.Client.Player;

namespace Content.Client.CombatMode
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedCombatModeComponent))]
    public sealed class CombatModeComponent : SharedCombatModeComponent
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override bool IsInCombatMode
        {
            get => base.IsInCombatMode;
            set
            {
                base.IsInCombatMode = value;
                UpdateHud();
            }
        }

        public override TargetingZone ActiveZone
        {
            get => base.ActiveZone;
            set
            {
                base.ActiveZone = value;
                UpdateHud();
            }
        }

        private void UpdateHud()
        {
            if (Owner != _playerManager.LocalPlayer?.ControlledEntity)
            {
                return;
            }

            var verbs = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<VerbSystem>();
            verbs.CloseAllMenus();
        }
    }
}
