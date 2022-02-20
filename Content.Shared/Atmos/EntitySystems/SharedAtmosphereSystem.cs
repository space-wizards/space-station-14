using System.Collections.Generic;
using Content.Shared.Atmos.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Atmos.EntitySystems
{
    public abstract class SharedAtmosphereSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        protected readonly GasPrototype[] GasPrototypes = new GasPrototype[Atmospherics.TotalNumberOfGases];

        private readonly SpriteSpecifier?[] _gasOverlays = new SpriteSpecifier[Atmospherics.TotalNumberOfGases];

        public override void Initialize()
        {
            base.Initialize();

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gasPrototype = _prototypeManager.Index<GasPrototype>(i.ToString());
                GasPrototypes[i] = gasPrototype;

                if(string.IsNullOrEmpty(gasPrototype.GasOverlaySprite) && !string.IsNullOrEmpty(gasPrototype.GasOverlayTexture))
                    _gasOverlays[i] = new SpriteSpecifier.Texture(new ResourcePath(gasPrototype.GasOverlayTexture));

                if(!string.IsNullOrEmpty(gasPrototype.GasOverlaySprite) && !string.IsNullOrEmpty(gasPrototype.GasOverlayState))
                    _gasOverlays[i] = new SpriteSpecifier.Rsi(new ResourcePath(gasPrototype.GasOverlaySprite), gasPrototype.GasOverlayState);
            }
        }

        public GasPrototype GetGas(int gasId) => GasPrototypes[gasId];

        public GasPrototype GetGas(Gas gasId) => GasPrototypes[(int) gasId];

        public IEnumerable<GasPrototype> Gases => GasPrototypes;

        public SpriteSpecifier? GetOverlay(int overlayId) => _gasOverlays[overlayId];
    }
}
