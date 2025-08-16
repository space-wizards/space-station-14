using Robust.Shared.Serialization;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Paper;

/// <summary>
///     Set of required information to draw a stamp in UIs, where
///     representing the state of the stamp at the point in time
///     when it was applied to a paper. These fields mirror the
///     equivalent in the component.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial struct StampDisplayInfo
{
    StampDisplayInfo(string s)
    {
        StampedName = s;
    }

    [DataField]
    public string StampedName;

    [DataField]
    public Color StampedColor;

    [DataField]
    public StampType StampedType;
};

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StampComponent : Component
{
    /// <summary>
    /// The loc string name that will be stamped to the piece of paper on examine.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string StampedName = "stamp-component-stamped-name-default";

    /// <summary>
    /// The sprite state of the stamp to display on the paper from paper Sprite path.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string StampState = "paper_stamp-generic";

    /// <summary>
    /// The type of stamp this will apply.
    /// </summary>
    [DataField, AutoNetworkedField]
    public StampType StampType = StampType.Stamp;

    /// <summary>
    /// The color of the ink used by the stamp in UIs
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color StampedColor = Color.FromHex("#BB3232"); // StyleNano.DangerousRedFore

    /// <summary>
    /// Should the stamped paper be uneditable after this stamp is applied?
    /// Can still be modified by things with the WriteIgnoreStamps tag.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ProtectAfterStamp = true;

    /// <summary>
    /// The sound when stamp stamped
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound = null;
}

[Serializable, NetSerializable]
public enum StampType : byte
{
    Stamp,
    Signature // Means the stamp is borderless and uses the entity's name instead of StampedName
}
