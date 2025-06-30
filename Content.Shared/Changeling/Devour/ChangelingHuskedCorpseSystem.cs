using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Devour;

public sealed class ChangelingHuskedCorpseSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingHuskedCorpseComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingHuskedCorpseComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMapInit(Entity<ChangelingHuskedCorpseComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid)
            || !_prototype.TryIndex(humanoid.Species, out var speciesPrototype))
            return;

        _adminLogger.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player} was successfully consumed by a changeling and their body was husked");

        RemComp(ent, humanoid);
        var newComp = EnsureComp<HumanoidAppearanceComponent>(ent);
        newComp.Species = speciesPrototype;
        _metaSystem.SetEntityName(ent, Loc.GetString("changeling-unidentified-husked-corpse"));

        _humanoidSystem.SetGender((ent, humanoid), Gender.Epicene);

        var unrevivable = EnsureComp<UnrevivableComponent>(ent);
        unrevivable.Analyzable = false;
        unrevivable.ReasonMessage = "changeling-defibrillator-failure";
    }

    private void OnExamined(Entity<ChangelingHuskedCorpseComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("changeling-husked-corpse", ("target", Identity.Entity(ent, EntityManager))));
    }
}
