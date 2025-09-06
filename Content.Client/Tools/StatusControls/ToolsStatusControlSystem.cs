using Content.Client.Items;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;

namespace Content.Client.Tools;

public sealed class ToolsStatusControlSystem : EntitySystem
{
    [Dependency] private readonly SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<WelderComponent>(ent => new WelderStatusControl(ent, EntityManager, _tool));
        Subs.ItemStatus<MultipleToolComponent>(ent => new MultipleToolStatusControl(ent));
    }
}
