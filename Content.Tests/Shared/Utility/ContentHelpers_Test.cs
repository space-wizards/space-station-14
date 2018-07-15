using Content.Shared.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Tests.Shared.Utility
{
    [TestFixture]
    [TestOf(typeof(ContentHelpers))]
    public class ContentHelpers_Test
    {
        [Test]
        public void Test()
        {
            Assert.That(ContentHelpers.RoundToLevels(1, 10, 5), Is.EqualTo(1));
        }
    }
}
