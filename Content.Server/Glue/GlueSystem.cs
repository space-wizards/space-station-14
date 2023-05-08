using Content.Server.Ghost.Roles.Components;
using Content.Server.Instruments;
using Content.Server.Mind.Components;
using Content.Server.Popups;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Dummy;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Content.Shared.Hands.Components;
using Content.Server.Speech.Muting;
using Content.Shared.Item;
using Content.Shared.Interaction.Components;
using Content.Shared.Hands;
using Content.Shared.CombatMode;
using Content.Shared.Actions;
using Content.Shared.Glue;
using Content.Server.Abilities.Mime;
using Robust.Shared.Audio;
using Content.Server.Magic.Events;
using Content.Server.Chat.Systems;
using Content.Shared.Stealth.Components;
using Content.Shared.Ghost.Roles;
using Content.Server.Nutrition.Components;
using Content.Shared.DoAfter;
using Content.Shared.Nutrition;
using Content.Shared.Interaction;

namespace Content.Server.Glue
{
    public sealed class GlueSystem : SharedGlueSystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly ChatSystem _chat = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GlueComponent,AfterInteractEvent>(OnInteract);
        }

        private void OnInteract(EntityUid uid, GlueComponent component, AfterInteractEvent args)
        {
            if (args.Handled)
                return;

            EnsureComp<UnremoveableComponent>(args.Used);
            QueueDel(args.Used);

            args.Handled = true;
        }

    }
}

