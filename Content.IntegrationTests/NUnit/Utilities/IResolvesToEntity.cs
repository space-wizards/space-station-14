using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.NUnit.Utilities;

/// <summary>
///     An interface for objects that NUnit constraints should treat as a sided entity.
/// </summary>
public interface IResolvesToEntity
{
    /// <summary>
    ///     The server-sided entity, if any.
    /// </summary>
    EntityUid? SEntity { get; }
    /// <summary>
    ///     The client-sided entity, if any.
    /// </summary>
    EntityUid? CEntity { get; }
}

