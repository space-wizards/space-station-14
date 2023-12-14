using System.Linq;
using Content.Shared.BarSign;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.BarSign.Systems
{
    public sealed class BarSignSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<BarSignComponent, MapInitEvent>(OnMapInit);
        }

        private void OnMapInit(EntityUid uid, BarSignComponent component, MapInitEvent args)
        {
            if (component.Current != null)
                return;

            var prototypes = _prototypeManager
                .EnumeratePrototypes<BarSignPrototype>()
                .Where(p => !p.Hidden)
                .ToList();

            var newPrototype = _random.Pick(prototypes);

            var meta = Comp<MetaDataComponent>(uid);
            var name = newPrototype.Name != string.Empty ? newPrototype.Name : "barsign-component-name";
            _metaData.SetEntityName(uid, Loc.GetString(name), meta);
            _metaData.SetEntityDescription(uid, Loc.GetString(newPrototype.Description), meta);

            component.Current = newPrototype.ID;
            Dirty(component);
        }
    }
}
