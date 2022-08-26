using Content.Server._00OuterRim.Worldgen.Euis;
using Content.Shared.Procedural;
using Robust.Server.Player;

namespace Content.Server._00OuterRim.Worldgen.Systems.Overworld;

public partial class WorldChunkSystem
{
    private readonly Dictionary<IPlayerSession, OverworldDebugEui> _openUis = new();

    public void OpenEui(IPlayerSession session)
    {
        if(_openUis.ContainsKey(session))
            CloseEui(session);

        var eui = _openUis[session] = new OverworldDebugEui();
        _euiManager.OpenEui(eui, session);
        eui.StateDirty();
    }

    public void CloseEui(IPlayerSession session)
    {
        if (!_openUis.ContainsKey(session)) return;

        _openUis.Remove(session, out var eui);

        eui?.Close();
    }

    public DebugChunkData[][] GetWorldDebugData(int width, int height, Vector2i topLeft)
    {
        var data = new DebugChunkData[height][];
        for (int y = 0; y < height; y++)
        {
            data[y] = new DebugChunkData[width];
            for (int x = 0; x < width; x++)
            {
                var chunk = topLeft + (x, y);
                data[y][x] = new DebugChunkData()
                {
                    Density = (int)(GetDensityValue(chunk) * 10),
                    Radiation = (int)(GetRadiationClipped(chunk) * 10),
                    Wrecks = (int)(GetWreckClipped(chunk) * 10),
                    Temperature = (int)(GetTemperatureClipped(chunk) * 10),
                    Clipped = ShouldClipChunk(chunk),
                    Loaded = _currLoaded.Contains(chunk),
                    Radstorming = ShouldRadstorm(chunk),
                    BiomeSymbol = _chunks.ContainsKey(chunk) ? _chunks[chunk].Biome.Symbol : SelectBiome(chunk).Symbol,
                };
            }
        }

        return data;
    }
}
