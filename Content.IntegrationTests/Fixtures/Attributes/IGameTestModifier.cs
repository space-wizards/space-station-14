namespace Content.IntegrationTests.Fixtures.Attributes;

/// <summary>
///     Marks an attribute as a modifier for <see cref="GameTest"/> fixtures.
///     These attributes can be applied to both test methods and fixtures.
/// </summary>
/// <remarks>
///     GameTest modifiers are <b>encouraged</b> to also implement IApplyToTest and add properties to the test
///     indicating their presence.
/// </remarks>
public interface IGameTestModifier
{
    /// <summary>
    ///     Method called by GameTest on itself when applying <see cref="GameTest"/> modifiers.
    /// </summary>
    /// <param name="test">The test being modified</param>
    /// <returns>Async task to await.</returns>
    Task ApplyToTest(GameTest test);
}

