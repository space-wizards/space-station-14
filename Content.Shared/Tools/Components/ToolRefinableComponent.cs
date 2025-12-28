using Content.Shared.Storage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Shared.Tools.Components;

/// <summary>
/// Used for something that can be refined by welder.
/// For example, glass shard can be refined to glass sheet.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ToolRefinableSystem))]
public sealed partial class ToolRefinableComponent : Component
{
    /// <summary>
    /// The items created when the item is refined.
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> RefineResult = new();

    /// <summary>
    /// The amount of time it takes to refine a given item.
    /// </summary>
    [DataField]
    public TimeSpan RefineTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The amount of fuel it takes to refine a given item.
    /// </summary>
    [DataField]
    public float RefineFuel = 3f;

    /// <summary>
    /// The tool type needed in order to refine this item.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> QualityNeeded = "Welding";

    /// <summary>
    /// Sound that will be played after refine process finished.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Refine verb text. If there is no text, verb will not be added.
    /// </summary>
    [DataField]
    public LocId? VerbText;

    /// <summary>
    /// Icon for refine verb.
    /// </summary>
    [DataField]
    public SpriteSpecifier? VerbIcon;

    /// <summary>
    /// Default tooltip text for refine verb.
    /// </summary>
    [DataField]
    public LocId? VerbDefaultTooltip;

    /// <summary>
    /// Verb tooltip in case currently used tool
    /// is missing required (<see cref="QualityNeeded"/>) quality.
    /// </summary>
    [DataField]
    public LocId? ToolMissingQualityTooltip;

    /// <summary>
    /// Popup text to display for action performer
    /// on refine action performed (end of DoAfter).
    /// </summary>
    [DataField]
    public LocId? PopupForUser;

    /// <summary>
    /// Popup text to display for players near action performer
    /// on action performed (end of DoAfter).
    /// </summary>
    [DataField]
    public LocId? PopupForOther;

    /// <summary>
    /// Is usage of 'caution' type popup required for <see cref="PopupForUser"/> and <see cref="PopupForOther"/>,
    /// or small popup will suffice.
    /// </summary>
    [DataField]
    public bool IsUsingCautionPopup;
}
