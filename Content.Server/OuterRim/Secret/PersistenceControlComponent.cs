using JetBrains.Annotations;

namespace Content.Server.OuterRim.Secret;

/// <summary>
/// This is used for managing persistent structures, namely cleaning them up to avoid game cheesing.
/// No, you're not going to find what uses this here. Nice try.
/// </summary>
[RegisterComponent, PublicAPI]
public sealed class PersistenceControlComponent : Component
{
    [DataField("mode")] public PersistenceControlMode Mode = PersistenceControlMode.Delete;
}

[PublicAPI]
public enum PersistenceControlMode
{
    Delete,
    Event,
}
