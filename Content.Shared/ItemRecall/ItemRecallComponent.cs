using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.ItemRecall;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedItemRecallSystem))]
public sealed partial class ItemRecallComponent : Component
{
    /// <summary>
    ///     Does this spell require Wizard Robes & Hat?
    /// </summary>
    [DataField]
    public bool RequiresClothes = true;

    [DataField]
    public LocId WhileMarkedName = "";

    [DataField]
    public LocId WhileMarkedDescription = "";

    [DataField]
    public SpriteSpecifier? WhileMarkedSprite;

    [ViewVariables]
    public EntityUid? MarkedEntity;

    /// <summary>
    ///     Does this spell require the user to speak?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequiresSpeech;

}
