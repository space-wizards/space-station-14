using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.IoC;
using SS14.Shared.Prototypes;

namespace Content.Client.Research
{
    public class LatheMenu : SS14Window
    {
#pragma warning disable CS0649
        [Dependency]
        readonly IPrototypeManager PrototypeManager;
        [Dependency]
        readonly IResourceCache ResourceCache;
#pragma warning restore

        public LatheComponent Owner { get; set; }

        protected override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);

            Title = "Lathe Menu";
            Visible = false;


        }

    }
}
