using Content.Client.GameObjects.Components.Research;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;

namespace Content.Client.Research
{
    public class ResearchConsoleMenu : SS14Window
    {
        public ResearchConsoleBoundUserInterface Owner { get; set; }

        protected override Vector2? CustomSize => (800, 400);

        public ResearchConsoleMenu()
        {
            IoCManager.InjectDependencies(this);
            Title = "R&D Console";
        }
    }
}
