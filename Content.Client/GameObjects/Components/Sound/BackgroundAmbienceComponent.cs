using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Content.Shared.GameObjects.Components.Sound;
using Content.Shared.Maps;
using Content.Shared.Physics;
using NFluidsynth;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Logger = Robust.Shared.Log.Logger;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Client.GameObjects.Components.Sound
{
    [RegisterComponent]
    public class BackgroundAmbienceComponent : Component
    {
        public override string Name => "BackgroundAmbience";

        [ViewVariables(VVAccess.ReadWrite)]
        private bool debugDoStart = false;

        private bool started = false;

        private bool inArea = false;

        private readonly CancellationTokenSource _timerCancelTokenSource = new CancellationTokenSource();

        private LoopingSoundComponent _soundComponent;

        private IMapManager _mapManager;

        private ITileDefinitionManager _tileDefinitionManager;

        private ScheduledSound _scheduledSound = new ScheduledSound();


        private AudioParams _audioParams = new AudioParams();

        protected override void Startup()
        {
            base.Startup();

            _scheduledSound.Filename = "/Audio/machines/microwave_loop.ogg";

            AudioParams newParams = AudioParams.Default;
            newParams.Loop = true;
            _scheduledSound.AudioParams = newParams;

            _soundComponent = Owner.GetComponent<LoopingSoundComponent>();

            _mapManager = IoCManager.Resolve<IMapManager>();

            _tileDefinitionManager = IoCManager.Resolve<ITileDefinitionManager>();

            Timer.SpawnRepeating(500, CheckConditions, _timerCancelTokenSource.Token);

        }

        private void CheckConditions()
        {
            Circle _area = new Circle(Owner.Transform.WorldPosition, 5f);
            IEnumerable<TileRef> intersectingTiles = _mapManager.GetGrid(Owner.Transform.GridID).GetTilesIntersecting(_area);
            List<TileRef> validTiles = new List<TileRef>();


            int maintTiles = 0;

            foreach (var tile in intersectingTiles)
            {
                if (IsVisible(tile, Owner))
                {
                   validTiles.Add(tile);
                }
                else
                {
                    continue;
                }
                if (((ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId]).Name == "plating")
                {
                    maintTiles++;
                }
            }



            bool isInMaint = (float) maintTiles / validTiles.Count() >= 0.2f;


            Logger.Debug(((float)maintTiles / validTiles.Count()).ToString());
            Logger.Debug(validTiles.Count().ToString() + " visible tiles detected");

            Logger.Debug(isInMaint + " for isInMaint");
            Logger.Debug(inArea + " for inArea");



            if (isInMaint && !inArea)
            {
                inArea = true;
                _scheduledSound.Play = true;
                _soundComponent.AddScheduledSound(_scheduledSound);
            }
            else if(!isInMaint && inArea)
            {
                inArea = false;
                _soundComponent.FadeStopScheduledSound(_scheduledSound.Filename, 2000);
            }

            if (debugDoStart && !started)
            {
                started = true;
                _soundComponent.AddScheduledSound(_scheduledSound);
                Timer.Spawn(500, () =>
                {
                    _soundComponent.FadeStopScheduledSound(_scheduledSound.Filename, 2000);
                    //_soundComponent.StopScheduledSound(_scheduledSound.Filename);
                });
            }
        }

        private bool IsVisible(TileRef tile, IEntity entity)
        {
            Vector2 tilePos = new Vector2(tile.X, tile.Y);

            if (tile.GridIndex != entity.Transform.GridID)
            {
                return false;
            }

            if ((tilePos - entity.Transform.GridPosition.Position).Length <= 1.0f)
            {
                return true;
            }

            var angle = new Angle(tilePos - entity.Transform.GridPosition.Position);
            var ray = new CollisionRay(
                entity.Transform.GridPosition.Position,
                angle.ToVec(),
                (int)(CollisionGroup.Opaque));

            var rayCastResults = IoCManager.Resolve<IPhysicsManager>()
                .IntersectRay(entity.Transform.MapID, ray, (tilePos - entity.Transform.GridPosition.Position).Length, entity);

            if (rayCastResults.Count() >= 1)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }



}
