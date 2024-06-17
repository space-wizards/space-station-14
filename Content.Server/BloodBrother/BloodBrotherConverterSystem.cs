using Content.Server.Flash;
using Content.Server.GameTicking.Rules;
using Content.Shared.BloodBrother.Components;

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
        if (!TryComp<SharedBloodBrotherComponent>(args.User, out var self))
            return;
        if (TryComp<SharedBloodBrotherComponent>(args.Target, out var target))
            _bbrule.MakeBloodBrother(args.Target, self.TeamID, target);

        _bbrule.MakeBloodBrother(args.Target, self.TeamID);
    }
}
