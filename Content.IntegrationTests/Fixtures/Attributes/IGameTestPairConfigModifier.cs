#nullable enable
using System.Collections.Generic;

namespace Content.IntegrationTests.Fixtures.Attributes;

/// <summary>
///     Interface used for <see cref="GameTest"/> pair configuration attributes.
///     This allows such attributes to modify the pair settings, and also describe what parts of pairs they modify
///     so odd configuration choices can be spotted.
/// </summary>
public interface IGameTestPairConfigModifier
{
    bool Exclusive { get; }

    void ApplyToPairSettings(GameTest test, ref PoolSettings settings);
}

