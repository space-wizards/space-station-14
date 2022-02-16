using System;
using Content.Server.Database;
using Content.Shared.Preferences;
using NUnit.Framework;

namespace Content.Tests.Shared.Preferences.Job
{
    [TestFixture]
    [TestOf(typeof(JobPriority))]
    [TestOf(typeof(DbJobPriority))]
    public sealed class JobPriorityTest
    {
        [Test]
        public void JobPriorityEnumParityTest()
        {
            var priorities = Enum.GetValues<JobPriority>();
            var dbPriorities = Enum.GetValues<DbJobPriority>();

            Assert.That(priorities.Length, Is.EqualTo(dbPriorities.Length));

            for (var i = 0; i < priorities.Length; i++)
            {
                var priority = priorities[i];
                var dbPriority = dbPriorities[i];

                Assert.That((int) priority, Is.EqualTo((int) dbPriority));
                Assert.That(priority.ToString(), Is.EqualTo(dbPriority.ToString()));
            }
        }
    }
}
