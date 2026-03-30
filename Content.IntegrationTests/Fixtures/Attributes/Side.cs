#nullable enable
namespace Content.IntegrationTests.Fixtures.Attributes;

/// <summary>
///     A flag enum representing a side of a testpair.
/// </summary>
[Flags]
public enum Side : byte
{
    /// <summary>
    ///     Bitflag representing the client side of a testpair.
    /// </summary>
    Client = 1,
    /// <summary>
    ///     Bitflag representing the server side of a testpair.
    /// </summary>
    Server = 2,

    /// <summary>
    ///     A value indicating no side was specified. You shouldn't use this outside of checking for it as an error.
    /// </summary>
    Neither = 0,
    /// <summary>
    ///     A value indicating both sides were specified.
    /// </summary>
    Both = Client | Server,
}
