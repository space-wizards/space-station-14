using Content.Server.Actions.Events;
using Content.Server.Administration.Components;
using Content.Server.Administration.Logs;
using Content.Server.CombatMode.Disarm;
using Content.Server.Hands.Components;
using Content.Server.Popups;
using Content.Server.Contests;
using Content.Server.Weapon.Melee;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Stunnable;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Physics;

namespace Content.Server.CombatMode
{
    [UsedImplicitly]
    public sealed class CombatModeSystem : SharedCombatModeSystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly MeleeWeaponSystem _meleeWeaponSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger= default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        [Dependency] private readonly ContestsSystem _contests = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedCombatModeComponent, DisarmActionEvent>(OnEntityActionPerform);
            SubscribeLocalEvent<SharedCombatModeComponent, ComponentGetState>(OnGetState);
        }

        private void OnGetState(EntityUid uid, SharedCombatModeComponent component, ref ComponentGetState args)
        {
            args.State = new CombatModeComponentState(component.PrecisionMode, component.IsInCombatMode, component.ActiveZone);
        }

        private void OnEntityActionPerform(EntityUid uid, SharedCombatModeComponent component, DisarmActionEvent args)
        {
            if (args.Handled)
                return;

            if (!_actionBlockerSystem.CanAttack(args.Performer))
                return;

            // TODO: Toggle disarm
        }
    }
}
