using Content.Server.Actions;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Actions;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server.Nutrition.Components;

namespace Content.Server.Abilities
{
    public sealed class PrawnSquirtSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly ActionsSystem _action = default!;
        [Dependency] private readonly AtmosphereSystem _atmos = default!;
        [Dependency] private readonly TransformSystem _xform = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PrawnSquirtComponent, ComponentStartup>(OnStartup);

            SubscribeLocalEvent<PrawnSquirtComponent, PrawnSquirtActionEvent>(OnPrawnSquirt);
        }

        private void OnStartup(EntityUid uid, PrawnSquirtComponent component, ComponentStartup args)
        {
            _action.AddAction(uid, component.ActionPrawnSquirt, null);
        }


        private void OnPrawnSquirt(EntityUid uid, PrawnSquirtComponent component, PrawnSquirtActionEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp<ThirstComponent>(uid, out var thirst))
                return;

            if (thirst.CurrentThirst < component.ThirstPerSquirtUse)
            {
                _popup.PopupEntity(Loc.GetString("prawn-too-thirsty"), uid, Filter.Entities(uid));
                return;
            }
            args.Handled = true;
            thirst.CurrentThirst -= component.ThirstPerSquirtUse;
            Spawn(component.PuddleBrineId, Transform(uid).Coordinates);
            _audio.PlayPvs(component.SquirtSound, uid);
        }
    }

    public sealed class PrawnSquirtActionEvent : InstantActionEvent { };

};