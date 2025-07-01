using Content.Server.Body.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;

namespace Content.Server.Body.Systems;

public sealed class BloodstreamSystem : SharedBloodstreamSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, BeingGibbedEvent>(OnBeingGibbed);
    }

    private void OnBeingGibbed(Entity<BloodstreamComponent> ent, ref BeingGibbedEvent args)
    {
        SpillAllSolutions(ent, ent);
    }

}
