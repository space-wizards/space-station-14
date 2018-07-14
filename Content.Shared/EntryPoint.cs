using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Resources;
using SS14.Shared.IoC;

namespace Content.Shared
{
    public class EntryPoint : GameShared
    {
        public override void Init()
        {
#if DEBUG
            var resm = IoCManager.Resolve<IResourceManager>();
            resm.MountContentDirectory(@"../../../Resources/");
#endif
        }
    }
}
