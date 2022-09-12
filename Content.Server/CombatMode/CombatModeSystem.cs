using Content.Shared.CombatMode;
using JetBrains.Annotations;
using Robust.Shared.GameStates;

namespace Content.Server.CombatMode
{
    [UsedImplicitly]
    public sealed class CombatModeSystem : SharedCombatModeSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedCombatModeComponent, ComponentGetState>(OnGetState);
        }

        private void OnGetState(EntityUid uid, SharedCombatModeComponent component, ref ComponentGetState args)
        {
            args.State = new CombatModeComponentState(component.IsInCombatMode, component.ActiveZone);
        }
    }
}
