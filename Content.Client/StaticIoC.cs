using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.IoC;

namespace Content.Client
{
    public static class StaticIoC
    {
        public static IResourceCache ResC => IoCManager.Resolve<IResourceCache>();
    }
}
