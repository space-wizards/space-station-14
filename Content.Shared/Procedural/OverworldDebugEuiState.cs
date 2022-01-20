using System;
using Content.Shared.Eui;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Procedural;

[Serializable, NetSerializable]
public sealed class OverworldDebugEuiState : EuiStateBase
{
    public DebugChunkData[][] ChunkData { get; }

    public Vector2 PlayerPosition { get; }

    public OverworldDebugEuiState(DebugChunkData[][] chunkData)
    {
        ChunkData = chunkData;
    }

}

[NetSerializable, Serializable]
public class OverworldDebugCloseMessage : EuiMessageBase { }

[NetSerializable, Serializable]
public class OverworldDebugSettingsMessage : EuiMessageBase
{
    public int Zoom = 8;
}

[NetSerializable, Serializable]
public record struct DebugChunkData
{
    public float Density;
    public float Radiation;
    public float Wrecks;
    public float Temperature;
    public bool Clipped;
    public bool Loaded;
    public bool Radstorming;
    public char BiomeSymbol;
}

