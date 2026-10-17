using System.Linq;
using Content.Shared.Body;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidProfileSystem : EntitySystem
{
    [Dependency] private GrammarSystem _grammar = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidProfileComponent, ExaminedEvent>(OnExamined);
    }

    public void ApplyProfileTo(Entity<HumanoidProfileComponent?> ent, HumanoidCharacterProfile profile)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Gender = profile.Gender;
        ent.Comp.Age = profile.Age;
        ent.Comp.Species = profile.Species;
        ent.Comp.Voice = profile.Voice;
        ent.Comp.Sex = profile.Sex;
        Dirty(ent);

        var voiceChanged = new VoiceChangedEvent(ent.Comp.Voice, profile.Voice);
        RaiseLocalEvent(ent, ref voiceChanged);

        if (TryComp<GrammarComponent>(ent, out var grammar))
        {
            _grammar.SetGender((ent, grammar), profile.Gender);
        }
    }

    /// <summary>
    /// Apply a new <see cref="EmoteSoundsPrototype"/> to an entity, updating any emote sounds.
    /// </summary>
    /// <param name="ent">The entity to be changed.</param>
    /// <param name="voice">ID of the prototype.</param>
    public void ApplyVoice(Entity<HumanoidProfileComponent?> ent, ProtoId<EmoteSoundsPrototype> voice)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Voice = voice;
        Dirty(ent);

        var voiceChanged = new VoiceChangedEvent(ent.Comp.Voice, voice);
        RaiseLocalEvent(ent, ref voiceChanged);
    }

    /// <summary>
    /// Apply a new <see cref="Sex"/> to an entity, updating visuals.
    /// </summary>
    /// <param name="ent">The entity to be changed.</param>
    /// <param name="sex">The <see cref="Sex"/> to set the entity at.</param>
    public void ApplySex(Entity<HumanoidProfileComponent?> ent, Sex sex)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Sex = sex;
        Dirty(ent);

        // Update visuals of clothing & body
        var enumerator = _inventory.GetSlotEnumerator((ent, null));
        while (enumerator.NextItem(out var item, out _))
        {
            _item.VisualsChanged(item);
        }

        if (_visualBody.TryGatherMarkingsData(ent.Owner, null, out var profiles, out _, out _))
        {
            var changedProfiles = profiles.ToDictionary(pair => pair.Key,
                pair => pair.Value with { Sex = sex });
            _visualBody.ApplyProfiles(ent, changedProfiles);
        }
    }

    private void OnExamined(Entity<HumanoidProfileComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Examinable)
            return;

        var identity = Identity.Entity(ent, EntityManager);
        var species = GetSpeciesRepresentation(ent.Comp.Species).ToLower();
        var age = GetAgeRepresentation(ent.Comp.Species, ent.Comp.Age);

        args.PushText(Loc.GetString("humanoid-appearance-component-examine", ("user", identity), ("age", age), ("species", species)));
    }

    /// <summary>
    /// Takes ID of the species prototype, returns UI-friendly name of the species.
    /// </summary>
    public string GetSpeciesRepresentation(ProtoId<SpeciesPrototype> species)
    {
        if (ProtoMan.TryIndex(species, out var speciesPrototype))
            return Loc.GetString(speciesPrototype.Name);

        Log.Error("Tried to get representation of unknown species: {speciesId}");
        return Loc.GetString("humanoid-appearance-component-unknown-species");
    }

    /// <summary>
    /// Takes ID of the species prototype and an age, returns an approximate description
    /// </summary>
    public string GetAgeRepresentation(ProtoId<SpeciesPrototype> species, int age)
    {
        if (!ProtoMan.TryIndex(species, out var speciesPrototype))
        {
            Log.Error("Tried to get age representation of species that couldn't be indexed: " + species);
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.YoungAge)
        {
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.OldAge)
        {
            return Loc.GetString("identity-age-middle-aged");
        }

        return Loc.GetString("identity-age-old");
    }
}
