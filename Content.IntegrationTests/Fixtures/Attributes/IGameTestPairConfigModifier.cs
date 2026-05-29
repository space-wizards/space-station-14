#nullable enable

namespace Content.IntegrationTests.Fixtures.Attributes;

/// <summary>
///     Interface used for <see cref="GameTest"/> pair configuration attributes.
///     This allows such attributes to modify the pair settings, and also describe what parts of pairs they modify
///     so odd configuration choices can be spotted.
/// </summary>
public interface IGameTestPairConfigModifier
{
    /// <summary>
    ///     Whether this modifier is exclusive and should conflict with other exclusive modifiers.
    ///     Essentially, fail immediately if other IGameTestPairConfigModifier attributes are present if this is set.
    /// </summary>
    bool Exclusive { get; }

    /// <summary>
    ///     Called when GameTest needs its <see cref="PoolSettings"/> modified by the modifier.
    /// </summary>
    /// <param name="test">The test we're applying to.</param>
    /// <param name="settings">The settings object to modify.</param>
    void ApplyToPairSettings(GameTest test, ref PoolSettings settings);
}

