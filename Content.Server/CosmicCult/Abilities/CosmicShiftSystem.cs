// SPDX-FileCopyrightText: 2026 AftrLite
//
// SPDX-License-Identifier: LicenseRef-CosmicCult

using System.Collections.Immutable;
using Content.Server.Atmos.Components;
using Content.Shared.Actions;
using Content.Shared.CosmicCult.Abilities;
using Content.Shared.CosmicCult.Components;
using Content.Shared.Doors.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Components;
using Content.Shared.Popups;
using Content.Stellar.Server.CosmicCult.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.CosmicCult.Abilities;

public sealed class CosmicShiftSystem : SharedCosmicShiftSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var shuntQuery = EntityQueryEnumerator<CosmicShiftedComponent>();
        while (shuntQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.ReadyToReturn && !comp.Occupied)
            {
                _actions.RemoveAction(uid, comp.CosmicReturnActionActionEntity);
                EnsureComp<BlockMovementComponent>(uid);
                RemComp<CosmicShiftedComponent>(uid);
                TransformSystem.AnchorEntity(uid);

                ShiftToDestination(uid, comp.DepartureCoordinates);
                foreach (var entity in _lookup.GetEntitiesIntersecting(comp.DepartureCoordinates, LookupFlags.Static))
                {
                    if (HasComp<AirtightComponent>(entity) && !HasComp<AirlockComponent>(entity))
                        QueueDel(entity);
                }
            }
        }
    }

    protected override void OnShiftStartDoAfter(Entity<CosmicCultistComponent> ent, ref CosmicShiftStartDoAfter args)
    {
        if (args.Cancelled || args.Handled || Container.IsEntityInContainer(ent.Owner))
            return;

        var spawnPoints = EntityManager.GetAllComponents(typeof(CosmicVoidSpawnComponent)).ToImmutableList();
        if (spawnPoints.IsEmpty)
            return;

        var destination = TransformSystem.GetMapCoordinates(_random.Pick(spawnPoints).Uid);
        _popup.PopupCoordinates(Loc.GetString("cosmicability-shift-start", ("target", Identity.Entity(ent, EntityManager))), Transform(ent).Coordinates, PopupType.MediumCaution);

        EnsureComp<CosmicShiftedComponent>(ent, out var shiftedComp);
        ShiftToDestination(ent, destination);
        shiftedComp.DepartureCoordinates = TransformSystem.GetMapCoordinates(ent);
        shiftedComp.ReadyToReturn = false;
        base.OnShiftStartDoAfter(ent, ref args);
    }

    protected override void OnShiftMove(EntityUid ent, MapCoordinates destination)
    {
        TransformSystem.SetMapCoordinates(ent, destination);
        TransformSystem.AnchorEntity(ent);
        base.OnShiftMove(ent, destination);
    }

    protected override void OnShiftEnd(EntityUid ent)
    {
        base.OnShiftEnd(ent);
        if (TryComp<CosmicShiftedComponent>(ent, out var shiftComp))
            _actions.AddAction(ent, ref shiftComp.CosmicReturnActionActionEntity, shiftComp.CosmicReturnAction, ent);
    }
}
