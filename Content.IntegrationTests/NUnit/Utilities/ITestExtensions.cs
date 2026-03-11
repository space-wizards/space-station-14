using Content.IntegrationTests.Fixtures;
using NUnit.Framework.Interfaces;

namespace Content.IntegrationTests.NUnit.Utilities;

public static class ITestExtensions
{
    extension<T>(T test)
        where T: ITest
    {
        /// <summary>
        ///     Ensures the given fixture is a <see cref="GameTest"/>, and if not gives a nice error message.
        /// </summary>
        /// <param name="callingType">The caller's type, usually an attribute.</param>
        /// <param name="gt">The <see cref="GameTest"/>.</param>
        /// <exception cref="NotSupportedException">Thrown when the given test isn't a <see cref="GameTest"/></exception>
        public void EnsureFixtureIsGameTest(Type callingType, out GameTest gt)
        {
            if (test.Fixture is not GameTest gameTest)
            {
                throw new NotSupportedException(
                    $"The fixture {test.Fixture?.GetType()} needs to be a GameTest for {callingType.Name} to work.");
            }

            gt = gameTest;
        }
    }
}
