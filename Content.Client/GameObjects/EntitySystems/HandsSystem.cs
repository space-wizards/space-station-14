using Content.Client.UserInterface;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.IoC;
using static Content.Shared.GameObjects.EntitySystemMessages.HandsSystemMessages;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class HandsSystem : SharedHandsSystem
    {
        [Dependency] private readonly IGameHud _gameHud = default!;

        public override void Initialize()
        {
            base.Initialize();

            _gameHud.OnHandChanged = OnHandChanged;
        }

        private void OnHandChanged(string hand)
        {
            EntityManager.RaisePredictiveEvent(new ChangeHandMessage(hand));
        }
    }
}
