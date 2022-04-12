using System;
using System.Linq;
using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Tools
{
    public sealed partial class ToolSystem
    {
        private void InitializeMultipleTools()
        {
            SubscribeLocalEvent<MultipleToolComponent, ComponentStartup>(OnMultipleToolStartup);
            SubscribeLocalEvent<MultipleToolComponent, ActivateInWorldEvent>(OnMultipleToolActivated);
            SubscribeLocalEvent<MultipleToolComponent, ComponentGetState>(OnMultipleToolGetState);
        }

        private void OnMultipleToolStartup(EntityUid uid, MultipleToolComponent multiple, ComponentStartup args)
        {
            // Only set the multiple tool if we have a tool component.
            if(EntityManager.TryGetComponent(uid, out ToolComponent? tool))
                SetMultipleTool(uid, multiple, tool);
        }

        private void OnMultipleToolActivated(EntityUid uid, MultipleToolComponent multiple, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = CycleMultipleTool(uid, multiple);
        }

        private void OnMultipleToolGetState(EntityUid uid, MultipleToolComponent multiple, ref ComponentGetState args)
        {
            args.State = new MultipleToolComponentState(multiple.CurrentQualityName);
        }

        public bool CycleMultipleTool(EntityUid uid, MultipleToolComponent? multiple = null)
        {
            if (!Resolve(uid, ref multiple))
                return false;

            if (multiple.Entries.Length == 0)
                return false;

            multiple.CurrentEntry = (multiple.CurrentEntry + 1) % multiple.Entries.Length;
            SetMultipleTool(uid, multiple);

            var current = multiple.Entries[multiple.CurrentEntry];

            if(current.ChangeSound is {} changeSound)
                SoundSystem.Play(Filter.Pvs(uid), changeSound.GetSound(), uid);

            return true;
        }

        public void SetMultipleTool(EntityUid uid, MultipleToolComponent? multiple = null, ToolComponent? tool = null, SpriteComponent? sprite = null)
        {
            if (!Resolve(uid, ref multiple, ref tool))
                return;

            // Sprite is optional.
            Resolve(uid, ref sprite);

            if (multiple.Entries.Length == 0)
            {
                multiple.CurrentQualityName = Loc.GetString("multiple-tool-component-no-behavior");
                multiple.Dirty();
                return;
            }

            var current = multiple.Entries[multiple.CurrentEntry];

            tool.UseSound = current.Sound;
            tool.Qualities = current.Behavior;

            if (_prototypeManager.TryIndex(current.Behavior.First(), out ToolQualityPrototype? quality))
            {
                multiple.CurrentQualityName = Loc.GetString(quality.Name);
            }

            multiple.Dirty();

            if (current.Sprite == null || sprite == null)
                return;

            sprite.LayerSetSprite(0, current.Sprite);
        }
    }
}
