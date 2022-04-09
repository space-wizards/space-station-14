using Content.Server.AME.Components;
using Content.Server.Power.Components;
using Content.Server.Hands.Components;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Robust.Shared.Player;
using JetBrains.Annotations;

namespace Content.Server.AME
{
    [UsedImplicitly]
    public sealed class AntimatterEngineSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        private float _accumulatedFrameTime;

        private const float UpdateCooldown = 10f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AMEControllerComponent, PowerChangedEvent>(OnAMEPowerChange);
            SubscribeLocalEvent<AMEControllerComponent, InteractUsingEvent>(OnInteractUsing);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // TODO: Won't exactly work with replays I guess?
            _accumulatedFrameTime += frameTime;
            if (_accumulatedFrameTime >= UpdateCooldown)
            {
                foreach (var comp in EntityManager.EntityQuery<AMEControllerComponent>())
                {
                    comp.OnUpdate(frameTime);
                }
                _accumulatedFrameTime -= UpdateCooldown;
            }
        }

        private static void OnAMEPowerChange(EntityUid uid, AMEControllerComponent component, PowerChangedEvent args)
        {
            component.UpdateUserInterface();
        }

        private void OnInteractUsing(EntityUid uid, AMEControllerComponent component, InteractUsingEvent args)
        {
            if (!TryComp(args.User, out HandsComponent? hands))
            {
                _popupSystem.PopupEntity(Loc.GetString("ame-controller-component-interact-using-no-hands-text"), uid, Filter.Entities(args.User));
                return;
            }

            if (HasComp<AMEFuelContainerComponent?>(args.Used))
            {
                if (component.HasJar)
                {
                    _popupSystem.PopupEntity(Loc.GetString("ame-controller-component-interact-using-already-has-jar"), uid, Filter.Entities(args.User));
                }

                else
                {
                    component.JarSlot.Insert(args.Used);
                    _popupSystem.PopupEntity(Loc.GetString("ame-controller-component-interact-using-success"), uid, Filter.Entities(args.User));
                    component.UpdateUserInterface();
                }
            }
            else
            {
                 _popupSystem.PopupEntity(Loc.GetString("ame-controller-component-interact-using-fail"), uid, Filter.Entities(args.User));
            }
        }
    }
}
