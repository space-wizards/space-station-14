using Robust.Client.ResourceManagement;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;

namespace Content.Client.IoC
{
    public static class StaticIoC
    {
        public static IClientResourceCache ResC => IoCManager.Resolve<IClientResourceCache>();
    }
}
