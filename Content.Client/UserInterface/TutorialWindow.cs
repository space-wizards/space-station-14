using Robust.Client.Graphics;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Content.Shared.Input.ContentKeyFunctions;
using static Robust.Shared.Input.EngineKeyFunctions;

namespace Content.Client.UserInterface
{
    public sealed class TutorialWindow : SS14Window
    {
        private readonly int _headerFontSize = 14;
        private VBoxContainer VBox { get; }

        private const string IntroContents = @"Hi and welcome to Space Station 14! This tutorial will assume that you know a bit about how SS13 plays. It's mostly intended to lay out the controls and their differences from SS13.";
        private const string GameplayContents = @"Some notes on gameplay. To talk in OOC, prefix your chat message with \[ or /ooc. Death is currently show as a black circle around the player. You can respawn via the respawn button in the sandbox menu. Instead of intents, we have ""combat mode"". Check controls above for its keybind. You can't attack anybody with it off, so no more hitting yourself with your own crowbar.";
        private const string FeedbackContents = @"If you have any feedback, questions, bug reports, etc..., do not be afraid to tell us! You can ask on Discord or heck, just write it in OOC! We'll catch it.";
        private const string SandboxSpawnerContents = @"[color=#ffffff]Entitiy spawn panel options:[/color]
[color=#a4885c]Default[/color] spawns small entities like mugs without aligning them to anything, while aligning block entities like walls to the grid.
[color=#a4885c]PlaceFree[/color] spawns all entities without aligning them.
[color=#a4885c]PlaceNearby[/color] limits the spawn radius to around 2 tiles.
[color=#a4885c]SnapgridCenter[/color] aligns the entity to the middle of the tile.
[color=#a4885c]SnapgridBorder[/color] aligns the entity to the border of the tile.
[color=#ffffff]Grid aligned options:[/color]
[color=#a4885c]AlignSimilar[/color] snaps the entity to similar entities. Currently broken.
[color=#a4885c]AlignTileAny[/color] aligns the entity to the grid.
[color=#a4885c]AlignTileEmpty[/color] target tile must be empty
[color=#a4885c]AlignTileNonDense[/color] no colliders allowed in the target tile.
[color=#a4885c]AlignTileDense[/color] colliders must be in the target tile.
[color=#a4885c]AlignWall[/color] snaps to vertical halftiles.
[color=#a4885c]AlignWallProper[/color] snaps the entity to the middle of the tile edges";

        protected override Vector2? CustomSize => (520, 580);

        public TutorialWindow()
        {
            Title = "The Tutorial!";

            //Get section header font
            var cache = IoCManager.Resolve<IResourceCache>();
            var inputManager = IoCManager.Resolve<IInputManager>();
            Font headerFont = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), _headerFontSize);

            var scrollContainer = new ScrollContainer();
            scrollContainer.AddChild(VBox = new VBoxContainer());
            Contents.AddChild(scrollContainer);

            //Intro
            VBox.AddChild(new Label{FontOverride = headerFont, Text = "Intro"});
            AddFormattedText(IntroContents);

            string Key(BoundKeyFunction func)
            {
                return FormattedMessage.EscapeText(inputManager.GetKeyFunctionButtonString(func));
            }

            //Controls
            VBox.AddChild(new Label{FontOverride = headerFont, Text = "\nControls"});

            // Moved this down here so that Rider shows which args correspond to which format spot.
            AddFormattedText(Loc.GetString(@"Movement: [color=#a4885c]{0} {1} {2} {3}[/color]
Switch hands: [color=#a4885c]{4}[/color]
Use held item: [color=#a4885c]{5}[/color]
Drop held item: [color=#a4885c]{6}[/color]
Smart equip from backpack: [color=#a4885c]{24}[/color]
Smart equip from belt: [color=#a4885c]{25}[/color]
Open inventory: [color=#a4885c]{7}[/color]
Open character window: [color=#a4885c]{8}[/color]
Open crafting window: [color=#a4885c]{9}[/color]
Focus chat: [color=#a4885c]{10}[/color]
Focus OOC: [color=#a4885c]{26}[/color]
Focus Admin Chat: [color=#a4885c]{27}[/color]
Use hand/object in hand: [color=#a4885c]{22}[/color]
Do wide attack: [color=#a4885c]{23}[/color]
Use targeted entity: [color=#a4885c]{11}[/color]
Throw held item: [color=#a4885c]{12}[/color]
Pull entity: [color=#a4885c]{30}[/color]
Move pulled entity: [color=#a4885c]{29}[/color]
Stop pulling: [color=#a4885c]{32}[/color]
Examine entity: [color=#a4885c]{13}[/color]
Point somewhere: [color=#a4885c]{28}[/color]
Open entity context menu: [color=#a4885c]{14}[/color]
Toggle combat mode: [color=#a4885c]{15}[/color]
Toggle console: [color=#a4885c]{16}[/color]
Toggle UI: [color=#a4885c]{17}[/color]
Toggle debug overlay: [color=#a4885c]{18}[/color]
Toggle entity spawner: [color=#a4885c]{19}[/color]
Toggle tile spawner: [color=#a4885c]{20}[/color]
Toggle sandbox window: [color=#a4885c]{21}[/color]
Toggle admin menu [color=#a4885c]{31}[/color]",
                Key(MoveUp), Key(MoveLeft), Key(MoveDown), Key(MoveRight),
                Key(SwapHands),
                Key(ActivateItemInHand),
                Key(Drop),
                Key(OpenInventoryMenu),
                Key(OpenCharacterMenu),
                Key(OpenCraftingMenu),
                Key(FocusChat),
                Key(ActivateItemInWorld),
                Key(ThrowItemInHand),
                Key(ExamineEntity),
                Key(OpenContextMenu),
                Key(ToggleCombatMode),
                Key(ShowDebugConsole),
                Key(HideUI),
                Key(ShowDebugMonitors),
                Key(OpenEntitySpawnWindow),
                Key(OpenTileSpawnWindow),
                Key(OpenSandboxWindow),
                Key(Use),
                Key(WideAttack),
                Key(SmartEquipBackpack),
                Key(SmartEquipBelt),
                Key(FocusOOC),
                Key(FocusAdminChat),
                Key(Point),
                Key(TryPullObject),
                Key(MovePulledObject),
                Key(OpenAdminMenu),
                Key(ReleasePulledObject)));

            //Gameplay
            VBox.AddChild(new Label { FontOverride = headerFont, Text = "\nGameplay" });
            AddFormattedText(GameplayContents);

            //Gameplay
            VBox.AddChild(new Label { FontOverride = headerFont, Text = Loc.GetString("\nSandbox spawner", Key(OpenSandboxWindow)) });
            AddFormattedText(SandboxSpawnerContents);

            //Feedback
            VBox.AddChild(new Label { FontOverride = headerFont, Text = "\nFeedback" });
            AddFormattedText(FeedbackContents);
        }

        private void AddFormattedText(string text)
        {
            if(VBox == null)
                return;

            var introLabel = new RichTextLabel();
            var introMessage = new FormattedMessage();
            introMessage.AddMarkup(text);
            introLabel.SetMessage(introMessage);
            VBox.AddChild(introLabel);
        }
    }
}
