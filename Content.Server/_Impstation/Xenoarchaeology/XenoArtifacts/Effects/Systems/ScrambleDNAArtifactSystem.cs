using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Preferences;
using Content.Server.Humanoid;
using System.Linq;
using Robust.Shared.Random;
using Content.Shared.Humanoid;
using Content.Shared.Forensics.Components;
using Content.Shared.Forensics;
using Content.Server.Forensics;
using Content.Server.IdentityManagement;



namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class ScrambleDNAArtifactSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly ForensicsSystem _forensicsSystem = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;



    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ScrambleDNAArtifactComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnActivated(EntityUid uid, ScrambleDNAArtifactComponent component, ArtifactActivatedEvent args)
    {
        // Get all entities in range, and the person who activated the artifact even if they are not within range
        var ents = _lookup.GetEntitiesInRange(uid, component.Range);
        if (args.Activator != null)
            ents.Add(args.Activator.Value);

        //Extract the people who can be scrambled
        var possibleVictims = new List<EntityUid>();
        foreach (var ent in ents)
        {
            if (HasComp<HumanoidAppearanceComponent>(ent))
                possibleVictims.Add(ent);
        }

        //Select random targets to scramble DNA
        var numScrambled = 0;
        while (numScrambled < component.Count){
            var targetIndex = _random.Next(0, possibleVictims.Count);
            var target = possibleVictims.ElementAt(targetIndex);
            possibleVictims.Remove(target);

            ScrambleTargetDNA(target, component);
            numScrambled ++;
        }
    }

    private void ScrambleTargetDNA(EntityUid target, ScrambleDNAArtifactComponent component)
    {
        if (TryComp<HumanoidAppearanceComponent>(target, out var humanoid))
        {
            var newProfile = (HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species));
            _humanoidAppearance.LoadProfile(target, newProfile, humanoid);
            _metaData.SetEntityName(target, newProfile.Name, raiseEvents: false);
            if (TryComp<DnaComponent>(target, out var dna))
            {
                dna.DNA = _forensicsSystem.GenerateDNA();

                var ev = new GenerateDnaEvent { Owner = target, DNA = dna.DNA };
                RaiseLocalEvent(target, ref ev);
            }
            if (TryComp<FingerprintComponent>(target, out var fingerprint))
            {
                fingerprint.Fingerprint = _forensicsSystem.GenerateFingerprint();
            }
            _identity.QueueIdentityUpdate(target); // manually queue identity update since we don't raise the event
        }
    }

}
