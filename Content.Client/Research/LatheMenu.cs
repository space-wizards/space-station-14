using Content.Shared.Research;
using Microsoft.SqlServer.Server;
using Mono.Cecil;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;
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

        private ItemList Items;
        protected override Vector2? CustomSize => (758, 431);

        public LatheComponent Owner { get; set; }

        protected override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);

            HideOnClose = true;
            Title = "Lathe Menu";
            Visible = false;

            var hbox = new HBoxContainer();

            Contents.AddChild(hbox);

            hbox.SetAnchorPreset(LayoutPreset.Wide);

            Items = new ItemList()
            {
                SizeFlagsHorizontal = SizeFlags.Expand,
                SizeFlagsStretchRatio = 1,
            };

            Items.AddItem("henkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenk", ResourceCache.GetResource<TextureResource>("/Textures/Objects/crowbar.png"));
            Items.AddItem("honkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonk", ResourceCache.GetResource<TextureResource>("/Textures/Objects/Flashlight.png"));
            Items.AddItem("henkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenk", ResourceCache.GetResource<TextureResource>("/Textures/Objects/crowbar.png"));
            Items.AddItem("honkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonk", ResourceCache.GetResource<TextureResource>("/Textures/Objects/Flashlight.png"));
            Items.AddItem("henkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenk", ResourceCache.GetResource<TextureResource>("/Textures/Objects/crowbar.png"));
            Items.AddItem("honkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonk", ResourceCache.GetResource<TextureResource>("/Textures/Objects/Flashlight.png"));
            Items.AddItem("henkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenk", ResourceCache.GetResource<TextureResource>("/Textures/Objects/crowbar.png"));
            Items.AddItem("honkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonk", ResourceCache.GetResource<TextureResource>("/Textures/Objects/Flashlight.png"));
            Items.AddItem("henkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenk", ResourceCache.GetResource<TextureResource>("/Textures/Objects/crowbar.png"));
            Items.AddItem("honkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonk", ResourceCache.GetResource<TextureResource>("/Textures/Objects/Flashlight.png"));
            Items.AddItem("henkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenkhenk", ResourceCache.GetResource<TextureResource>("/Textures/Objects/crowbar.png"));
            Items.AddItem("honkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonkhonk", ResourceCache.GetResource<TextureResource>("/Textures/Objects/Flashlight.png"));

            var spacer = new Control()
            {
                SizeFlagsHorizontal = SizeFlags.Expand,
                SizeFlagsStretchRatio = 1
            };

            var label = new Label() { Text = "mecmecmecm" };

            spacer.AddChild(label);

            hbox.AddChild(Items);
            hbox.AddChild(spacer);

            Items.SetAnchorPreset(LayoutPreset.Wide);

            Logger.Info("Test: " + label.Rect + Items.Rect + Items.Size);

            //spacer.SetAnchorPreset(LayoutPreset.Wide);






            AddToScreen();
        }

        public void Populate()
        {
            foreach (var prototype in PrototypeManager.EnumeratePrototypes<LatheRecipePrototype>())
            {

            }
        }

    }
}
