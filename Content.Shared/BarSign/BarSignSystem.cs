using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.BarSign;

public sealed class BarSignSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BarSignComponent, MapInitEvent>(OnMapInit);
        Subs.BuiEvents<BarSignComponent>(BarSignUiKey.Key,
            subs =>
        {
            subs.Event<SetBarSignMessage>(OnSetBarSignMessage);
        });
    }

    private void OnMapInit(Entity<BarSignComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Current != null)
            return;

        var newPrototype = _random.Pick(GetAllBarSigns(_prototypeManager));
        SetBarSign(ent, newPrototype);
    }

    private void OnSetBarSignMessage(Entity<BarSignComponent> ent, ref SetBarSignMessage args)
    {
        if (!_prototypeManager.TryIndex(args.Sign, out var signPrototype))
            return;

        SetBarSign(ent, signPrototype);
    }

    public void SetBarSign(Entity<BarSignComponent> ent, BarSignPrototype newPrototype)
    {
        var meta = MetaData(ent);
        var name = Loc.GetString(newPrototype.Name);
        _metaData.SetEntityName(ent, name, meta);
        _metaData.SetEntityDescription(ent, Loc.GetString(newPrototype.Description), meta);

        ent.Comp.Current = newPrototype.ID;
        Dirty(ent);
    }

    public static List<BarSignPrototype> GetAllBarSigns(IPrototypeManager prototypeManager)
    {
        return prototypeManager
            .EnumeratePrototypes<BarSignPrototype>()
            .Where(p => !p.Hidden)
            .ToList();
    }
}
