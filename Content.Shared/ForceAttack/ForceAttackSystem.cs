using System.Linq;
using Content.Shared.CombatMode;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed.Commands.Generic;
using Robust.Shared.Utility;

namespace Content.Shared.ForceAttack;

/// <summary>
/// This handles forcing a player-controlled mob to attack nearby enemies.
/// </summary>
public sealed class ForceAttackSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly SharedCombatModeSystem _mode = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ForceAttackComponent, MeleeAttackEvent>(OnMeleeAttack);
    }

    private void OnMeleeAttack(Entity<ForceAttackComponent> ent, ref MeleeAttackEvent args)
    {
        var curTime = _timing.CurTime;
        ent.Comp.NextAttack = curTime + ent.Comp.PassiveTime;
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        // Query includes MindContainerComponent to only get mobs currently controlled by players
        var query = EntityQueryEnumerator<ForceAttackComponent, NpcFactionMemberComponent, CombatModeComponent, MindContainerComponent>();
        while (query.MoveNext(out var uid, out var forceComp, out var factionComp, out var modeComp, out var _))
        {
            // Check if we have a weapon
            if (!_melee.TryGetWeapon(uid, out var weaponUid, out var weapon))
            {
                forceComp.InRange = false;
                continue;
            }

            // Find a target in range that isn't critical or dead
            var hostiles = _faction.GetNearbyHostiles((uid, factionComp), weapon.Range);
            if (!hostiles
                    .Where((potTarget) => !_mob.IsIncapacitated(potTarget))
                    .TryFirstOrNull(out var target))
            {
                forceComp.InRange = false;
                continue;
            }

            if (!forceComp.InRange)
            {
                forceComp.InRange = true;
                forceComp.NextAttack = curTime + forceComp.PassiveTime;
                continue;
            }

            if (forceComp.NextAttack > curTime || weapon.NextAttack > curTime)
                continue;

            // Force mob to enter combat mode (necessary for AttemptAttack to succeed).
            _mode.SetInCombatMode(uid, true, modeComp);
            if (forceComp.Message.Length != 0)
                _popup.PopupPredicted(forceComp.Message, uid, uid);

            _melee.AttemptLightAttack(uid, weaponUid, weapon, target.Value);
        }
    }
}
