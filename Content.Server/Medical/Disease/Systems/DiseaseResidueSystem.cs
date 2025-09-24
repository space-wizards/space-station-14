using System;
using System.Linq;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Medical.Disease;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Inventory;
using Robust.Shared.Map;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.Medical.Disease.Systems;

/// <summary>
/// Decays disease residue on tiles/items and infects entities on direct contact.
/// </summary>
public sealed class DiseaseResidueSystem : EntitySystem
{
    [Dependency] private readonly DiseaseSystem _disease = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseResidueComponent, ContactInteractionEvent>(OnResidueContact);
        SubscribeLocalEvent<DiseaseCarrierComponent, ContactInteractionEvent>(OnCarrierContact);
        SubscribeLocalEvent<DiseaseCarrierComponent, MeleeHitEvent>(OnMeleeHit);

        SubscribeLocalEvent<DiseaseCarrierComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DiseaseResidueComponent>();
        while (query.MoveNext(out var uid, out var residue))
        {
            // Decay per-disease intensities.
            var decay = residue.DecayPerTick * (float)frameTime;
            var toRemoveAfterDecay = new ValueList<string>();
            foreach (var kv in residue.Diseases.ToArray())
            {
                var newVal = kv.Value - decay;
                if (newVal <= 0f)
                    toRemoveAfterDecay.Add(kv.Key);
                else
                    residue.Diseases[kv.Key] = newVal;
            }

            foreach (var k in toRemoveAfterDecay)
                residue.Diseases.Remove(k);

            if (residue.Diseases.Count == 0)
            {
                RemComp<DiseaseResidueComponent>(uid);
                continue;
            }
        }

        // Adjacent contact spread around carriers each tick.
        var carriers = EntityQueryEnumerator<DiseaseCarrierComponent>();
        while (carriers.MoveNext(out var cuid, out var carrier))
        {
            TryAdjacentContactSpread(cuid, carrier);
            DepositFootResidue(cuid, carrier);
        }
    }

    /// <summary>
    /// Handles contact spread when a user uses an item on a diseased target (or vice versa).
    /// </summary>
    private void OnAfterInteractUsing(Entity<DiseaseCarrierComponent> target, ref AfterInteractUsingEvent args)
    {
        if (!args.CanReach || args.User == args.Target)
            return;

        var user = args.User;
        var other = target.Owner;

        var userHas = TryComp<DiseaseCarrierComponent>(user, out var userCarrier) && userCarrier.ActiveDiseases.Count > 0;
        var targetHas = target.Comp.ActiveDiseases.Count > 0;

        if (!userHas && !targetHas)
            return;

        if (userHas)
        {
            foreach (var (id, _) in userCarrier!.ActiveDiseases)
            {
                var proto = _prototypes.Index<DiseasePrototype>(id);
                if (proto.SpreadFlags.Contains(DiseaseSpreadFlags.Contact))
                    InfectByContactChance(other, id, 1f);
            }
        }

        if (targetHas)
        {
            foreach (var (id, _) in target.Comp.ActiveDiseases)
            {
                var proto = _prototypes.Index<DiseasePrototype>(id);
                if (proto.SpreadFlags.Contains(DiseaseSpreadFlags.Contact))
                    InfectByContactChance(user, id, 1f);
            }
        }
    }

    /// <summary>
    /// Attempts infection on melee hit in both directions for contact-spread diseases.
    /// </summary>
    private void OnMeleeHit(Entity<DiseaseCarrierComponent> attacker, ref MeleeHitEvent args)
    {
        if (attacker.Comp.ActiveDiseases.Count == 0 && args.HitEntities.Count == 0)
            return;

        // Attacker -> Targets
        if (attacker.Comp.ActiveDiseases.Count > 0)
        {
            foreach (var target in args.HitEntities)
            {
                foreach (var (id, _) in attacker.Comp.ActiveDiseases)
                {
                    if (!_prototypes.TryIndex<DiseasePrototype>(id, out var proto))
                        continue;

                    if (!proto.SpreadFlags.Contains(DiseaseSpreadFlags.Contact))
                        continue;

                    InfectByContactChance(target, id, 1f);
                }
            }
        }

        // Targets -> Attacker
        foreach (var target in args.HitEntities)
        {
            if (!TryComp<DiseaseCarrierComponent>(target, out var tcar) || tcar.ActiveDiseases.Count == 0)
                continue;

            foreach (var (id, _) in tcar.ActiveDiseases)
            {
                if (!_prototypes.TryIndex<DiseasePrototype>(id, out var proto))
                    continue;

                if (!proto.SpreadFlags.Contains(DiseaseSpreadFlags.Contact))
                    continue;

                InfectByContactChance(attacker.Owner, id, 1f);
            }
        }
    }

    /// <summary>
    /// Attempts contact-based infection and reduces residue intensity per contact.
    /// </summary>
    private void OnResidueContact(EntityUid uid, DiseaseResidueComponent residue, ContactInteractionEvent args)
    {
        var toRemove = new ValueList<string>();
        foreach (var kv in residue.Diseases.ToArray())
        {
            var id = kv.Key;
            var intensity = kv.Value;
            InfectByContactChance(args.Other, id, intensity);

            // reduce intensity after contact
            var newVal = intensity - residue.ContactReduction;
            if (newVal <= 0f)
                toRemove.Add(id);
            else
                residue.Diseases[id] = newVal;
        }

        foreach (var k in toRemove)
            residue.Diseases.Remove(k);
    }

    /// <summary>
    /// Deposits per-disease residue intensity onto contacted entity/tile.
    /// </summary>
    private void OnCarrierContact(EntityUid uid, DiseaseCarrierComponent carrier, ContactInteractionEvent args)
    {
        if (carrier.ActiveDiseases.Count == 0)
            return;

        var residue = EnsureComp<DiseaseResidueComponent>(args.Other);
        foreach (var (id, _) in carrier.ActiveDiseases)
        {
            if (!_prototypes.TryIndex<DiseasePrototype>(id, out var proto))
                continue;

            var deposit = proto.ContactDeposit;
            if (residue.Diseases.TryGetValue(id, out var cur))
                residue.Diseases[id] = MathF.Min(1f, cur + deposit);
            else
                residue.Diseases[id] = MathF.Min(1f, deposit);
        }
    }

    /// <summary>
    /// Tries to infect a target via contact, scaling chance by residue intensity and disease ContactInfect.
    /// </summary>
    private void InfectByContactChance(EntityUid target, string diseaseId, float intensity = 1f)
    {
        if (!_prototypes.TryIndex<DiseasePrototype>(diseaseId, out var proto))
            return;

        if (!proto.SpreadFlags.Contains(DiseaseSpreadFlags.Contact))
            return;

        var chance = Math.Clamp(proto.ContactInfect * intensity, 0f, 1f);
        chance = _disease.AdjustContactChanceForProtection(target, chance, proto);
        _disease.TryInfectWithChance(target, diseaseId, chance);
    }

    /// <summary>
    /// Adjacent contact spread within 1 tile if disease has Contact vector.
    /// </summary>
    private void TryAdjacentContactSpread(EntityUid source, DiseaseCarrierComponent carrier)
    {
        if (carrier.ActiveDiseases.Count == 0)
            return;

        var mapPos = _xform.GetMapCoordinates(source);
        if (mapPos.MapId == MapId.Nullspace)
            return;

        // Checks the proposed tile in the carrier range.
        var targets = _lookup.GetEntitiesInRange(mapPos, 1.0f, LookupFlags.Dynamic | LookupFlags.Sundries);
        foreach (var other in targets)
        {
            if (other == source)
                continue;

            if (!_interaction.InRangeUnobstructed(source, other, 1.1f))
                continue;

            foreach (var (id, _) in carrier.ActiveDiseases)
            {
                if (!_prototypes.TryIndex<DiseasePrototype>(id, out var proto))
                    continue;

                if (!proto.SpreadFlags.Contains(DiseaseSpreadFlags.Contact))
                    continue;

                InfectByContactChance(other, id, 1f);
            }
        }
    }

    /// <summary>
    /// Leaves a small residue on the tile under the carrier to emulate footprints.
    /// Shoes reduce the deposit amount.
    /// </summary>
    private void DepositFootResidue(EntityUid source, DiseaseCarrierComponent carrier)
    {
        if (carrier.ActiveDiseases.Count == 0)
            return;

        var coords = _xform.GetMapCoordinates(source);
        if (coords.MapId == MapId.Nullspace)
            return;

        // Checks if there is already a residue tile in the carrier range.
        EntityUid? residueEnt = null;
        foreach (var ent in _lookup.GetEntitiesInRange(coords, 0.2f, LookupFlags.Sundries))
        {
            if (TryComp<DiseaseResidueComponent>(ent, out _))
            {
                residueEnt = ent;
                break;
            }
        }

        if (residueEnt is null)
            residueEnt = EntityManager.SpawnEntity("DiseaseResidueTile", coords);

        // Adds the residue to the tile.
        var residue = EnsureComp<DiseaseResidueComponent>(residueEnt.Value);
        foreach (var (id, _) in carrier.ActiveDiseases)
        {
            if (!_prototypes.TryIndex<DiseasePrototype>(id, out var proto))
                continue;

            // Adjusts the deposit amount based on clothing items.
            var deposit = proto.ContactDeposit;
            foreach (var (slot, mult) in DiseaseEffectiveness.FootResidueSlots)
            {
                if (_inventory.TryGetSlotEntity(source, slot, out _))
                    deposit *= mult;
            }

            if (residue.Diseases.TryGetValue(id, out var cur))
                residue.Diseases[id] = MathF.Min(1f, cur + deposit);
            else
                residue.Diseases[id] = MathF.Min(1f, deposit);
        }
    }
}
