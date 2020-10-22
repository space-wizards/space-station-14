using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Interfaces;
using Content.Shared.Utility;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
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

    [Prototype("seed")]
    public class Seed : IPrototype, IIndexedPrototype, IExposeData
    {
        public string ID { get; private set; }

        /// <summary>
        ///     Unique identifier of this seed. Do NOT set this.
        /// </summary>
        public int Uid { get; internal set; } = -1;

        #region Tracking
        public string Name { get; set; }
        public string SeedName { get; set; }
        public string SeedNoun { get; set; }
        public string DisplayName { get; set; }
        public bool RoundStart { get; private set; }
        public bool Mysterious { get; set; }
        public bool Immutable { get; set; }
        #endregion

        #region Output
        public List<string> ProductPrototypes { get; set; }
        public List<string> Chemicals { get; set; }
        public Dictionary<Gas, float> ConsumeGasses { get; set; }
        public Dictionary<Gas, float> ExudeGasses { get; set; }
        #endregion

        #region Tolerances
        public float NutrientConsumption { get; set; }
        public float WaterConsumption { get; set; }
        public float IdealHeat { get; set; }
        public float HeatTolerance { get; set; }
        public float IdealLight { get; set; }
        public float LightTolerance { get; set; }
        public float ToxinsTolerance { get; set; }
        public float LowPressureTolerance { get; set; }
        public float HighPressureTolerance { get; set; }
        public float PestTolerance { get; set; }
        public float WeedTolerance { get; set; }
        #endregion

        #region General traits
        public float Endurance { get; set; }
        public int Yield { get; set; }
        public float Lifespan { get; set; }
        public float Maturation { get; set; }
        public float Production { get; set; }
        public int GrowthStages { get; set; }
        public HarvestType HarvestRepeat { get; set; }
        public float Potency { get; set; }
        //public PlantSpread Spread { get; set; }
        //public PlantMutation Mutation { get; set; }
        //public float AlterTemperature { get; set; }
        //public PlantCarnivorous Carnivorous { get; set; }
        //public bool Parasite { get; set; }
        //public bool Hematophage { get; set; }
        //public bool Thorny { get; set; }
        //public bool Stinging { get; set; }
        //public bool Ligneous { get; set; }
        // public bool Teleporting { get; set; }
        // public PlantJuicy Juicy { get; set; }
        #endregion

        #region Cosmetics
        public ResourcePath PlantRsi { get; set; }
        public string PlantIconState { get; set; }
        public bool Bioluminescent { get; set; }
        public Color BioluminescentColor { get; set; }
        public string SplatPrototype { get; set; }
        #endregion


        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.ID, "id", null);

            // TODO
        }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            ExposeData(serializer);
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
                Chemicals = new List<string>(Chemicals),
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

        public IEntity SpawnSeedPacket(EntityCoordinates transformCoordinates)
        {
            // TODO
            return null;
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
                products.Add(entity);

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
            // TODO
            return Clone();
        }
    }
}
