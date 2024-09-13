using Content.Shared.Destructible;
using Content.Shared.Emag.Systems;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared.LawChips.Judge;

public abstract class SharedJudgeInterfaceSystem : EntitySystem
{
    [Dependency] SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

}
