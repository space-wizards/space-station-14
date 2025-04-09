using Content.Server.GameTicking.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Robust.Shared.Timing;
using System.Linq;
using Content.Server.Heretic.Components;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Examine;
using Content.Server.Body.Systems;
using Content.Server._Goobstation.Heretic.Components;
using Content.Server._Goobstation.Heretic.UI;
using System.Collections.Immutable;
using Content.Server.EUI;
using Robust.Shared.Random;
using Content.Server.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Server.Administration.Systems;
using Content.Shared.Humanoid;
using Robust.Shared.Utility;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Server.GameObjects;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Server.StationEvents;

//this is kind of badly named since we're doing infinite archives stuff now but i dont feel like changing it :)

namespace Content.Server._Goobstation.Heretic.EntitySystems
{

    public sealed partial class HellWorldSystem : EntitySystem
    {
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly SharedMapSystem _map = default!;
        [Dependency] private readonly MetaDataSystem _metaSystem = default!;
        [Dependency] private readonly SharedTransformSystem _xform = default!;
        [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
        [Dependency] private readonly EuiManager _euiMan = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
        [Dependency] private readonly BlindableSystem _blind = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IEntityManager _ent = default!;

        private readonly ResPath _mapPath = new("Maps/_Impstation/Nonstations/InfiniteArchives.yml"); 

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HellVictimComponent, ExaminedEvent>(OnExamine);
        }

        /// <summary>
        /// Creates the hell world map.
        /// </summary>
        public void MakeHell()
        {
            if(_mapLoader.TryLoadMap(_mapPath, out var map, out _, new DeserializationOptions { InitializeMaps = true }))
            _map.SetPaused(map.Value.Comp.MapId, false);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            //hell world return
            var returnQuery = EntityQueryEnumerator<HellVictimComponent>();
            while (returnQuery.MoveNext(out var uid, out var victimComp))
            {
                //if they've been in hell long enough, return and revive them
                if (_timing.CurTime >= victimComp.ExitHellTime && !victimComp.CleanupDone)
                {
                    //make sure they won't get into this loop again
                    victimComp.CleanupDone = true;
                    if (victimComp.Mind != null) //if they ghosted before the gib, no need to return the hell mind to the body
                    {
                        //put them back in the original body
                        _mind.TransferTo(victimComp.Mind.Value, victimComp.OriginalBody);
                        //let them ghost again
                        MindComponent? mindComp = Comp<MindComponent>(victimComp.Mind.Value);
                        mindComp.PreventGhosting = false;
                    }
                    //give the original body some visual changes
                    TransformVictim(uid);
                    //tell them about the metashield
                    if (_mind.TryGetSession(victimComp.Mind, out var session))
                        _euiMan.OpenEui(new HellMemoryEui(), session);
                    //and then revive the old body
                    _rejuvenate.PerformRejuvenate(uid);
                }
            }
        }

        public void AddVictimComponent(EntityUid victim)
        {
            EnsureComp<HellVictimComponent>(victim, out var victimComp);
            victimComp.OriginalBody = victim;
            victimComp.ExitHellTime = _timing.CurTime + victimComp.HellDuration;
            victimComp.OriginalPosition = Transform(victim).Coordinates;
            //make sure the victim has a mind
            if (!TryComp<MindContainerComponent>(victim, out var mindContainer) || !mindContainer.HasMind)
            {
                return;
            }
            victimComp.HasMind = true;
            victimComp.Mind = mindContainer.Mind.Value;
        }

        //AddVictimComponent MUST BE RUN BEFORE CALLING THIS!!
        public void SendToHell(EntityUid target, RitualData args, SpeciesPrototype species)
        {
            //get the hell victim component
            if (!args.EntityManager.TryGetComponent<HellVictimComponent>(target, out var victimComp))
                return;
            //if already sent, don't send again
            if(victimComp.AlreadyHelled)
                return;

            //get all possible spawn points, choose one, then get the place
            var spawnPoints = EntityManager.GetAllComponents(typeof(HellSpawnPointComponent)).ToImmutableList();
            var newSpawn = _random.Pick(spawnPoints);
            var spawnTgt = Transform(newSpawn.Uid).Coordinates;

            //spawn your hellsona
            if (!victimComp.HasMind || victimComp.Mind == null) //just in case the 
            {
                victimComp.AlreadyHelled = true;
                return;
            }
            MindComponent? mindComp = Comp<MindComponent>(victimComp.Mind.Value);
            mindComp.PreventGhosting = true;
            //don't have to change this one's blood because nobody's bringing a forensic scanner to hell
            var sufferingWhiteBoy = Spawn(species.Prototype, spawnTgt);
            _metaSystem.SetEntityName(sufferingWhiteBoy, MetaData(target).EntityName);
            _humanoid.CloneAppearance(victimComp.OriginalBody, sufferingWhiteBoy);
            if (TryComp<BlindableComponent>(sufferingWhiteBoy, out var blindable))
            {
                _blind.AdjustEyeDamage(sufferingWhiteBoy, 5); //make it more disorienting

            }

            //and then send the mind into the hellsona
            _mind.TransferTo(victimComp.Mind.Value, sufferingWhiteBoy);
            victimComp.AlreadyHelled = true;

            //returning the mind to the original body happens in Update()
        }

        public void TeleportRandomly(RitualData args, EntityUid uid) 
        {
            //get all possible spawn points, choose one, then get the place
            var spawnPoints = EntityManager.GetAllComponents(typeof(MidRoundAntagSpawnLocationComponent)).ToImmutableList();
            var newSpawn = _random.Pick(spawnPoints);
            var spawnTgt = Transform(newSpawn.Uid).Coordinates;

            _xform.SetCoordinates(uid, spawnTgt);
        }

        private void TransformVictim(EntityUid ent)
        {
            if (TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
            {
                //there's no color saturation methods so you get this garbage instead
                var skinColor = humanoid.SkinColor;
                var colorHSV = Color.ToHsv(skinColor);
                colorHSV.Y /= 4;
                var newColor = Color.FromHsv(colorHSV);
                //make them look like they've seen some shit
                _humanoid.SetSkinColor(ent, newColor, true, false, humanoid);
                _humanoid.SetBaseLayerColor(ent, HumanoidVisualLayers.Eyes, Color.White, true, humanoid);
            }
        }

        private void OnExamine(Entity<HellVictimComponent> ent, ref ExaminedEvent args)
        {
            args.PushMarkup($"[color=red]{Loc.GetString("heretic-hell-victim-examine", ("ent", args.Examined))}[/color]");
        }
    }
}
