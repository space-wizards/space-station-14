using System.Linq;
using Content.Server.Medical.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Polymorph.Systems;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Genetics;
using Content.Shared.Genetics.GeneticsConsole;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mobs;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceLinking.Systems;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power;
using Content.Shared.Preferences;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics;

public sealed class GeneticsConsoleSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _deviceLink = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SharedGeneticsSystem _genetics = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    private const string MonkeyToHumanPolymorph = "GeneticsMonkeyToHuman";
    private const int MaxGeneticDamage = 150;
    private const float JokerCooldown = 20f;

    private float _uiUpdateTimer;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeneticsConsoleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GeneticsConsoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GeneticsConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<GeneticsConsoleComponent, SourcePortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<GeneticsConsoleComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<GeneticsConsoleComponent, AfterActivatableUIOpenEvent>(OnUiOpen);
        SubscribeLocalEvent<GeneticsConsoleComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<GeneticsConsoleComponent, GeneticsConsoleMessage>(OnConsoleMessage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _uiUpdateTimer += frameTime;
        if (_uiUpdateTimer < 1f)
            return;

        _uiUpdateTimer -= 1f;
        var query = EntityQueryEnumerator<GeneticsConsoleComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.JokerCooldownRemaining > 0f)
                component.JokerCooldownRemaining = Math.Max(0f, component.JokerCooldownRemaining - 1f);
            RecheckScanner(uid, component);
        }
    }

    private void OnInit(EntityUid uid, GeneticsConsoleComponent component, ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(uid, GeneticsConsoleComponent.ScannerPort);
    }

    private void OnMapInit(EntityUid uid, GeneticsConsoleComponent component, MapInitEvent args)
    {
        if (!TryComp<DeviceLinkSourceComponent>(uid, out var source))
            return;

        foreach (var linkedEntity in source.Outputs.Values.SelectMany(ports => ports))
        {
            if (!HasComp<MedicalScannerComponent>(linkedEntity))
                continue;

            component.GeneticScanner = linkedEntity;
            break;
        }

        RecheckScanner(uid, component);
    }

    private void OnNewLink(EntityUid uid, GeneticsConsoleComponent component, ref NewLinkEvent args)
    {
        if (args.SourcePort != GeneticsConsoleComponent.ScannerPort ||
            !HasComp<MedicalScannerComponent>(args.Sink))
            return;

        component.GeneticScanner = args.Sink;
        RecheckScanner(uid, component);
    }

    private void OnPortDisconnected(EntityUid uid, GeneticsConsoleComponent component, ref SourcePortDisconnectedEvent args)
    {
        if (args.Port != GeneticsConsoleComponent.ScannerPort)
            return;

        component.GeneticScanner = null;
        UpdateUserInterface(uid, component);
    }

    private void OnAnchorChanged(EntityUid uid, GeneticsConsoleComponent component, ref AnchorStateChangedEvent args)
    {
        RecheckScanner(uid, component);
    }

    private void OnUiOpen(EntityUid uid, GeneticsConsoleComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnPowerChanged(EntityUid uid, GeneticsConsoleComponent component, PowerChangedEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnConsoleMessage(EntityUid uid, GeneticsConsoleComponent component, GeneticsConsoleMessage args)
    {
        RecheckScanner(uid, component);
        component.LastResult = null;
        if (!_power.IsPowered(uid))
        {
            UpdateUserInterface(uid, component);
            return;
        }

        switch (args.Action)
        {
            case GeneticsConsoleAction.HumanizeMonkey:
                if (!TryGetSubject(component, out var monkey) || !IsMonkey(monkey))
                    break;

                var human = _polymorph.PolymorphEntity(monkey, MonkeyToHumanPolymorph);
                if (human is { } humanEntity)
                {
                    var profile = HumanoidCharacterProfile.RandomWithSpecies("Human");
                    _visualBody.ApplyProfileTo(humanEntity, profile);
                    _humanoidProfile.ApplyProfileTo(humanEntity, profile);
                    _metaData.SetEntityName(humanEntity, profile.Name);
                    component.LastResult = Loc.GetString("genetics-console-humanize-success", ("name", profile.Name));
                }
                else
                {
                    component.LastResult = Loc.GetString("genetics-console-humanize-failed");
                }
                break;

            case GeneticsConsoleAction.ScrambleDna:
                if (!TryGetSubject(component, out var scrambleTarget) ||
                    !HasComp<HumanoidProfileComponent>(scrambleTarget))
                    break;

                var scrambleDamage = new DamageSpecifier();
                scrambleDamage.DamageDict.Add("Cellular", FixedPoint2.New(15));
                _damageable.TryChangeDamage(scrambleTarget, scrambleDamage, ignoreResistances: true);
                _genetics.ScrambleGenome(scrambleTarget);
                component.LastResult = Loc.GetString("genetics-console-scramble-success");
                break;

            case GeneticsConsoleAction.StoreEnzymes:
                if (!TryGetSubject(component, out var enzymeSource) ||
                    !TryComp<HumanoidProfileComponent>(enzymeSource, out var sourceProfile))
                    break;

                component.StoredEnzymeProfile = HumanoidCharacterProfile
                    .DefaultWithSpecies(sourceProfile.Species, sourceProfile.Sex)
                    .WithName(MetaData(enzymeSource).EntityName)
                    .WithAge(sourceProfile.Age)
                    .WithVoice(sourceProfile.Voice)
                    .WithGender(sourceProfile.Gender);
                component.StoredOrganProfiles = null;
                component.StoredMarkings = null;
                if (_visualBody.TryGatherMarkingsData(
                        enzymeSource,
                        null,
                        out var enzymeProfiles,
                        out _,
                        out var enzymeMarkings))
                {
                    component.StoredOrganProfiles = enzymeProfiles;
                    component.StoredMarkings = enzymeMarkings;
                }
                component.LastResult = Loc.GetString("genetics-console-enzymes-saved");
                break;

            case GeneticsConsoleAction.InjectEnzymes:
                if (component.StoredEnzymeProfile == null ||
                    !TryGetSubject(component, out var enzymeTarget) ||
                    !HasComp<VisualBodyComponent>(enzymeTarget))
                    break;

                _visualBody.ApplyProfileTo(enzymeTarget, component.StoredEnzymeProfile);
                _humanoidProfile.ApplyProfileTo(enzymeTarget, component.StoredEnzymeProfile);
                _metaData.SetEntityName(enzymeTarget, component.StoredEnzymeProfile.Name);
                if (component.StoredOrganProfiles != null)
                    _visualBody.ApplyProfiles(enzymeTarget, component.StoredOrganProfiles);
                if (component.StoredMarkings != null)
                    _visualBody.ApplyMarkings(enzymeTarget, component.StoredMarkings);
                component.LastResult = Loc.GetString(
                    "genetics-console-enzymes-injected",
                    ("name", component.StoredEnzymeProfile.Name));
                break;

            case GeneticsConsoleAction.Analyze:
                if (!TryGetSubject(component, out var jokerTarget) ||
                    !HasComp<HumanoidProfileComponent>(jokerTarget) ||
                    !_genetics.IsPotentialMutation(jokerTarget, args.MutationId))
                    break;

                if (component.JokerCooldownRemaining > 0f)
                {
                    component.LastResult = Loc.GetString(
                        "genetics-console-joker-cooldown",
                        ("seconds", Math.Ceiling(component.JokerCooldownRemaining)));
                    break;
                }

                var jokerWorked = _genetics.TryAnalyzeMutation(jokerTarget, args.MutationId);
                component.JokerCooldownRemaining = JokerCooldown;
                component.LastJokerMutationId = args.MutationId;
                component.LastResult = Loc.GetString(jokerWorked
                    ? "genetics-console-joker-success"
                    : "genetics-console-joker-failed");
                break;

            case GeneticsConsoleAction.GuessSequence:
            case GeneticsConsoleAction.SetSequence:
            case GeneticsConsoleAction.Apply:
            case GeneticsConsoleAction.SaveToMemory:
                if (!TryGetSubject(component, out var subject) ||
                    !HasComp<HumanoidProfileComponent>(subject) ||
                    !_genetics.IsPotentialMutation(subject, args.MutationId) ||
                    !_prototypeManager.TryIndex<GeneticMutationPrototype>(args.MutationId, out var mutation))
                    break;

                if (!_genetics.TryGetMutationSequence(subject, mutation.ID, out var mutationSequence))
                    break;

                if (args.Action == GeneticsConsoleAction.SetSequence)
                {
                    if (IsCandidateStrand(args.Sequence, mutationSequence.Length) &&
                        IsCandidateStrand(args.ComplementSequence, mutationSequence.Length) &&
                        TryComp<GeneticMutationComponent>(subject, out var editedGenome))
                    {
                        editedGenome.SequenceGuesses.TryGetValue(mutation.ID, out var previousSequence);
                        editedGenome.ComplementSequenceGuesses.TryGetValue(mutation.ID, out var previousComplement);
                        if (previousSequence != args.Sequence || previousComplement != args.ComplementSequence)
                        {
                            editedGenome.SequenceGuesses[mutation.ID] = args.Sequence.ToUpperInvariant();
                            editedGenome.ComplementSequenceGuesses[mutation.ID] = args.ComplementSequence.ToUpperInvariant();
                            Dirty(subject, editedGenome);

                            var clickDamage = new DamageSpecifier();
                            clickDamage.DamageDict.Add("Cellular", FixedPoint2.New(1));
                            _damageable.TryChangeDamage(subject, clickDamage, ignoreResistances: true);
                        }
                    }
                }
                else if (args.Action == GeneticsConsoleAction.GuessSequence)
                {
                    var sequence = args.Sequence;
                    var complementSequence = args.ComplementSequence;
                    if (string.IsNullOrEmpty(sequence) &&
                        TryComp<GeneticMutationComponent>(subject, out var mutationDataCheck))
                    {
                        mutationDataCheck.SequenceGuesses.TryGetValue(mutation.ID, out sequence);
                        mutationDataCheck.ComplementSequenceGuesses.TryGetValue(mutation.ID, out complementSequence);
                    }

                    var knownPositions = _genetics.GetKnownPositions(subject, mutation.ID);
                    var validSequence = sequence is not null &&
                                        sequence.Length == mutationSequence.Length &&
                                        sequence.All(nucleotide => char.ToUpperInvariant(nucleotide) is 'A' or 'T' or 'G' or 'C');
                    var correct = false;
                    if (validSequence && complementSequence != null &&
                        IsCandidateStrand(complementSequence, mutationSequence.Length))
                    {
                        var mergedComplement = complementSequence.ToUpperInvariant().ToCharArray();
                        var merged = sequence!.ToUpperInvariant().ToCharArray();
                        for (var i = 0; i < mutationSequence.Length; i++)
                        {
                            if (knownPositions.Contains(i))
                            {
                                merged[i] = mutationSequence[i];
                                mergedComplement[i] = GeneticMutationPrototype.GetComplement(mutationSequence[i]);
                            }
                        }
                        var mergedSequence = new string(merged);
                        var mergedComplementSequence = new string(mergedComplement);

                        var genetics = EnsureComp<GeneticMutationComponent>(subject);
                        genetics.SequenceGuesses[mutation.ID] = mergedSequence;
                        genetics.ComplementSequenceGuesses[mutation.ID] = mergedComplementSequence;
                        Dirty(subject, genetics);
                        correct = _genetics.TryGuessMutation(
                            subject,
                            mutation.ID,
                            mergedSequence,
                            mergedComplementSequence);
                    }

                    component.LastResult = Loc.GetString(correct
                        ? "genetics-console-guess-correct"
                        : "genetics-console-guess-incorrect");
                }
                else if (args.Action == GeneticsConsoleAction.Apply)
                {
                    var applied = _mobState.IsAlive(subject) && _genetics.TryApplyMutation(subject, mutation.ID);
                    component.LastResult = Loc.GetString(applied
                        ? "genetics-console-apply-success"
                        : "genetics-console-apply-failed");
                }
                else
                {
                    var saved = component.MemoryMutations.Count < component.MaxMemorySlots &&
                                !component.MemoryMutations.Contains(mutation.ID) &&
                                TryComp<GeneticMutationComponent>(subject, out var genetics) &&
                                genetics.DiscoveredMutations.Contains(mutation.ID);
                    if (saved)
                        component.MemoryMutations.Add(mutation.ID);
                    component.LastResult = Loc.GetString(saved
                        ? "genetics-console-memory-saved"
                        : "genetics-console-memory-failed");
                }
                break;

            case GeneticsConsoleAction.CreateInjector:
                var injectorParts = args.MutationId.Split(':');
                if (injectorParts.Length is < 1 or > 2 ||
                    !component.MemoryMutations.Contains(injectorParts[0]))
                    break;

                var mutationId = injectorParts[0];
                var isActivator = injectorParts.Length == 2 && injectorParts[1] == "activator";
                if (injectorParts.Length == 2 && injectorParts[1] is not ("activator" or "mutator") ||
                    !_prototypeManager.TryIndex<GeneticMutationPrototype>(mutationId, out var injectorMutation))
                    break;

                var injectorEntity = Spawn("GeneticMutationInjector", Transform(uid).Coordinates);
                var injector = EnsureComp<GeneticMutationInjectorComponent>(injectorEntity);
                injector.MutationId = mutationId;
                injector.ActiveDuration = isActivator ? 300f : 0f;
                _metaData.SetEntityName(
                    injectorEntity,
                    $"{Loc.GetString(injectorMutation.Name)} {(isActivator
                        ? Loc.GetString("genetics-console-activator-suffix")
                        : Loc.GetString("genetics-console-mutator-suffix"))}");
                component.LastResult = Loc.GetString("genetics-console-injector-created");
                break;
        }

        UpdateUserInterface(uid, component);
    }

    private void RecheckScanner(EntityUid uid, GeneticsConsoleComponent component)
    {
        if (component.GeneticScanner is not { } scanner ||
            !TryComp<MedicalScannerComponent>(scanner, out _) ||
            !Transform(scanner).Anchored ||
            !Transform(scanner).Coordinates.TryDistance(EntityManager, Transform(uid).Coordinates, out var distance))
        {
            component.GeneticScannerInRange = false;
        }
        else
        {
            component.GeneticScannerInRange = distance <= component.MaxDistance;
        }

        UpdateUserInterface(uid, component);
    }

    private bool TryGetSubject(GeneticsConsoleComponent component, out EntityUid subject)
    {
        subject = default;
        if (component.GeneticScanner is not { } scanner ||
            !component.GeneticScannerInRange ||
            !TryComp<MedicalScannerComponent>(scanner, out var scannerComponent) ||
            !Transform(scanner).Anchored ||
            scannerComponent.BodyContainer.ContainedEntity is not { } body ||
            !HasComp<MobStateComponent>(body) ||
            !_mobState.IsAlive(body))
            return false;

        subject = body;
        return true;
    }

    private void UpdateUserInterface(EntityUid uid, GeneticsConsoleComponent component)
    {
        if (!_ui.HasUi(uid, GeneticsConsoleUiKey.Key))
            return;

        if (!_power.IsPowered(uid))
        {
            _ui.CloseUis(uid);
            return;
        }

        _ui.SetUiState(uid, GeneticsConsoleUiKey.Key, GetUserInterfaceState(component));
    }

    private GeneticsConsoleBoundUserInterfaceState GetUserInterfaceState(GeneticsConsoleComponent component)
    {
        var scannerConnected = component.GeneticScanner is { } scanner &&
                               HasComp<MedicalScannerComponent>(scanner);
        if (!scannerConnected ||
            !component.GeneticScannerInRange ||
            !TryGetSubject(component, out var subject))
        {
            return new GeneticsConsoleBoundUserInterfaceState(
                scannerConnected,
                component.GeneticScannerInRange,
                null,
                0,
                0,
                0,
                0,
                MaxGeneticDamage,
                false,
                false,
                component.LastResult,
                new List<GeneticsMutationUiEntry>(),
                GetMemoryEntries(component),
                component.MaxMemorySlots,
                (int) Math.Ceiling(component.JokerCooldownRemaining),
                component.StoredEnzymeProfile?.Name,
                GetStoredEnzymeDetails(component));
        }

        var instability = _genetics.GetInstability(subject);
        var totalDamage = _damageable.GetAllDamage(subject).DamageDict.Values.Aggregate(
            FixedPoint2.Zero,
            (total, damage) => total + damage);
        var maxHealth = _mobThreshold.GetThresholdForState(subject, MobState.Dead);
        var health = FixedPoint2.Max(FixedPoint2.Zero, maxHealth - totalDamage).Int();
        var cellularDamage = _damageable.GetAllDamage(subject).DamageDict.GetValueOrDefault("Cellular").Int();
        var canHumanizeSubject = IsMonkey(subject);
        var isHumanoidSubject = HasComp<HumanoidProfileComponent>(subject);
        var genetics = isHumanoidSubject ? _genetics.EnsureGenome(subject) : null;
        var entries = new List<GeneticsMutationUiEntry>();
        var index = 0;

        if (genetics is { } genome)
        {
            foreach (var mutationId in genome.PotentialMutations)
            {
                if (!_prototypeManager.TryIndex<GeneticMutationPrototype>(mutationId, out var mutation) ||
                    !genome.MutationSequences.TryGetValue(mutation.ID, out var mutationSequence))
                    continue;

                var progress = 0;
                var knownPositions = _genetics.GetKnownPositions(subject, mutation.ID);
                if (genome.DiscoveredMutations.Contains(mutation.ID))
                    progress = mutationSequence.Length;
                else
                    progress = knownPositions.Count;
                progress = Math.Clamp(progress, 0, mutationSequence.Length);
                var discovered = genome.DiscoveredMutations.Contains(mutation.ID);
                var active = genome.ActiveMutations.Contains(mutation.ID);
                var sequence = new string('?', mutationSequence.Length);
                genome.SequenceGuesses.TryGetValue(mutation.ID, out var guessedSequence);
                genome.ComplementSequenceGuesses.TryGetValue(mutation.ID, out var guessedComplementSequence);
                var candidateArr = new char[mutationSequence.Length];
                var complementArr = new char[mutationSequence.Length];
                for (var i = 0; i < mutationSequence.Length; i++)
                {
                    candidateArr[i] = '?';
                    complementArr[i] = '?';
                }

                if (discovered)
                {
                    sequence = mutationSequence;
                    for (var i = 0; i < mutationSequence.Length; i++)
                    {
                        candidateArr[i] = mutationSequence[i];
                        complementArr[i] = GeneticMutationPrototype.GetComplement(mutationSequence[i]);
                    }
                }
                else
                {
                    foreach (var pos in knownPositions)
                    {
                        if (pos < mutationSequence.Length)
                        {
                            candidateArr[pos] = mutationSequence[pos];
                            complementArr[pos] = GeneticMutationPrototype.GetComplement(mutationSequence[pos]);
                        }
                    }

                    if (guessedSequence != null &&
                        guessedSequence.Length == mutationSequence.Length &&
                        IsCandidateStrand(guessedSequence, mutationSequence.Length))
                    {
                        for (var i = 0; i < mutationSequence.Length; i++)
                        {
                            if (!knownPositions.Contains(i))
                                candidateArr[i] = guessedSequence[i];
                        }
                    }

                    if (guessedComplementSequence != null &&
                        guessedComplementSequence.Length == mutationSequence.Length &&
                        IsCandidateStrand(guessedComplementSequence, mutationSequence.Length))
                    {
                        for (var i = 0; i < mutationSequence.Length; i++)
                        {
                            if (!knownPositions.Contains(i))
                                complementArr[i] = guessedComplementSequence[i];
                        }
                    }

                    if (genome.HalfKnownPositions.TryGetValue(mutation.ID, out var halfKnownPositions))
                    {
                        foreach (var pos in halfKnownPositions)
                        {
                            if (pos < mutationSequence.Length)
                                candidateArr[pos] = mutationSequence[pos];
                        }
                    }
                }

                var candidateSequence = new string(candidateArr);
                var candidateComplementSequence = new string(complementArr);

                entries.Add(new GeneticsMutationUiEntry(
                    mutation.ID,
                    discovered ? Loc.GetString(mutation.Name) : Loc.GetString("genetics-console-unknown-gene", ("index", ++index)),
                    discovered ? Loc.GetString(mutation.Description) : Loc.GetString("genetics-console-unknown-description"),
                    sequence,
                    candidateSequence,
                    candidateComplementSequence,
                    progress,
                    discovered,
                    active,
                    mutation.Instability,
                    knownPositions,
                    genome.HalfKnownPositions.TryGetValue(mutation.ID, out var halfPositions)
                        ? halfPositions
                        : new HashSet<int>()));
            }

        }

        return new GeneticsConsoleBoundUserInterfaceState(
            true,
            true,
            MetaData(subject).EntityName,
            instability,
            health,
            maxHealth.Int(),
            cellularDamage,
            MaxGeneticDamage,
            canHumanizeSubject,
            isHumanoidSubject,
            component.LastResult,
            entries,
            GetMemoryEntries(component),
            component.MaxMemorySlots,
            (int) Math.Ceiling(component.JokerCooldownRemaining),
            component.StoredEnzymeProfile?.Name,
            GetStoredEnzymeDetails(component));
    }

    private static bool IsCandidateStrand(string sequence, int expectedLength)
    {
        return sequence.Length == expectedLength &&
               sequence.All(nucleotide =>
                   char.ToUpperInvariant(nucleotide) is 'A' or 'T' or 'G' or 'C' or '?');
    }

    private bool IsMonkey(EntityUid subject)
    {
        var prototypeId = MetaData(subject).EntityPrototype?.ID;
        return prototypeId == "MobMonkey" || prototypeId?.StartsWith("MobMonkey", StringComparison.Ordinal) == true;
    }

    private List<GeneticsMemoryUiEntry> GetMemoryEntries(GeneticsConsoleComponent component)
    {
        var entries = new List<GeneticsMemoryUiEntry>();
        foreach (var mutationId in component.MemoryMutations)
        {
            if (_prototypeManager.TryIndex<GeneticMutationPrototype>(mutationId, out var mutation))
                entries.Add(new GeneticsMemoryUiEntry(
                    mutation.ID,
                    Loc.GetString(mutation.Name),
                    Loc.GetString(mutation.Description),
                    mutation.Instability));
        }

        return entries;
    }

    private string? GetStoredEnzymeDetails(GeneticsConsoleComponent component)
    {
        if (component.StoredEnzymeProfile is not { } profile)
            return null;

        return Loc.GetString(
            "genetics-console-enzymes-stored",
            ("name", profile.Name),
            ("species", _humanoidProfile.GetSpeciesRepresentation(profile.Species)),
            ("age", profile.Age),
            ("gender", profile.Gender.ToString()));
    }
}
