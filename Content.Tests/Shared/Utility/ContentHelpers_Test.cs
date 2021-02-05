using System.Collections.Generic;
using Content.Shared.Utility;
using NUnit.Framework;

namespace Content.Tests.Shared.Utility
{
    [Parallelizable]
    [TestFixture]
    [TestOf(typeof(ContentHelpers))]
    public class ContentHelpers_Test
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
                (2.5f, 10, 5, 1),
                (2.51f, 10, 5, 2),
                (3, 10, 5, 2),
                (4, 10, 5, 2),
                (5, 10, 5, 2),
                (6, 10, 5, 2),
                (7, 10, 5, 2),
                (7.49f, 10, 5, 2),
                (7.5f, 10, 5, 3),
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

        public static readonly IEnumerable<(double val, double max, int levels, int expected)> TestIndexData =
            new (double, double, int, int)[]
            {
                // Testing odd counts
                (0, 5, 3, 0),
                (1, 5, 3, 0),
                (2, 5, 3, 1),
                (3, 5, 3, 1),
                (4, 5, 3, 2),
                (5, 5, 3, 2),
 
                // Testing even counts
                (0, 6, 6, 0),
                (1, 6, 6, 1),
                (2, 6, 6, 2),
                (3, 6, 6, 3),
                (4, 6, 6, 3),
                (5, 6, 6, 4),
                (6, 6, 6, 5),
                
                // Testing transparency disable
                (0, 6, 7, 0),
                (1, 6, 7, 1),
                (2, 6, 7, 2),
                (3, 6, 7, 3),
                (4, 6, 7, 4),
                (5, 6, 7, 5),
                (6, 6, 7, 6),
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
        public void TestIndex([ValueSource(nameof(TestIndexData))] (double val, double max, int index, int expected) data)
        {
            (double val, double max, int levels, int expected) = data;
            Assert.That(ContentHelpers.RoundToNearestIndex(val, max, levels), Is.EqualTo(expected));
        }
    }
}
