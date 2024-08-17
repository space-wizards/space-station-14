using Robust.Shared.Audio;

namespace Content.Server.SyndicateTeleporter;

[RegisterComponent]
public sealed partial class SyndicateTeleporterComponent : Component
{
    /// <summary>
    /// adds a random value to which you teleport, which is added to the guaranteed teleport value. from 0 to the set number. set 0 if you don't need randomness when teleporting
    /// </summary>
    [DataField]
    public int RandomDistanceValue = 4;
    /// <summary>
    /// this is the guaranteed number of tiles that you teleport to.
    /// </summary>
    [DataField]
    public float TeleportationValue = 4f;
    /// <summary>
    /// how many attempts do you have to teleport into the wall without a fatal outcome
    /// </summary>
    [DataField]
    public int SaveAttempts = 1;
    /// <summary>
    /// the distance to which you will be teleported when you teleport into a wall
    /// </summary>
    [DataField]
    public int SaveDistance = 3;

    [DataField("alarm"), AutoNetworkedField]
    public SoundSpecifier? AlarmSound = new SoundPathSpecifier("/Audio/Effects/beeps.ogg");


    public EntityUid UserComp;
    /// <summary>
    /// the number of seconds the player stays in the wall. (just so that he would realize that he almost died)
    /// </summary>
    [DataField]
    public float CorrectTime = 0.5f;
    public float Timer = 0; 

    public bool InWall = false;


}
