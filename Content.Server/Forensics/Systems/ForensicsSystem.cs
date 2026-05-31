using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Forensics.Components;
using Content.Shared.Forensics.Events;
using Content.Shared.Forensics.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;

namespace Content.Server.Forensics.Systems;

public sealed partial class ForensicsSystem : SharedForensicsSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandsComponent, ContactInteractionEvent>(OnInteract);
        SubscribeLocalEvent<FingerprintComponent, MapInitEvent>(OnFingerprintInit, after: [typeof(SharedBloodstreamSystem),]);
        // The solution entities are spawned on MapInit as well, so we have to wait for that to be able to set the DNA in the bloodstream correctly without ResolveSolution failing
        SubscribeLocalEvent<DnaComponent, MapInitEvent>(OnDNAInit, after: [typeof(SharedBloodstreamSystem)]);

        SubscribeLocalEvent<ForensicsComponent, GibbedBeforeDeletionEvent>(OnBeingGibbed);
        SubscribeLocalEvent<ForensicsComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ForensicsComponent, GotRehydratedEvent>(OnRehydrated);
        SubscribeLocalEvent<ForensicsComponent, CleanForensicsDoAfterEvent>(OnCleanForensicsDoAfter);
        SubscribeLocalEvent<DnaSubstanceTraceComponent, SolutionChangedEvent>(OnSolutionChanged);
    }

    private void OnSolutionChanged(Entity<DnaSubstanceTraceComponent> puddle, ref SolutionChangedEvent ev)
    {
        var soln = GetSolutionsDNA(ev.Solution);

        if (soln.Count <= 0)
            return;

        var comp = EnsureComp<ForensicsComponent>(puddle.Owner);
        foreach (var dna in soln)
        {
            comp.DNAs.Add(dna);
        }
    }

    private void OnInteract(Entity<HandsComponent> hands, ref ContactInteractionEvent args)
    {
        ApplyEvidence(hands.Owner, args.Other);
    }

    private void OnFingerprintInit(Entity<FingerprintComponent> fingerPrint, ref MapInitEvent args)
    {
        if (fingerPrint.Comp.Fingerprint == null)
            RandomizeFingerprint((fingerPrint.Owner, fingerPrint.Comp));
    }

    private void OnDNAInit(Entity<DnaComponent> dna, ref MapInitEvent args)
    {
        if (dna.Comp.DNA == null)
            RandomizeDNA(dna.AsNullable());
        else
        {
            // If set manually (for example by cloning) we also need to inform the bloodstream of the correct DNA string so it can be updated
            var ev = new GenerateDnaEvent { Owner = dna.Owner, DNA = dna.Comp.DNA };
            RaiseLocalEvent(dna.Owner, ref ev);
        }
    }

    private void OnBeingGibbed(Entity<ForensicsComponent> mob, ref GibbedBeforeDeletionEvent args)
    {
        foreach (var part in args.Giblets)
        {
            TransferDna(part, mob, false);
        }
    }

    private void OnMeleeHit(Entity<ForensicsComponent> weapon, ref MeleeHitEvent args)
    {
        if ((!args.BaseDamage.DamageDict.TryGetValue("Blunt", out var bluntDamage) || bluntDamage.Value <= 0) &&
            (!args.BaseDamage.DamageDict.TryGetValue("Slash", out var slashDamage) || slashDamage.Value <= 0) &&
            (!args.BaseDamage.DamageDict.TryGetValue("Piercing", out var pierceDamage) || pierceDamage.Value <= 0))
            return;

        foreach (var hitEntity in args.HitEntities)
        {
            TransferDna(weapon, hitEntity);
        }
    }

    private void OnRehydrated(Entity<ForensicsComponent> ent, ref GotRehydratedEvent args)
    {
        CopyForensicsFrom(ent.Comp, args.Target);
    }

    private void OnCleanForensicsDoAfter(Entity<ForensicsComponent> component, ref CleanForensicsDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        if (!TryComp<ForensicsComponent>(args.Target, out var targetComp))
            return;

        targetComp.Fibers = [];
        targetComp.Fingerprints = [];

        if (targetComp.CanDnaBeCleaned)
            targetComp.DNAs = [];

        // leave behind evidence it was cleaned
        if (TryComp<FiberComponent>(args.Used, out var fiber))
            targetComp.Fibers.Add(string.IsNullOrEmpty(fiber.FiberColor) ? Loc.GetString("forensic-fibers", ("material", fiber.FiberMaterial)) : Loc.GetString("forensic-fibers-colored", ("color", fiber.FiberColor), ("material", fiber.FiberMaterial)));

        if (TryComp<ResidueComponent>(args.Used, out var residue))
            targetComp.Residues.Add(string.IsNullOrEmpty(residue.ResidueColor) ? Loc.GetString("forensic-residue", ("adjective", residue.ResidueAdjective)) : Loc.GetString("forensic-residue-colored", ("color", residue.ResidueColor), ("adjective", residue.ResidueAdjective)));

        targetComp.IsDirty = false;
        Dirty(args.Target.Value, targetComp);
    }

    private void ApplyEvidence(EntityUid user, EntityUid target)
    {
        if (HasComp<IgnoresFingerprintsComponent>(target))
            return;

        var component = EnsureComp<ForensicsComponent>(target);
        if (_inventory.TryGetSlotEntity(user, "gloves", out var gloves) && TryComp<FiberComponent>(gloves, out var fiber))
        {
            component.Fibers.Add(string.IsNullOrEmpty(fiber.FiberColor)
                ? Loc.GetString("forensic-fibers", ("material", fiber.FiberMaterial))
                : Loc.GetString("forensic-fibers-colored", ("color", fiber.FiberColor), ("material", fiber.FiberMaterial)));

            EnableCleanable((target, component));
        }

        if (TryAccessFingerprint(user, out _, out var fingerprint))
        {
            component.Fingerprints.Add(fingerprint);
            EnableCleanable((target, component));
        }
    }

    /// <summary>
    /// Sets the Client-side boolean and networks it to clients.
    /// </summary>
    /// <param name="cleanable">The entity with cleanable forensics.</param>
    private void EnableCleanable(Entity<ForensicsComponent> cleanable)
    {
        if (cleanable.Comp.IsDirty)
            return;

        cleanable.Comp.IsDirty = true;
        Dirty(cleanable);
    }

    /// <summary>
    /// Copy forensic information from a source entity to a destination.
    /// Existing forensic information on the target is still kept.
    /// </summary>
    public void CopyForensicsFrom(ForensicsComponent comp, EntityUid target)
    {
        var dest = EnsureComp<ForensicsComponent>(target);
        foreach (var dna in comp.DNAs)
        {
            dest.DNAs.Add(dna);
        }

        foreach (var fiber in comp.Fibers)
        {
            dest.Fibers.Add(fiber);
        }

        foreach (var print in comp.Fingerprints)
        {
            dest.Fingerprints.Add(print);
        }

        foreach (var residue in comp.Residues)
        {
            dest.Residues.Add(residue);
        }
    }

    public List<string> GetSolutionsDNA(EntityUid uid)
    {
        List<string> list = [];

        foreach (var (_, soln) in _solutionContainerSystem.EnumerateSolutions(uid))
        {
            list.AddRange(GetSolutionsDNA(soln.Comp.Solution));
        }
        return list;
    }

    public List<string> GetSolutionsDNA(Solution soln)
    {
        List<string> list = [];
        foreach (var reagent in soln.Contents)
        {
            foreach (var data in reagent.Reagent.EnsureReagentData())
            {
                if (data is DnaData)
                {
                    list.Add(((DnaData) data).DNA);
                }
            }
        }
        return list;
    }

    public string GenerateFingerprint()
    {
        var fingerprint = new byte[16];
        _random.NextBytes(fingerprint);
        return Convert.ToHexString(fingerprint);
    }

    public string GenerateDNA()
    {
        var letters = new[] { "A", "C", "G", "T" };
        var DNA = string.Empty;

        for (var i = 0; i < 16; i++)
        {
            DNA += letters[_random.Next(letters.Length)];
        }

        return DNA;
    }

    #region Public API
    public override void RandomizeDNA(Entity<DnaComponent?> dnaOwner)
    {
        if (!Resolve(dnaOwner, ref dnaOwner.Comp, false))
            return;

        dnaOwner.Comp.DNA = GenerateDNA();

        var ev = new GenerateDnaEvent { Owner = dnaOwner.Owner, DNA = dnaOwner.Comp.DNA };
        RaiseLocalEvent(dnaOwner.Owner, ref ev);
    }

    public override void RandomizeFingerprint(Entity<FingerprintComponent?> fingerprintOwner)
    {
        if (!Resolve(fingerprintOwner, ref fingerprintOwner.Comp, false))
            return;

        fingerprintOwner.Comp.Fingerprint = GenerateFingerprint();
        Dirty(fingerprintOwner);
    }

    public override void TransferDna(EntityUid recipient, EntityUid donor, bool canDnaBeCleaned = true)
    {
        if (!TryComp<DnaComponent>(donor, out var donorComp) || donorComp.DNA == null)
            return;

        EnsureComp<ForensicsComponent>(recipient, out var recipientComp);
        recipientComp.DNAs.Add(donorComp.DNA);
        // This seems problematic. What if something adds a uncleanable DNA and then something else adds a cleanable DNA?
        recipientComp.CanDnaBeCleaned = canDnaBeCleaned;

        if (canDnaBeCleaned)
            EnableCleanable((recipient,  recipientComp));
    }
    #endregion
}
