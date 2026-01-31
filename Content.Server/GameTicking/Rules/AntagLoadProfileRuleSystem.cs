using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Humanoid;
using Content.Server.Preferences.Managers;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed class AntagLoadProfileRuleSystem : GameRuleSystem<AntagLoadProfileRuleComponent>
{
    [Dependency] private readonly HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagLoadProfileRuleComponent, AntagSelectEntityEvent>(OnSelectEntity);
    }

    private void OnSelectEntity(Entity<AntagLoadProfileRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Handled)
            return;

        var profile = args.Session != null
            ? _prefs.GetPreferences(args.Session.UserId).SelectedCharacter as HumanoidCharacterProfile
            : HumanoidCharacterProfile.RandomWithSpecies();


        if (profile?.Species is not { } speciesId || !_proto.Resolve(speciesId, out var species))
        {
            species = _proto.Index<SpeciesPrototype>(HumanoidCharacterProfile.DefaultSpecies);
        }

        if (ent.Comp.SpeciesOverride != null
            && (ent.Comp.SpeciesOverrideBlacklist?.Contains(new ProtoId<SpeciesPrototype>(species.ID)) ?? false))
        {
            species = _proto.Index(ent.Comp.SpeciesOverride.Value);
        }

        args.Entity = Spawn(species.Prototype);
        if (profile?.WithSpecies(species.ID) is { } humanoidProfile)
        {
            _visualBody.ApplyProfileTo(args.Entity.Value, humanoidProfile);
            _humanoidProfile.ApplyProfileTo(args.Entity.Value, humanoidProfile);
        }
    }
}
