using Robust.Shared.Serialization;

namespace Content.Shared.Body.Part;

[Serializable, NetSerializable]
public sealed class BodySkinComponentState : ComponentState
{
    public  List<SkinlayerData> SkinLayers;

    public BodySkinComponentState(BodySkinComponent part)
    {
        SkinLayers = part.SkinLayers;
    }
}
