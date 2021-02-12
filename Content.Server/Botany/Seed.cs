using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Botany;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Interfaces;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Server.Botany
{
    public enum HarvestType : byte
    {
        NoRepeat,
        Repeat,
        SelfHarvest,
    }

/*
    public enum PlantSpread : byte
    {
        NoSpread,
        Creepers,
        Vines,
    }

    public enum PlantMutation : byte
    {
        NoMutation,
        Mutable,
        HighlyMutable,
    }

    public enum PlantCarnivorous : byte
    {
        NotCarnivorous,
        EatPests,
        EatLivingBeings,
    }

    public enum PlantJuicy : byte
    {
        NotJuicy,
        Juicy,
        Slippery,
    }
*/

    public struct SeedChemQuantity
    {
        public int Min;
        public int Max;
        public int PotencyDivisor;
    }

    [Prototype("seed")]
    public class Seed : IPrototype, IIndexedPrototype, IExposeData
    {
        private const string SeedPrototype = "SeedBase";

        public string ID { get; private set; }

        /// <summary>
        ///     Unique identifier of this seed. Do NOT set this.
        /// </summary>
        public int Uid { get; internal set; } = -1;

        #region Tracking
        [ViewVariables] public string Name { get; set; }
        [ViewVariables] public string SeedName { get; set; }
        [ViewVariables] public string SeedNoun { get; set; }
        [ViewVariables] public string DisplayName { get; set; }
        [ViewVariables] public bool RoundStart { get; private set; }
        [ViewVariables] public bool Mysterious { get; set; }
        [ViewVariables] public bool Immutable { get; set; }
        #endregion

        #region Output
        [ViewVariables] public List<string> ProductPrototypes { get; set; }
        [ViewVariables] public Dictionary<string, SeedChemQuantity> Chemicals { get; set; }
        [ViewVariables] public Dictionary<Gas, float> ConsumeGasses { get; set; }
        [ViewVariables]public Dictionary<Gas, float> ExudeGasses { get; set; }
        #endregion

        #region Tolerances
        [ViewVariables] public float NutrientConsumption { get; set; }
        [ViewVariables] public float WaterConsumption { get; set; }
        [ViewVariables] public float IdealHeat { get; set; }
        [ViewVariables] public float HeatTolerance { get; set; }
        [ViewVariables] public float IdealLight { get; set; }
        [ViewVariables] public float LightTolerance { get; set; }
        [ViewVariables] public float ToxinsTolerance { get; set; }
        [ViewVariables] public float LowPressureTolerance { get; set; }
        [ViewVariables] public float HighPressureTolerance { get; set; }
        [ViewVariables] public float PestTolerance { get; set; }
        [ViewVariables] public float WeedTolerance { get; set; }
        #endregion

        #region General traits
        [ViewVariables] public float Endurance { get; set; }
        [ViewVariables] public int Yield { get; set; }
        [ViewVariables] public float Lifespan { get; set; }
        [ViewVariables] public float Maturation { get; set; }
        [ViewVariables] public float Production { get; set; }
        [ViewVariables] public int GrowthStages { get; set; }
        [ViewVariables] public HarvestType HarvestRepeat { get; set; }
        [ViewVariables] public float Potency { get; set; }
        // No, I'm not removing these.
        //public PlantSpread Spread { get; set; }
        //public PlantMutation Mutation { get; set; }
        //public float AlterTemperature { get; set; }
        //public PlantCarnivorous Carnivorous { get; set; }
        //public bool Parasite { get; set; }
        //public bool Hematophage { get; set; }
        //public bool Thorny { get; set; }
        //public bool Stinging { get; set; }
        public bool Ligneous { get; set; }
        // public bool Teleporting { get; set; }
        // public PlantJuicy Juicy { get; set; }
        #endregion

        #region Cosmetics
        [ViewVariables]public ResourcePath PlantRsi { get; set; }
        [ViewVariables] public string PlantIconState { get; set; }
        [ViewVariables] public bool Bioluminescent { get; set; }
        [ViewVariables] public Color BioluminescentColor { get; set; }
        [ViewVariables] public string SplatPrototype { get; set; }
        #endregion


        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            InternalExposeData(serializer);
        }

        public void InternalExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.ID, "id", string.Empty);
            serializer.DataField(this, x => x.Name, "name", string.Empty);
            serializer.DataField(this, x => x.SeedName, "seedName", string.Empty);
            serializer.DataField(this, x => x.SeedNoun, "seedNoun", "seeds");
            serializer.DataField(this, x => x.DisplayName, "displayName", string.Empty);
            serializer.DataField(this, x => x.RoundStart, "roundStart", true);
            serializer.DataField(this, x => x.Mysterious, "mysterious", false);
            serializer.DataField(this, x => x.Immutable, "immutable", false);
            serializer.DataField(this, x => x.ProductPrototypes, "productPrototypes", new List<string>());
            serializer.DataField(this, x => x.Chemicals, "chemicals", new Dictionary<string, SeedChemQuantity>());
            serializer.DataField(this, x => x.ConsumeGasses, "consumeGasses", new Dictionary<Gas, float>());
            serializer.DataField(this, x => x.ExudeGasses, "exudeGasses", new Dictionary<Gas, float>());
            serializer.DataField(this, x => x.NutrientConsumption, "nutrientConsumption", 0.25f);
            serializer.DataField(this, x => x.WaterConsumption, "waterConsumption", 3f);
            serializer.DataField(this, x => x.IdealHeat, "idealHeat", 293f);
            serializer.DataField(this, x => x.HeatTolerance, "heatTolerance", 20f);
            serializer.DataField(this, x => x.IdealLight, "idealLight", 7f);
            serializer.DataField(this, x => x.LightTolerance, "lightTolerance", 5f);
            serializer.DataField(this, x => x.ToxinsTolerance, "toxinsTolerance", 4f);
            serializer.DataField(this, x => x.LowPressureTolerance, "lowPressureTolerance", 25f);
            serializer.DataField(this, x => x.HighPressureTolerance, "highPressureTolerance", 200f);
            serializer.DataField(this, x => x.PestTolerance, "pestTolerance", 5f);
            serializer.DataField(this, x => x.WeedTolerance, "weedTolerance", 5f);
            serializer.DataField(this, x => x.Endurance, "endurance", 100f);
            serializer.DataField(this, x => x.Yield, "yield", 0);
            serializer.DataField(this, x => x.Lifespan, "lifespan", 0f);
            serializer.DataField(this, x => x.Maturation, "maturation", 0f);
            serializer.DataField(this, x => x.Production, "production", 0f);
            serializer.DataField(this, x => x.GrowthStages, "growthStages", 6);
            serializer.DataField(this, x => x.HarvestRepeat, "harvestRepeat", HarvestType.NoRepeat);
            serializer.DataField(this, x => x.Potency, "potency", 1f);
            serializer.DataField(this, x => x.Ligneous, "ligneous", false);
            serializer.DataField(this, x => x.PlantRsi, "plantRsi", null);
            serializer.DataField(this, x => x.PlantIconState, "plantIconState", "produce");
            serializer.DataField(this, x => x.Bioluminescent, "bioluminescent", false);
            serializer.DataField(this, x => x.BioluminescentColor, "bioluminescentColor", Color.White);
            serializer.DataField(this, x => x.SplatPrototype, "splatPrototype", string.Empty);
        }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            InternalExposeData(serializer);
        }

        public Seed Clone()
        {
            var newSeed = new Seed()
            {
                ID = ID,
                Name = Name,
                SeedName = SeedName,
                SeedNoun = SeedNoun,
                RoundStart = RoundStart,
                Mysterious = Mysterious,

                ProductPrototypes = new List<string>(ProductPrototypes),
                Chemicals = new Dictionary<string, SeedChemQuantity>(Chemicals),
                ConsumeGasses = new Dictionary<Gas, float>(ConsumeGasses),
                ExudeGasses = new Dictionary<Gas, float>(ExudeGasses),

                NutrientConsumption = NutrientConsumption,
                WaterConsumption = WaterConsumption,
                IdealHeat = IdealHeat,
                HeatTolerance = HeatTolerance,
                IdealLight = IdealLight,
                LightTolerance = LightTolerance,
                ToxinsTolerance = ToxinsTolerance,
                LowPressureTolerance = LowPressureTolerance,
                HighPressureTolerance = HighPressureTolerance,
                PestTolerance = PestTolerance,
                WeedTolerance = WeedTolerance,

                Endurance = Endurance,
                Yield = Yield,
                Lifespan = Lifespan,
                Maturation = Maturation,
                Production = Production,
                GrowthStages = GrowthStages,
                HarvestRepeat = HarvestRepeat,
                Potency = Potency,

                PlantRsi = PlantRsi,
                PlantIconState = PlantIconState,
                Bioluminescent = Bioluminescent,
                BioluminescentColor = BioluminescentColor,
                SplatPrototype = SplatPrototype,
            };

            return newSeed;
        }

        public IEntity SpawnSeedPacket(EntityCoordinates transformCoordinates, IEntityManager entityManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            var seed = entityManager.SpawnEntity(SeedPrototype, transformCoordinates);

            var seedComp = seed.EnsureComponent<SeedComponent>();
            seedComp.Seed = this;

            if (seed.TryGetComponent(out SpriteComponent sprite))
            {
                // Seed state will always be seed. Blame the spriter if that's not the case!
                sprite.LayerSetSprite(0, new SpriteSpecifier.Rsi(PlantRsi, "seed"));
            }

            seed.Name = Loc.GetString($"packet of {SeedName} {SeedNoun}");

            return seed;
        }

        private void AddToDatabase()
        {
            var plantSystem = EntitySystem.Get<PlantSystem>();
            if (plantSystem.AddSeedToDatabase(this))
            {
                Name = Uid.ToString();
            }
        }

        public IEnumerable<IEntity> AutoHarvest(EntityCoordinates position, int yieldMod = 1)
        {
            if (position.IsValid(IoCManager.Resolve<IEntityManager>()) && ProductPrototypes != null &&
                ProductPrototypes.Count > 0)
                return GenerateProduct(position, yieldMod);

            return Enumerable.Empty<IEntity>();
        }

        public IEnumerable<IEntity> Harvest(IEntity user, int yieldMod = 1)
        {
            AddToDatabase();

            if (user == null)
                return Enumerable.Empty<IEntity>();

            if (ProductPrototypes == null || ProductPrototypes.Count == 0 || Yield <= 0)
            {
                user.PopupMessageCursor(Loc.GetString("You fail to harvest anything useful."));
                return Enumerable.Empty<IEntity>();
            }

            user.PopupMessageCursor(Loc.GetString($"You harvest from the {DisplayName}"));
            return GenerateProduct(user.Transform.Coordinates, yieldMod);
        }

        public IEnumerable<IEntity> GenerateProduct(EntityCoordinates position, int yieldMod = 1)
        {
            var totalYield = 0;
            if (Yield > -1)
            {
                if (yieldMod < 0)
                {
                    yieldMod = 1;
                    totalYield = Yield;
                }
                else
                {
                    totalYield = Yield * yieldMod;
                }

                totalYield = Math.Max(1, totalYield);
            }

            var random = IoCManager.Resolve<IRobustRandom>();
            var entityManager = IoCManager.Resolve<IEntityManager>();

            var products = new List<IEntity>();

            for (var i = 0; i < totalYield; i++)
            {
                var product = random.Pick(ProductPrototypes);

                var entity = entityManager.SpawnEntity(product, position);
                entity.RandomOffset(0.25f);
                products.Add(entity);

                var produce = entity.EnsureComponent<ProduceComponent>();

                produce.Seed = this;
                produce.Grown();

                if (Mysterious)
                {
                    entity.Name += "?";
                    entity.Description += Loc.GetString(" On second thought, something about this one looks strange.");
                }
            }

            return products;
        }

        public Seed Diverge(bool modified)
        {
            return Clone();
        }

        public bool CheckHarvest(IEntity user, IEntity held = null)
        {
            return (!Ligneous || (Ligneous && held != null && held.HasComponent<BotanySharpComponent>()));
        }
    }
}
