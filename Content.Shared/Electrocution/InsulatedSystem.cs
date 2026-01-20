using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Verbs;

namespace Content.Shared.Electrocution;

public sealed class InsulatedSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InsulatedComponent, GetVerbsEvent<ExamineVerb>>(OnDetailedExamine);
    }

    private void OnDetailedExamine(EntityUid ent, InsulatedComponent component, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!HasComp<ClothingComponent>(ent))
            return;

        var iconTexture = "/Textures/Interface/VerbIcons/zap.svg.192dpi.png";

        _examine.AddHoverExamineVerb(args,
            component,
            Loc.GetString("identity-block-examinable-verb-text"),
            Loc.GetString("identity-block-examinable-verb-text-message"),
            iconTexture
        );
    }
}
