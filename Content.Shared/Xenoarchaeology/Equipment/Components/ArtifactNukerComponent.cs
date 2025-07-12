namespace Content.Shared.Xenoarchaeology.Equipment.Components;

[RegisterComponent]
[Access(typeof(SharedArtifactNukerSystem))]
public sealed partial class ArtifactNukerComponent : Component
{
    #region Gameplay stuff
    /// <summary>
    ///     When true, activates nuked node.
    /// </summary>
    [DataField]
    public bool ActivateNode = true;
    #endregion

    #region LocIDs
    // Not a datafields because serialisation doesn't makes any sense?
    public string PopupNotArtifact = "";

    public string PopupZeroNodes = "";
    #endregion
}
