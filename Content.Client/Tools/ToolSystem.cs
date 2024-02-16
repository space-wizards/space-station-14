using Content.Client.Items;
using Content.Client.Tools.Components;
using Content.Client.Tools.UI;
using Content.Shared.Item;
using Content.Shared.Tools.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;

namespace Content.Client.Tools
{
    public sealed class ToolSystem : SharedToolSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<WelderComponent, ComponentHandleState>(OnWelderHandleState);
            Subs.ItemStatus<WelderComponent>(ent => new WelderStatusControl(ent));
            Subs.ItemStatus<MultipleToolComponent>(ent => new MultipleToolStatusControl(ent));
        }

        public override void SetMultipleTool(EntityUid uid,
        MultipleToolComponent? multiple = null,
        ToolComponent? tool = null,
        bool playSound = false,
        EntityUid? user = null)
        {
            if (!Resolve(uid, ref multiple))
                return;

            base.SetMultipleTool(uid, multiple, tool, playSound, user);
            multiple.UiUpdateNeeded = true;

            // TODO replace this with appearance + visualizer
            // in order to convert this to a specifier, the manner in which the sprite is specified in yaml needs to be updated.

            if (multiple.Entries.Length > multiple.CurrentEntry && TryComp(uid, out SpriteComponent? sprite))
            {
                var current = multiple.Entries[multiple.CurrentEntry];
                if (current.Sprite != null)
                    sprite.LayerSetSprite(0, current.Sprite);
            }
        }

        private void OnWelderHandleState(EntityUid uid, WelderComponent welder, ref ComponentHandleState args)
        {
            if (args.Current is not WelderComponentState state)
                return;

            welder.FuelCapacity = state.FuelCapacity;
            welder.Fuel = state.Fuel;
            welder.UiUpdateNeeded = true;
        }

        protected override bool IsWelder(EntityUid uid)
        {
            return HasComp<WelderComponent>(uid);
        }
    }
}
