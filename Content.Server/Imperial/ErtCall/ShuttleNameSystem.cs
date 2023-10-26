using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Content.Shared.Dataset;

namespace Content.Server.ErtCall
{

    public sealed class ShuttleNameSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        private string[] SuffixLetters => new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShuttleNameComponent, ComponentStartup>(OnComponentStartup);

        }

        public void OnComponentStartup(EntityUid uid, ShuttleNameComponent component, ComponentStartup args)
        {
            string res = "";
            res += component.CorpName;
            res += _random.Next(1, component.MaxCorpNumber);
            res += " ";
            res += component.SpecName;
            res += "-";
            res += _random.Next(1, component.MaxSpecNumber);
            if (component.SuffixName)
            {
                res += _random.Pick(SuffixLetters);
                res += _random.Pick(SuffixLetters);
            }
            if (component.NeedShipName)
            {
                _prototype.TryIndex<DatasetPrototype>(component.ShipNameFirst, out var firstName);
                _prototype.TryIndex<DatasetPrototype>(component.ShipNameLast, out var lasttName);
                if(firstName != null && lasttName != null)
                {
                    res += " \"";
                    res += _random.Pick(firstName.Values);
                    res += " ";
                    res += _random.Pick(lasttName.Values);
                    res += "\"";
                }
            }
            var metadata = MetaData(uid);
            metadata.EntityName = res;
        }
    }

}
