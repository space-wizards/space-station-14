using Content.Client.Chemistry.UI;
using Content.Client.Items;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Client.Chemistry.EntitySystems;

public sealed class HypospraySystem : SharedHypospraySystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<HyposprayComponent>(ent => new HyposprayStatusControl(ent, _solutionContainers));
    }
}
