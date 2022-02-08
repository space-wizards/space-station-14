using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Shared.Camera;

[RegisterComponent]
[NetworkedComponent]
public class CameraRecoilComponent : Component
{
    public Vector2 CurrentKick { get; set; }
    public float LastKickTime { get; set; }

    /// <summary>
    ///     Basically I needed a way to chain this effect for the attack lunge animation. Sorry!
    /// </summary>
    public Vector2 BaseOffset { get; set; }
}
