using Content.Shared.Atmos.Visuals;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public sealed class AtmosPlaqueComponent : Component, IMapInit
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public override string Name => "AtmosPlaque";

        [DataField("plaqueType")]
        private PlaqueType _type = PlaqueType.Unset;

        [ViewVariables(VVAccess.ReadWrite)]
        public PlaqueType Type
        {
            get => _type;
            set
            {
                _type = value;
                UpdateSign();
            }
        }

        public void MapInit()
        {
            var random = IoCManager.Resolve<IRobustRandom>();
            var rand = random.Next(100);
            // Let's not pat ourselves on the back too hard.
            // 1% chance of zumos
            if (rand == 0) Type = PlaqueType.Zumos;
            // 9% FEA
            else if (rand <= 10) Type = PlaqueType.Fea;
            // 45% ZAS
            else if (rand <= 55) Type = PlaqueType.Zas;
            // 45% LINDA
            else Type = PlaqueType.Linda;
        }

        protected override void Startup()
        {
            base.Startup();

            UpdateSign();
        }

        private void UpdateSign()
        {
            if (!Running)
            {
                return;
            }

            var metaData = _entMan.GetComponent<MetaDataComponent>(Owner);

            var val = _type switch
            {
                PlaqueType.Zumos =>
                    "This plaque commemorates the rise of the Atmos ZUM division. May they carry the torch that the Atmos ZAS, LINDA and FEA divisions left behind.",
                PlaqueType.Fea =>
                    "This plaque commemorates the fall of the Atmos FEA division. For all the charred, dizzy, and brittle men who have died in its hands.",
                PlaqueType.Linda =>
                    "This plaque commemorates the fall of the Atmos LINDA division. For all the charred, dizzy, and brittle men who have died in its hands.",
                PlaqueType.Zas =>
                    "This plaque commemorates the fall of the Atmos ZAS division. For all the charred, dizzy, and brittle men who have died in its hands.",
                PlaqueType.Unset => "Uhm",
                _ => "Uhm",
            };

            metaData.EntityDescription = val;

            var val1 = _type switch
            {
                PlaqueType.Zumos =>
                    "ZUM Atmospherics Division plaque",
                PlaqueType.Fea =>
                    "FEA Atmospherics Division plaque",
                PlaqueType.Linda =>
                    "LINDA Atmospherics Division plaque",
                PlaqueType.Zas =>
                    "ZAS Atmospherics Division plaque",
                PlaqueType.Unset => "Uhm",
                _ => "Uhm",
            };

            metaData.EntityName = val1;

            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                var state = _type == PlaqueType.Zumos ? "zumosplaque" : "atmosplaque";

                appearance.SetData(AtmosPlaqueVisuals.State, state);
            }
        }

        public enum PlaqueType
        {
            Unset = 0,
            Zumos,
            Fea,
            Linda,
            Zas
        }
    }
}

// If you get the ZUM plaque it means your round will be blessed with good engineering luck.
