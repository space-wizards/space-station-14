using System;
using Content.Shared.Administration;
using NUnit.Framework;

namespace Content.Tests.Shared.Administration
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public sealed class AdminFlagsExtTest
    {
        [Test]
        [TestCase("ADMIN", AdminFlags.Admin)]
        [TestCase("ADMIN,DEBUG", AdminFlags.Admin | AdminFlags.Debug)]
        [TestCase("ADMIN,DEBUG,HOST", AdminFlags.Admin | AdminFlags.Debug | AdminFlags.Host)]
        [TestCase("", AdminFlags.None)]
        public void TestNamesToFlags(string namesConcat, AdminFlags flags)
        {
            var names = namesConcat.Split(",", StringSplitOptions.RemoveEmptyEntries);

            Assert.That(AdminFlagsHelper.NamesToFlags(names), Is.EqualTo(flags));
        }

        [Test]
        [TestCase("ADMIN", AdminFlags.Admin)]
        [TestCase("ADMIN,DEBUG", AdminFlags.Admin | AdminFlags.Debug)]
        [TestCase("ADMIN,DEBUG,HOST", AdminFlags.Admin | AdminFlags.Debug | AdminFlags.Host)]
        [TestCase("", AdminFlags.None)]
        public void TestFlagsToNames(string namesConcat, AdminFlags flags)
        {
            var names = namesConcat.Split(",", StringSplitOptions.RemoveEmptyEntries);

            Assert.That(AdminFlagsHelper.FlagsToNames(flags), Is.EquivalentTo(names));
        }
    }
}
