using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Robust.Shared.Prototypes; //just rider things :3

namespace Content.Server.Traits.Assorted;


public sealed partial class HemophiliaSystem : EntitySystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BloodstreamComponent, BleedStackReduceEvent>(SentryWasHere);
    }

    private void SentryWasHere(Entity<BloodstreamComponent> ent, ref BleedStackReduceEvent args)
    {
        if (TryComp<HemophiliaComponent>(ent, out var hemophiliaComponent))
        {
            args.BleedStackReductionAmount = hemophiliaComponent.HemophiliacBleedReductionAmount;
        }
    }
}

