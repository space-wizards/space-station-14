using Robust.Shared.Audio;

﻿namespace Content.Server.Roles;

/// <summary>
/// Plays a greeting sound when the role is added.
/// </summary>
[RegisterComponent]
public sealed partial class RoleSoundComponent : Component
{
    /// <summary>
    /// Greeting sound to play.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? Sound;
}
