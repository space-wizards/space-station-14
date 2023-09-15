// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Photocopier;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ButtScan;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState]
public sealed partial class ButtScanComponent : Component, IPhotocopyableComponent
{
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("buttTexturePath")]
    public string ButtTexturePath = "/Textures/SS220/Interface/Butts/human.png";

    public IPhotocopiedComponentData GetPhotocopiedData()
    {
        return new ButtScanPhotocopiedData()
        {
            ButtTexturePath = ButtTexturePath
        };
    }
}

[Serializable]
public sealed class ButtScanPhotocopiedData : IPhotocopiedComponentData
{
    public string? ButtTexturePath;
    public void RestoreFromData(EntityUid uid, Component someComponent)
    {
        if (someComponent is not ButtScanComponent buttScanComponent)
            return;

        if (ButtTexturePath is not null)
        {
            var entSys = IoCManager.Resolve<IEntityManager>();
            var changed = ButtTexturePath != buttScanComponent.ButtTexturePath;
            buttScanComponent.ButtTexturePath = ButtTexturePath;
            if (changed)
                entSys.Dirty(buttScanComponent);
        }
    }
}
