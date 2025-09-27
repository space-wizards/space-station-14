using System.Runtime.InteropServices;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using System.Collections.Generic;
using Robust.Shared.Map;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.Disease;

/// <summary>
/// Decays disease residue on tiles/items and infects entities on direct contact.
/// </summary>
public sealed class DiseaseResidueSystem : EntitySystem
{
    [Dependency] private readonly SharedDiseaseSystem _disease = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseCarrierComponent, ContactInteractionEvent>(OnCarrierContact);
        SubscribeLocalEvent<DiseaseResidueComponent, ContactInteractionEvent>(OnResidueContact);
        SubscribeLocalEvent<MeleeWeaponComponent, MeleeHitEvent>(OnMeleeHit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toRemoveAfterDecay = new ValueList<string>();
        var query = EntityQueryEnumerator<DiseaseResidueComponent>();
        while (query.MoveNext(out var uid, out var residue))
        {
            // Decay per-disease intensities.
            var decay = residue.DecayPerTick * (float)frameTime;
            toRemoveAfterDecay.Clear();

            foreach (var (id, value) in residue.Diseases)
            {
                var newVal = value - decay;
                if (newVal <= 0f)
                    toRemoveAfterDecay.Add(id);
                else
                    residue.Diseases[id] = newVal;
            }

            foreach (var k in toRemoveAfterDecay)
                residue.Diseases.Remove(k);

            if (residue.Diseases.Count == 0)
            {
                RemCompDeferred<DiseaseResidueComponent>(uid);
                continue;
            }
        }

        // Residue processing each disease tick.
        var carriers = EntityQueryEnumerator<DiseaseCarrierComponent>();
        var now = _timing.CurTime;
        while (carriers.MoveNext(out var cuid, out var carrier))
        {
            if (carrier.NextTick > now)
                continue;

            TryAdjacentContactSpread(cuid, carrier);
        }
    }

    /// <summary>
    /// Deposits per-disease residue intensity onto contacted entity.
    /// </summary>
    private void OnCarrierContact(EntityUid uid, DiseaseCarrierComponent carrier, ContactInteractionEvent args)
    {
        if (carrier.ActiveDiseases.Count == 0)
            return;

        var residue = EnsureComp<DiseaseResidueComponent>(args.Other);
        foreach (var (id, _) in carrier.ActiveDiseases)
        {
            if (!_prototypes.TryIndex(id, out DiseasePrototype? proto))
                continue;

            if ((proto.SpreadFlags & DiseaseSpreadFlags.Contact) == 0)
                continue;

            var deposit = proto.ContactDeposit;
            if (residue.Diseases.TryGetValue(id, out var cur))
                residue.Diseases[id] = MathF.Min(1f, cur + deposit);
            else
                residue.Diseases[id] = MathF.Min(1f, deposit);
        }
    }

    /// <summary>
    /// Attempts infection from residue to the contacting entity and reduces residue on contact.
    /// </summary>
    private void OnResidueContact(EntityUid uid, DiseaseResidueComponent residue, ContactInteractionEvent args)
    {
        if (residue.Diseases.Count == 0)
            return;

        foreach (var (id, intensity) in residue.Diseases)
        {
            InfectByContactChance(args.Other, id);

            var newIntensity = MathF.Max(0f, intensity - residue.ContactReduction);
            if (newIntensity > 0f)
                residue.Diseases[id] = newIntensity;
        }
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

            if (!_interaction.InRangeUnobstructed(source, other, 0.8f))
                continue;

            foreach (var (id, _) in carrier.ActiveDiseases)
            {
                InfectByContactChance(other, id);
            }
        }
    }

    /// <summary>
    /// Attempts infection on melee hit in both directions for contact-spread diseases.
    /// </summary>
    private void OnMeleeHit(Entity<MeleeWeaponComponent> weapon, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        var attackerUid = args.User;

        // Attacker -> Targets
        if (TryComp<DiseaseCarrierComponent>(attackerUid, out var attackerCar) && attackerCar.ActiveDiseases.Count > 0)
        {
            foreach (var target in args.HitEntities)
            {
                foreach (var (id, _) in attackerCar.ActiveDiseases)
                    InfectByContactChance(target, id);
            }
        }

        // Targets -> Attacker
        foreach (var target in args.HitEntities)
        {
            if (!TryComp<DiseaseCarrierComponent>(target, out var targetCar) || targetCar.ActiveDiseases.Count == 0)
                continue;

            foreach (var (id, _) in targetCar.ActiveDiseases)
                InfectByContactChance(attackerUid, id);
        }
    }

    /// <summary>
    /// Tries to infect a target via contact using fixed per-disease chance.
    /// </summary>
    private void InfectByContactChance(EntityUid target, string diseaseId)
    {
        if (!_prototypes.TryIndex(diseaseId, out DiseasePrototype? proto))
            return;

        if ((proto.SpreadFlags & DiseaseSpreadFlags.Contact) == 0)
            return;

        var chance = Math.Clamp(proto.ContactInfect, 0f, 1f);
        chance = _disease.AdjustContactChanceForProtection(target, chance, proto);
        _disease.TryInfectWithChance(target, diseaseId, chance);
    }
}
