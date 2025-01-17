using Robust.Shared.Serialization;

namespace Content.Shared.Crayon;

[Serializable, NetSerializable]
public sealed class CrayonOverlayUpdateEvent : EntityEventArgs
{
    public string State;
    public float Rotation;
    public Color Color;
    public bool PreviewMode;

    public CrayonOverlayUpdateEvent(string state, float rotation, Color color, bool previewMode)
    {
        State = state;
        Rotation = rotation;
        Color = color;
        PreviewMode = previewMode;
    }
}
