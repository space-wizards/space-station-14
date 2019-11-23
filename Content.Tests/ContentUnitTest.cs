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
    }
}
