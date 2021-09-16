using Content.Shared.ActionBlocker;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Tabletop.Components
{
    /// <summary>
    /// A component that makes an object playable as a tabletop game.
    /// </summary>
    [RegisterComponent]
    public class TabletopGameComponent : Component
    {
        public override string Name => "TabletopGame";

        [DataField("boardName")]
        public string BoardName { get; } = "tabletop-default-board-name";

        [DataField("setup", required: true)]
        public TabletopSetup Setup { get; } = new TabletopChessSetup();

        [DataField("size")]
        public Vector2i Size { get; } = (300, 300);

        [DataField("cameraZoom")]
        public Vector2 CameraZoom { get; } = Vector2.One;

        /// <summary>
        /// A verb that allows the player to start playing a tabletop game.
        /// </summary>
        [Verb]
        public class PlayVerb : Verb<TabletopGameComponent>
        {
            protected override void GetData(IEntity user, TabletopGameComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("tabletop-verb-play-game");
                data.IconTexture = "/Textures/Interface/VerbIcons/die.svg.192dpi.png";
            }

            protected override void Activate(IEntity user, TabletopGameComponent component)
            {
                EntitySystem.Get<TabletopSystem>().OpenTable(user, component.Owner);
            }
        }
    }
}
