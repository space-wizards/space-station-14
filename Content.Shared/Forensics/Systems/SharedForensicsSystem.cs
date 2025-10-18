using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Content.Shared.Fluids;
using Content.Shared.Forensics.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Forensics.Systems;

public sealed class ForensicsSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HandsComponent, ContactInteractionEvent>(OnInteract);
        SubscribeLocalEvent<FingerprintComponent, MapInitEvent>(OnFingerprintInit, after: new[] { typeof(SharedBloodstreamSystem) });
        // The solution entities are spawned on MapInit as well, so we have to wait for that to be able to set the DNA in the bloodstream correctly without ResolveSolution failing
        SubscribeLocalEvent<DnaComponent, MapInitEvent>(OnDNAInit, after: new[] { typeof(SharedBloodstreamSystem) });

        SubscribeLocalEvent<ForensicsComponent, BeingGibbedEvent>(OnBeingGibbed);
        SubscribeLocalEvent<ForensicsComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ForensicsComponent, GotRehydratedEvent>(OnRehydrated);
        SubscribeLocalEvent<CleansForensicsComponent, AfterInteractEvent>(OnAfterInteract, after: new[] { typeof(SharedAbsorbentSystem) });
        SubscribeLocalEvent<ForensicsComponent, CleanForensicsDoAfterEvent>(OnCleanForensicsDoAfter);
        SubscribeLocalEvent<DnaSubstanceTraceComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
        SubscribeLocalEvent<CleansForensicsComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
    }

    private void OnSolutionChanged(Entity<DnaSubstanceTraceComponent> ent, ref SolutionContainerChangedEvent ev)
    {
        var soln = GetSolutionsDNA(ev.Solution);
        if (soln.Count > 0)
        {
            var comp = EnsureComp<ForensicsComponent>(ent.Owner);
            foreach (string dna in soln)
            {
                comp.DNAs.Add(dna);
            }
            Dirty(ent, ent.Comp);
        }
    }

    private void OnInteract(EntityUid uid, HandsComponent component, ContactInteractionEvent args)
    {
        ApplyEvidence(uid, args.Other);
    }

    private void OnFingerprintInit(Entity<FingerprintComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Fingerprint == null)
            RandomizeFingerprint((ent.Owner, ent.Comp));
    }

    private void OnDNAInit(Entity<DnaComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.DNA == null)
            RandomizeDNA((ent.Owner, ent.Comp));
        else
        {
            // If set manually (for example by cloning) we also need to inform the bloodstream of the correct DNA string so it can be updated
            var ev = new GenerateDnaEvent ( ent.Owner, ent.Comp.DNA );
            RaiseLocalEvent(ent.Owner, ref ev);
        }
    }

    private void OnBeingGibbed(EntityUid uid, ForensicsComponent component, BeingGibbedEvent args)
    {
        string dna = Loc.GetString("forensics-dna-unknown");

        if (TryComp(uid, out DnaComponent? dnaComp) && dnaComp.DNA != null)
            dna = dnaComp.DNA;

        foreach (EntityUid part in args.GibbedParts)
        {
            var partComp = EnsureComp<ForensicsComponent>(part);
            partComp.DNAs.Add(dna);
            partComp.CanDnaBeCleaned = false;
            Dirty(part, partComp);
        }
    }

    private void OnMeleeHit(EntityUid uid, ForensicsComponent component, MeleeHitEvent args)
    {
        if ((args.BaseDamage.DamageDict.TryGetValue("Blunt", out var bluntDamage) && bluntDamage.Value > 0) ||
            (args.BaseDamage.DamageDict.TryGetValue("Slash", out var slashDamage) && slashDamage.Value > 0) ||
            (args.BaseDamage.DamageDict.TryGetValue("Piercing", out var pierceDamage) && pierceDamage.Value > 0))
        {
            foreach (EntityUid hitEntity in args.HitEntities)
            {
                if (TryComp<DnaComponent>(hitEntity, out var hitEntityComp) && hitEntityComp.DNA != null)
                    component.DNAs.Add(hitEntityComp.DNA);
            }
        }
        Dirty(uid, component);
    }

    private void OnRehydrated(Entity<ForensicsComponent> ent, ref GotRehydratedEvent args)
    {
        CopyForensicsFrom(ent.Owner, args.Target);
    }

    /// <summary>
    /// Copy forensic information from a source entity to a destination.
    /// Existing forensic information on the target is still kept.
    /// </summary>
    public void CopyForensicsFrom(Entity <ForensicsComponent?> src, EntityUid target)
    {
        if (!Resolve(target, ref src.Comp, false))
        {
            return;
        }

        var targetComp = EnsureComp<ForensicsComponent>(target);
        foreach (var dna in src.Comp.DNAs)
        {
            targetComp.DNAs.Add(dna);
        }

        foreach (var fiber in src.Comp.Fibers)
        {
            targetComp.Fibers.Add(fiber);
        }

        foreach (var print in src.Comp.Fingerprints)
        {
            targetComp.Fingerprints.Add(print);
        }

        foreach (var residue in src.Comp.Residues)
        {
            targetComp.Residues.Add(residue);
        }
        Dirty(target, targetComp);
    }

    public List<string> GetSolutionsDNA(EntityUid uid)
    {
        List<string> list = new();
        if (TryComp<SolutionContainerManagerComponent>(uid, out var comp))
        {
            foreach (var (_, soln) in _solutionContainerSystem.EnumerateSolutions((uid, comp)))
            {
                list.AddRange(GetSolutionsDNA(soln.Comp.Solution));
            }
        }
        return list;
    }

    public List<string> GetSolutionsDNA(Solution soln)
    {
        List<string> list = new();
        foreach (var reagent in soln.Contents)
        {
            foreach (var data in reagent.Reagent.EnsureReagentData())
            {
                if (data is DnaData)
                {
                    list.Add(((DnaData)data).DNA);
                }
            }
        }
        return list;
    }

    private void OnAfterInteract(Entity<CleansForensicsComponent> cleanForensicsEntity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        args.Handled = TryStartCleaning(cleanForensicsEntity, args.User, args.Target.Value);
    }

    private void OnUtilityVerb(Entity<CleansForensicsComponent> entity, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        // These need to be set outside for the anonymous method!
        var user = args.User;
        var target = args.Target;

        var verb = new UtilityVerb()
        {
            Act = () => TryStartCleaning(entity, user, target),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/bubbles.svg.192dpi.png")),
            Text = Loc.GetString(Loc.GetString("forensics-verb-text")),
            Message = Loc.GetString(Loc.GetString("forensics-verb-message")),
            // This is important because if its true using the cleaning device will count as touching the object.
            DoContactInteraction = false
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Attempts to clean the given item with the given CleansForensics entity.
    /// </summary>
    /// <param name="cleanForensicsEntity">The entity that is being used to clean the target.</param>
    /// <param name="user">The user that is using the cleanForensicsEntity.</param>
    /// <param name="target">The target of the forensics clean.</param>
    /// <returns>True if the target can be cleaned and has some sort of DNA or fingerprints / fibers and false otherwise.</returns>
    public bool TryStartCleaning(Entity<CleansForensicsComponent> cleanForensicsEntity, EntityUid user, EntityUid target)
    {
        if (!TryComp<ForensicsComponent>(target, out var forensicsComp))
        {
            _popupSystem.PopupClient(Loc.GetString("forensics-cleaning-cannot-clean", ("target", target)), user, user, PopupType.MediumCaution);
            return false;
        }

        var totalPrintsAndFibers = forensicsComp.Fingerprints.Count + forensicsComp.Fibers.Count;
        var hasRemovableDNA = forensicsComp.DNAs.Count > 0 && forensicsComp.CanDnaBeCleaned;

        if (hasRemovableDNA || totalPrintsAndFibers > 0)
        {
            var cleanDelay = cleanForensicsEntity.Comp.CleanDelay;
            var doAfterArgs = new DoAfterArgs(EntityManager, user, cleanDelay, new CleanForensicsDoAfterEvent(), cleanForensicsEntity, target: target, used: cleanForensicsEntity)
            {
                NeedHand = true,
                BreakOnDamage = true,
                BreakOnMove = true,
                MovementThreshold = 0.01f,
                DistanceThreshold = forensicsComp.CleanDistance,
            };

            _doAfterSystem.TryStartDoAfter(doAfterArgs);

            _popupSystem.PopupClient(Loc.GetString("forensics-cleaning", ("target", target)), user, user);

            return true;
        }
        else
        {
            _popupSystem.PopupClient(Loc.GetString("forensics-cleaning-cannot-clean", ("target", target)), user, user, PopupType.MediumCaution);
            return false;
        }

    }

    private void OnCleanForensicsDoAfter(EntityUid uid, ForensicsComponent component, CleanForensicsDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        if (!TryComp<ForensicsComponent>(args.Target, out var targetComp))
            return;

        targetComp.Fibers = new();
        targetComp.Fingerprints = new();

        if (targetComp.CanDnaBeCleaned)
            targetComp.DNAs = new();

        // leave behind evidence it was cleaned
        if (TryComp<FiberComponent>(args.Used, out var fiber))
            targetComp.Fibers.Add(string.IsNullOrEmpty(fiber.FiberColor) ? Loc.GetString("forensic-fibers", ("material", fiber.FiberMaterial)) : Loc.GetString("forensic-fibers-colored", ("color", fiber.FiberColor), ("material", fiber.FiberMaterial)));

        if (TryComp<ResidueComponent>(args.Used, out var residue))
            targetComp.Residues.Add(string.IsNullOrEmpty(residue.ResidueColor) ? Loc.GetString("forensic-residue", ("adjective", residue.ResidueAdjective)) : Loc.GetString("forensic-residue-colored", ("color", residue.ResidueColor), ("adjective", residue.ResidueAdjective)));

        Dirty(args.Target.Value, targetComp);
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
        var dna = string.Empty;

        for (var i = 0; i < 16; i++)
        {
            dna += letters[_random.Next(letters.Length)];
        }

        return dna;
    }

    private void ApplyEvidence(EntityUid user, EntityUid target)
    {
        if (HasComp<IgnoresFingerprintsComponent>(target))
            return;

        var component = EnsureComp<ForensicsComponent>(target);
        if (_inventory.TryGetSlotEntity(user, "gloves", out var gloves))
        {
            if (TryComp<FiberComponent>(gloves, out var fiber) && !string.IsNullOrEmpty(fiber.FiberMaterial))
                component.Fibers.Add(string.IsNullOrEmpty(fiber.FiberColor) ? Loc.GetString("forensic-fibers", ("material", fiber.FiberMaterial)) : Loc.GetString("forensic-fibers-colored", ("color", fiber.FiberColor), ("material", fiber.FiberMaterial)));
        }

        if (TryComp<FingerprintComponent>(user, out var fingerprint) && CanAccessFingerprint(user, out _))
            component.Fingerprints.Add(fingerprint.Fingerprint ?? "");

        Dirty(target, component);
    }

    #region PublicAPI

    public void RandomizeDNA(Entity<DnaComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.DNA = GenerateDNA();
        Dirty(ent);

        var ev = new GenerateDnaEvent( ent.Owner, ent.Comp.DNA );
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    public void RandomizeFingerprint(Entity<FingerprintComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Fingerprint = GenerateFingerprint();
        Dirty(ent);
    }

    /// <summary>
    /// Transfer DNA from one entity onto the forensics of another
    /// </summary>
    /// <param name="recipient">The entity receiving the DNA</param>
    /// <param name="donor">The entity applying its DNA</param>
    /// <param name="canDnaBeCleaned">If this DNA be cleaned off of the recipient. e.g. cleaning a knife vs cleaning a puddle of blood</param>
    public void TransferDna(EntityUid recipient, EntityUid donor, bool canDnaBeCleaned = true)
    {
        if (TryComp<DnaComponent>(donor, out var donorComp) && donorComp.DNA != null)
        {
            EnsureComp<ForensicsComponent>(recipient, out var recipientComp);
            recipientComp.DNAs.Add(donorComp.DNA);
            recipientComp.CanDnaBeCleaned = canDnaBeCleaned;

            Dirty(recipient, recipientComp);
        }
    }

    /// <summary>
    /// Checks if there's a way to access the fingerprint of the target entity.
    /// </summary>
    /// <param name="target">The entity with the fingerprint</param>
    /// <param name="blocker">The entity that blocked accessing the fingerprint</param>
    public bool CanAccessFingerprint(EntityUid target, out EntityUid? blocker)
    {
        var ev = new TryAccessFingerprintEvent();

        RaiseLocalEvent(target, ev);
        if (!ev.Cancelled && TryComp<InventoryComponent>(target, out var inv))
            _inventory.RelayEvent((target, inv), ev);

        blocker = ev.Blocker;
        return !ev.Cancelled;
    }

    #endregion
}
