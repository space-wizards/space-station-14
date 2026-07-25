// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Objectives.Components;
using Robust.Shared.Random;

namespace Content.Server.DeadSpace.Objectives;

/// <summary>
/// Выбирает случайное описание из пула <see cref="RandomObjectiveDescriptionComponent"/> при выдаче цели.
/// </summary>
public sealed class RandomObjectiveDescriptionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomObjectiveDescriptionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }

    private void OnAfterAssign(Entity<RandomObjectiveDescriptionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        if (ent.Comp.Descriptions.Count == 0)
            return;

        var key = _random.Pick(ent.Comp.Descriptions);
        _metaData.SetEntityDescription(ent.Owner, Loc.GetString(key), args.Meta);
    }
}
