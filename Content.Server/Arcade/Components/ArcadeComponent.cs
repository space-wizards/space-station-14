using Content.Server.Arcade.EntitySystems;
using Robust.Shared.Audio;

namespace Content.Server.Arcade.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, Access(typeof(ArcadeSystem))]
public sealed partial class ArcadeComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Player = null;

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public SoundSpecifier WinSound = new SoundPathSpecifier("/Audio/Effects/Arcade/win.ogg");

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public SoundSpecifier LossSound = new SoundPathSpecifier("/Audio/Effects/Arcade/gameover.ogg");

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public SoundSpecifier NewGameSound = new SoundPathSpecifier("/Audio/Effects/Arcade/newgame.ogg");
}
