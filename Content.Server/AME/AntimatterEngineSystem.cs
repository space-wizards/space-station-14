using System.Linq;
using Content.Server.AME.Components;
using Content.Server.Power.Components;
using Content.Server.Hands.Components;
using Content.Server.Popups;
using Content.Server.Tools;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using JetBrains.Annotations;

namespace Content.Server.AME
{
    [UsedImplicitly]
    public sealed class AntimatterEngineSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ToolSystem _toolSystem = default!;
        private float _accumulatedFrameTime;

        private const float UpdateCooldown = 10f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AMEControllerComponent, PowerChangedEvent>(OnAMEPowerChange);
            SubscribeLocalEvent<AMEControllerComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<AMEPartComponent, InteractUsingEvent>(OnPartInteractUsing);
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

        private static void OnAMEPowerChange(EntityUid uid, AMEControllerComponent component, ref PowerChangedEvent args)
        {
            component.UpdateUserInterface();
        }

        private void OnInteractUsing(EntityUid uid, AMEControllerComponent component, InteractUsingEvent args)
        {
            if (!TryComp(args.User, out HandsComponent? hands))
            {
                _popupSystem.PopupEntity(Loc.GetString("ame-controller-component-interact-using-no-hands-text"), uid, args.User);
                return;
            }

            if (HasComp<AMEFuelContainerComponent?>(args.Used))
            {
                if (component.HasJar)
                {
                    _popupSystem.PopupEntity(Loc.GetString("ame-controller-component-interact-using-already-has-jar"), uid, args.User);
                }

                else
                {
                    component.JarSlot.Insert(args.Used);
                    _popupSystem.PopupEntity(Loc.GetString("ame-controller-component-interact-using-success"), uid,
                        args.User, PopupType.Medium);
                    component.UpdateUserInterface();
                }
            }
            else
            {
                 _popupSystem.PopupEntity(Loc.GetString("ame-controller-component-interact-using-fail"), uid, args.User);
            }
        }

        private void OnPartInteractUsing(EntityUid uid, AMEPartComponent component, InteractUsingEvent args)
        {
            if (!HasComp<HandsComponent>(args.User))
            {
                _popupSystem.PopupEntity(Loc.GetString("ame-part-component-interact-using-no-hands"), uid, args.User);
                return;
            }

            if (!_toolSystem.HasQuality(args.Used, component.QualityNeeded))
                return;

            if (!_mapManager.TryGetGrid(args.ClickLocation.GetGridUid(EntityManager), out var mapGrid))
                return; // No AME in space.

            var snapPos = mapGrid.TileIndicesFor(args.ClickLocation);
            if (mapGrid.GetAnchoredEntities(snapPos).Any(sc => HasComp<AMEShieldComponent>(sc)))
            {
                _popupSystem.PopupEntity(Loc.GetString("ame-part-component-shielding-already-present"), uid, args.User);
                return;
            }

            var ent = EntityManager.SpawnEntity("AMEShielding", mapGrid.GridTileToLocal(snapPos));

            SoundSystem.Play(component.UnwrapSound.GetSound(), Filter.Pvs(uid), uid);

            EntityManager.QueueDeleteEntity(uid);
        }
    }
}
