using Content.Server.Teleportation;
using Content.Shared.Paper;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.Paper;

public sealed class PaperMakeQuantumSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperMakeQuantumComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<PaperMakeQuantumComponent> entity, ref MapInitEvent args)
    {
        RemCompDeferred<PaperQuantumComponent>(entity);
        if (_random.NextFloat() > entity.Comp.Chance)
            return;
        AddComp<SuperposedComponent>(entity.Owner);
        AddComp(entity.Owner, entity.Comp.PaperQuantum);
        AddComp(entity.Owner, entity.Comp.Explosive);
        if (TryComp(entity.Owner, out MetaDataComponent? metaComp))
        {
            if (entity.Comp.NewName is not null)
                _meta.SetEntityName(entity.Owner, entity.Comp.NewName, metaComp);
            if (entity.Comp.NewDesc is not null)
                _meta.SetEntityDescription(entity.Owner, entity.Comp.NewDesc, metaComp);
        }
    }
}
