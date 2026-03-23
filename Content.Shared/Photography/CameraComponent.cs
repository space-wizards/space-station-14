using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Photography;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CameraComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ImageRes = 1f;

    [DataField, AutoNetworkedField]
    public int TargetWidth = 3;

    [DataField, AutoNetworkedField]
    public int MaxPhotos = 10;

    [DataField, AutoNetworkedField]
    public int CurrentPhotos = 10;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan NextPhotoTime = TimeSpan.Zero;

    // <summary>
    /// The sound made when printing occurs
    /// </summary>
    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");
}
