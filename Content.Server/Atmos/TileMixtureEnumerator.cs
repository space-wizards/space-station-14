using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Atmos;

public struct TileMixtureEnumerator
{
    public readonly TileAtmosphere?[] Tiles;
    public int Index = 0;

    public static readonly TileMixtureEnumerator Empty = new(Array.Empty<TileAtmosphere>());

    internal TileMixtureEnumerator(TileAtmosphere?[] tiles)
    {
        Tiles = tiles;
    }

    public bool MoveNext([NotNullWhen(true)] out GasMixture? mix)
    {
        while (Index < Tiles.Length)
        {
            mix = Tiles[Index++]?.Air;
            if (mix != null)
                return true;
        }

        mix = null;
        return false;
    }
}
