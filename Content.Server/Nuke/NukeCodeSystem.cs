using Content.Server.Communications;
using Content.Server.Paper;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Nuke
{
    /// <summary>
    ///     Nuclear code is generated once per round
    ///     One code works for all nukes
    /// </summary>
    public class NukeCodeSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public const int CodeLength = 6;
        public string Code { get; private set; } = default!;

        public override void Initialize()
        {
            base.Initialize();
            GenerateNewCode();
        }

        /// <summary>
        ///     Checks if code is equal to current bombs code
        /// </summary>
        public bool IsCodeValid(string code)
        {
            return code == Code;
        }

        /// <summary>
        ///     Generate a new nuclear bomb code. Replacing old one.
        /// </summary>
        public void GenerateNewCode()
        {
            var ret = "";
            for (int i = 0; i < CodeLength; i++)
            {
                var c = (char) _random.Next('0', '9' + 1);
                ret += c;
            }

            Code = ret;
        }

        /*/// <summary>
        ///     Send a nuclear code to all communication consoles
        /// </summary>
        public void SendNukeCodes()
        {
            var consoles = EntityManager.EntityQuery<CommunicationsConsoleComponent>();
            foreach (var console in consoles)
            {
                if (!EntityManager.TryGetComponent(console.OwnerUid, out ITransformComponent? transform))
                    continue;

                var consolePos = transform.MapPosition;
                var paperEnt = EntityManager.SpawnEntity("Paper", consolePos);

                if (!EntityManager.TryGetComponent(paperEnt.Uid, out PaperComponent? paper))
                    continue;

                paper.Content
            }
        }*/
    }
}
