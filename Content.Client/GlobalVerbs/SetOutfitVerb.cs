using Content.Client.GameObjects.Components.HUD.Inventory;
using Content.Shared.GameObjects.Verbs;
using Robust.Client.Console;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.GlobalVerbs
{
    [GlobalVerb]
    public class SetOutfitVerb : GlobalVerb
    {
        public override bool RequireInteractionRange => false;
        public override bool BlockedByContainers => false;

        public override void GetData(IEntity user, IEntity target, VerbData data)
        {
            data.Visibility = VerbVisibility.Invisible;


            if (!CanCommand() || !target.HasComponent<ClientInventoryComponent>())
                return;

            data.Visibility = VerbVisibility.Visible;
            data.Text = Loc.GetString("Set Outfit");
            data.CategoryData = VerbCategories.Debug;
        }

        public override void Activate(IEntity user, IEntity target)
        {
            if (!CanCommand())
                return;
            var console = IoCManager.Resolve<IClientConsole>();

            var entityId = target.Uid.ToString();
            console.ProcessCommand($"setoutfit {entityId}");
        }

        private static bool CanCommand()
        {
            var groupController = IoCManager.Resolve<IClientConGroupController>();
            return groupController.CanCommand("setoutfit");
        }
    }
}
