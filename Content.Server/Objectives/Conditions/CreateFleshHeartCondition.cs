using Content.Server.Flesh;
using Content.Server.Objectives.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class CreateFleshHeartCondition : IObjectiveCondition
    {
        private Mind.Mind? _mind;

        public IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            return new CreateFleshHeartCondition {
                _mind = mind,
            };
        }

        public string Title => Loc.GetString("objective-condition-create-flesh-heart-title");

        public string Description => Loc.GetString("objective-condition-create-flesh-heart-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Texture(
            new ResPath("Interface/Actions/fleshCultistFleshHeart.png"));

        private bool IsFleshHeartFinale(FleshHeartComponent comp)
        {
            return comp.State == FleshHeartSystem.HeartStates.Disable;
        }

        public float Progress
        {
            get {
                var entMan = IoCManager.Resolve<IEntityManager>();

                var fleshHeartFinale = false;

                foreach (var fleshHeartComp in entMan.EntityQuery<FleshHeartComponent>())
                {
                    Logger.Info("Find flesh heart");
                    if (!IsFleshHeartFinale(fleshHeartComp))
                        continue;
                    fleshHeartFinale = true;
                    break;
                }

                return fleshHeartFinale ? 1f : 0f;
            }
        }

        public float Difficulty => 1.3f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is CreateFleshHeartCondition esc && Equals(_mind, esc._mind);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((CreateFleshHeartCondition) obj);
        }

        public override int GetHashCode()
        {
            return _mind != null ? _mind.GetHashCode() : 0;
        }
    }
}
