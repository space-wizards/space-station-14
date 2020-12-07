using System.Collections.Generic;
using System.Reflection;
using Content.Client;
using Content.Server;
using Robust.UnitTesting;

namespace Content.Tests
{
    public class ContentUnitTest : RobustUnitTest
    {
        protected override void OverrideIoC()
        {
            base.OverrideIoC();

            if (Project == UnitTestProject.Server)
            {
                ServerContentIoC.Register();
            }
            else if (Project == UnitTestProject.Client)
            {
                ClientContentIoC.Register();
            }
        }

        protected override Assembly[] GetContentAssemblies()
        {
            var l = new List<Assembly>
            {
                typeof(Content.Shared.EntryPoint).Assembly
            };

            if (Project == UnitTestProject.Server)
            {
                l.Add(typeof(Content.Server.EntryPoint).Assembly);
            }
            else if (Project == UnitTestProject.Client)
            {
                l.Add(typeof(Content.Client.EntryPoint).Assembly);
            }

            l.Add(typeof(ContentUnitTest).Assembly);

            return l.ToArray();
        }
    }
}
