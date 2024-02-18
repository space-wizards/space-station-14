using System.Collections.Generic;
using Content.Shared.Rounding;
using NUnit.Framework;

namespace Content.Tests.Shared.Utility
{
    [Parallelizable]
    [TestFixture]
    [TestOf(typeof(ContentHelpers))]
    public sealed class ContentHelpers_Test
    {
        public static readonly IEnumerable<(double val, double max, int levels, int expected)> TestData =
            new (double, double, int, int)[]
            {
                // Testing odd level counts. These are easy.
                (-1, 10, 5, 0),
                (0, 10, 5, 0),
                (0.01f, 10, 5, 1),
                (1, 10, 5, 1),
                (2, 10, 5, 1),
                (3, 10, 5, 1),
                (3.33f, 10, 5, 1),
                (3.34f, 10, 5, 2),
                (4, 10, 5, 2),
                (5, 10, 5, 2),
                (6, 10, 5, 2),
                (6.66f, 10, 5, 2),
                (6.67f, 10, 5, 3),
                (7, 10, 5, 3),
                (8, 10, 5, 3),
                (9, 10, 5, 3),
                (10, 10, 5, 4),
                (11, 10, 5, 4),

                // Even level counts though..
                (1, 10, 6, 1),
                (2, 10, 6, 1),
                (3, 10, 6, 2),
                (4, 10, 6, 2),
                (5, 10, 6, 2),
                (6, 10, 6, 3),
                (7, 10, 6, 3),
                (8, 10, 6, 4),
                (9, 10, 6, 4),
                (10, 10, 6, 5),
            };

        public static readonly IEnumerable<(double val, double max, int levels, int expected)> TestNear =
            new (double, double, int, int)[]
            {
                // Testing odd counts
                (0, 5, 2, 0),
                (1, 5, 2, 0),
                (2, 5, 2, 1),
                (3, 5, 2, 1),
                (4, 5, 2, 2),
                (5, 5, 2, 2),

                // Testing even counts
                (0, 6, 5, 0),
                (1, 6, 5, 1),
                (2, 6, 5, 2),
                (3, 6, 5, 3),
                (4, 6, 5, 3),
                (5, 6, 5, 4),
                (6, 6, 5, 5),

                // Testing transparency disable use case
                (0, 6, 6, 0),
                (1, 6, 6, 1),
                (2, 6, 6, 2),
                (3, 6, 6, 3),
                (4, 6, 6, 4),
                (5, 6, 6, 5),
                (6, 6, 6, 6),

                // Testing edge cases
                (0.1, 6, 5, 0),
                (-32, 6, 5, 0),
                (2.4, 6, 5, 2),
                (2.5, 6, 5, 2),
                (320, 6, 5, 5),
            };

        [Parallelizable]
        [Test]
        public void Test([ValueSource(nameof(TestData))] (double val, double max, int levels, int expected) data)
        {
            (double val, double max, int levels, int expected) = data;
            Assert.That(ContentHelpers.RoundToLevels(val, max, levels), Is.EqualTo(expected));
        }

        [Parallelizable]
        [Test]
        public void TestNearest([ValueSource(nameof(TestNear))] (double val, double max, int size, int expected) data)
        {
            (double val, double max, int size, int expected) = data;
            Assert.That(ContentHelpers.RoundToNearestLevels(val, max, size), Is.EqualTo(expected));
        }

        [Parallelizable]
        [Test]
        // Testing odd max on even levels
        [TestCase(0, 5, 2, ExpectedResult = 0)]
        [TestCase(1, 5, 2, ExpectedResult = 0)]
        [TestCase(2, 5, 2, ExpectedResult = 0)]
        [TestCase(3, 5, 2, ExpectedResult = 1)]
        [TestCase(4, 5, 2, ExpectedResult = 1)]
        [TestCase(5, 5, 2, ExpectedResult = 1)]
        // Testing even max on odd levels
        [TestCase(0, 6, 3, ExpectedResult = 0)]
        [TestCase(1, 6, 3, ExpectedResult = 0)]
        [TestCase(2, 6, 3, ExpectedResult = 1)]
        [TestCase(3, 6, 3, ExpectedResult = 1)]
        [TestCase(4, 6, 3, ExpectedResult = 2)]
        [TestCase(5, 6, 3, ExpectedResult = 2)]
        [TestCase(6, 6, 3, ExpectedResult = 2)]
        // Testing even max on even levels
        [TestCase(0, 4, 2, ExpectedResult = 0)]
        [TestCase(1, 4, 2, ExpectedResult = 0)]
        [TestCase(2, 4, 2, ExpectedResult = 1)]
        [TestCase(3, 4, 2, ExpectedResult = 1)]
        [TestCase(4, 4, 2, ExpectedResult = 1)]
        // Testing odd max on odd levels
        [TestCase(0, 5, 3, ExpectedResult = 0)]
        [TestCase(1, 5, 3, ExpectedResult = 0)]
        [TestCase(2, 5, 3, ExpectedResult = 1)]
        [TestCase(3, 5, 3, ExpectedResult = 1)]
        [TestCase(4, 5, 3, ExpectedResult = 2)]
        // Larger odd max on odd levels
        [TestCase(0, 7, 3, ExpectedResult = 0)]
        [TestCase(1, 7, 3, ExpectedResult = 0)]
        [TestCase(2, 7, 3, ExpectedResult = 0)]
        [TestCase(3, 7, 3, ExpectedResult = 1)]
        [TestCase(4, 7, 3, ExpectedResult = 1)]
        [TestCase(5, 7, 3, ExpectedResult = 2)]
        [TestCase(6, 7, 3, ExpectedResult = 2)]
        [TestCase(7, 7, 3, ExpectedResult = 2)]
        // Testing edge cases
        [TestCase(0.1, 6, 5, ExpectedResult = 0)]
        [TestCase(-32, 6, 5, ExpectedResult = 0)]
        [TestCase(2.4, 6, 5, ExpectedResult = 1)]
        [TestCase(2.5, 6, 5, ExpectedResult = 2)]
        [TestCase(320, 6, 5, ExpectedResult = 4)]
        public int TestEqual(double val, double max, int size)
        {
            return ContentHelpers.RoundToEqualLevels(val, max, size);
        }
    }
}
