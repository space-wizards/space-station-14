#nullable enable
using Content.Server.Atmos;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Atmos
{
    public partial class GridAtmosphereComponentData
    {
        public struct IntermediateTileAtmosphere
        {
            public readonly Vector2i Indices;
            public readonly GasMixture GasMixture;

            public IntermediateTileAtmosphere(Vector2i indices, GasMixture gasMixture)
            {
                Indices = indices;
                GasMixture = gasMixture;
            }
        }
    }
}
