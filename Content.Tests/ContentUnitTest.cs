using System.Collections.Generic;
using System.Reflection;
using Content.Client;
using Content.Client.IoC;
using Content.Server;
using Content.Server.IoC;
using Content.Shared;
using Robust.UnitTesting;
using EntryPoint = Content.Server.EntryPoint.EntryPoint;

namespace Content.Tests
{
    public class ContentUnitTest : RobustUnitTest
    {
        protected override void OverrideIoC()
        {
            base.OverrideIoC();

            SharedContentIoC.Register();

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
                l.Add(typeof(EntryPoint).Assembly);
            }
            else if (Project == UnitTestProject.Client)
            {
                l.Add(typeof(Content.Client.EntryPoint.EntryPoint).Assembly);
            }

            l.Add(typeof(ContentUnitTest).Assembly);

            return l.ToArray();
        }
    }
}
