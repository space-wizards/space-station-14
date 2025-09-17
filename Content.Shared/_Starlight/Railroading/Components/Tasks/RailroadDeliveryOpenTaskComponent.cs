using Robust.Shared.Utility;

namespace Content.Shared._Starlight.Railroading;

[RegisterComponent]
public sealed partial class RailroadDeliveryOpenTaskComponent : Component
{
    [DataField]
    public string Message = "rail-open-delivery-task";

    /// <summary>
    /// Pieces of mail to open before task completion
    /// </summary>
    [DataField]
    public int Amount = 2;

    public int AmountOpened = 0;

    [DataField]
    public SpriteSpecifier Icon;
}