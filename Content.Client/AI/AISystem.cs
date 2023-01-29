using System.Collections.Generic;
using Robust.Server.GameObjects;
using Robust.Shared.Maths;
using Robust.Client.Graphics;

namespace Content.Client.AI
{
    [UsedImplicitly]
    public sealed class AISystem : SharedAISystem
    {
        private readonly List<(Vector2 position, Matrix3 viewMatrix)> _eyeData = new List<(Vector2 position, Matrix3 viewMatrix)>();

        public override void Initialize()
        {
            // Fetch all the Eye components from the entities in the engine
            var cameras = EntityQuery<SurveillanceCameraComponent>();
            foreach (var camera in cameras)
            {
                var eye = EntityManager.EnsureComponent<EyeComponent>(camera.Owner);
                _eyeData.Add((Transform(camera.Owner).WorldPosition, eye.CurrentEye));
            }
        }

        public override void Update(float frameTime)
        {
            // Iterate over all the AI entities in the engine
            var aiEntities = EntityQuery<AIComponent>();
            foreach (var aiEntity in aiEntities)
            {
                var aiPosition = Transform(aiEntity.Owner).WorldPosition;

                // Initialize field of view to zero
                var compositeFov = 0.0;

                // Iterate over all the eyes
                foreach (var (eyePosition, eyeViewMatrix) in _eyeData)
                {
                    // Calculate the angle between the AI and the eye
                    var angle = (eyePosition - aiPosition).ToAngle();

                    // Update the AI's field of view with the angle
                    compositeFov += angle.Degrees;
                }

                // Update the AI's FOV with the composite FOV
                EntityManager.TryGetComponent<EyeComponent>(aiEntity).CurrentEye = compositeFov;
            }
        }
    }
}
