using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Equipment;

[RegisterComponent, Serializable, NetSerializable]
public sealed class SharedArtifactAnalyzerComponent : Component
{

}

[Serializable, NetSerializable]
public enum ArtifactAnaylzerUiKey : byte
{
    Key
}
