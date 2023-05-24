namespace Content.Server.Spaceshroom.Components;

[RegisterComponent]
[Access(typeof(SpaceshroomSystem))]
public sealed class SpaceshroomComponent : Component
{
    [DataField("mindropcount")]
    public int MinDropCount = 2;

    [DataField("maxdropcount")]
    public int MaxDropCount = 3;

    [DataField("dropradius")]
    public float DropRadius = 1.2f;

    [DataField("harvesttime")]
    public float HarvestTime = 2.0f;
}
