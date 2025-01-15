using Content.Shared.Changeling.Transform;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Devour;

public sealed class ChangelingHuskedCorpseSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly SharedChangelingTransformSystem _changelingTransformSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingHuskedCorpseComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ChangelingHuskedCorpseComponent, ExaminedEvent>(OnExamined);
    }


    private void OnInit(EntityUid uid, ChangelingHuskedCorpseComponent component, MapInitEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid)
            || !_prototype.TryIndex(humanoid.Species, out var speciesPrototype))
            return;
        var huskedBodyAppearance = Spawn(speciesPrototype.Prototype, MapCoordinates.Nullspace);
        _humanoidSystem.CloneAppearance(huskedBodyAppearance, uid);
        QueueDel(huskedBodyAppearance);
        _metaSystem.SetEntityName(uid, Loc.GetString("changeling-unidentified-husked-corpse"));
        _changelingTransformSystem.TransformGrammarSet(uid, Gender.Epicene);
    }

    private void OnExamined(EntityUid uid, ChangelingHuskedCorpseComponent comp, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("changeling-husked-corpse", ("target", Identity.Entity(uid, EntityManager))));
    }
}
