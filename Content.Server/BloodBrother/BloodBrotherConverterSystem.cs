using Content.Server.Flash;
using Content.Server.GameTicking.Rules;

namespace Content.Server.BloodBrother;

internal sealed class BloodBrotherConverterSystem : EntitySystem
{
    [Dependency] BloodBrotherRuleSystem _bbrule = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodBrotherConverterComponent, AfterFlashedEvent>(OnFlash);
    }

    private void OnFlash(EntityUid uid, BloodBrotherConverterComponent comp, ref AfterFlashedEvent args)
    {
        _bbrule.MakeBloodBrother(args.Target);
    }
}
