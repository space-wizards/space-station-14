using System.Collections.Generic;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class RandomPottedPlantComponent : Component, IMapInit
    {
        public override string Name => "RandomPottedPlant";

        private static readonly string[] RegularPlantStates;
        private static readonly string[] PlasticPlantStates;

        private string _selectedState;
        private bool _plastic;


        static RandomPottedPlantComponent()
        {
            // ReSharper disable once StringLiteralTypo
            var states = new List<string> {"applebush"};

            for (var i = 1; i < 25; i++)
            {
                states.Add($"plant-{i:D2}");
            }

            RegularPlantStates = states.ToArray();

            states.Clear();

            for (var i = 26; i < 30; i++)
            {
                states.Add($"plant-{i:D2}");
            }

            PlasticPlantStates = states.ToArray();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _selectedState, "selected", null);
            serializer.DataField(ref _plastic, "plastic", false);
        }

        protected override void Startup()
        {
            base.Startup();

            if (_selectedState != null)
            {
                Owner.GetComponent<SpriteComponent>().LayerSetState(0, _selectedState);
            }
        }

        public void MapInit()
        {
            var random = IoCManager.Resolve<IRobustRandom>();

            var list = _plastic ? PlasticPlantStates : RegularPlantStates;
            _selectedState = random.Pick(list);

            Owner.GetComponent<SpriteComponent>().LayerSetState(0, _selectedState);
        }
    }
}
