using Content.Shared.Silicons.StationAi;
using JetBrains.Annotations;

namespace Content.Client.DeadSpace.StationAI.UI;

/// <summary>
///     Initializes a <see cref="AICameraList"/> and updates it when new server messages are received.
/// </summary>
[UsedImplicitly]
public sealed class AICameraListBoundUserInterface : BoundUserInterface
{
    public AICameraList? Window;

    public AICameraListBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();
        Window?.Close();
        EntityUid? gridUid = null;

        if (EntMan.TryGetComponent<TransformComponent>(Owner, out var xform))
        {
            gridUid = xform.GridUid;
        }

        Window = new AICameraList(gridUid, Owner);
        Window.OpenCentered();
        Window.OnClose += Close;
        Window.WarpToCamera += WindowOnWarpToCamera;
    }

    private void WindowOnWarpToCamera(NetEntity obj)
    {
        SendMessage(new EyeMoveToCam { Entity = EntMan.GetNetEntity(Owner), Uid = obj });
    }

    public void Update()
    {
        Window?.UpdateCameras();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        Window?.Parent?.RemoveChild(Window);
    }
}
