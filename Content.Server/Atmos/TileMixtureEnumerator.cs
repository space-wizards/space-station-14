// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Atmos;

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
