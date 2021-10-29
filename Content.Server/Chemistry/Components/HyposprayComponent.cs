using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Interaction.Components;
using Content.Server.Weapon.Melee;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public sealed class HyposprayComponent : SharedHyposprayComponent
    {
        [DataField("ClumsyFailChance")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ClumsyFailChance { get; set; } = 0.5f;

        [DataField("TransferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit TransferAmount { get; set; } = ReagentUnit.New(5);

        [DataField("InjectSound")]
        private SoundSpecifier _injectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

        protected override void Initialize()
        {
            base.Initialize();

            Dirty();
        }

        public bool TryDoInject(IEntity? target, IEntity user)
        {
            if (target == null || !EligibleEntity(target))
                return false;

            string? msgFormat = null;

            if (target == user)
            {
                msgFormat = "hypospray-component-inject-self-message";
            }
            else if (EligibleEntity(user) && ClumsyComponent.TryRollClumsy(user, ClumsyFailChance))
            {
                msgFormat = "hypospray-component-inject-self-clumsy-message";
                target = user;
            }

            var solutionsSys = EntitySystem.Get<SolutionContainerSystem>();
            solutionsSys.TryGetSolution(Owner, SolutionName, out var hypoSpraySolution);

            if (hypoSpraySolution == null || hypoSpraySolution.CurrentVolume == 0)
            {
                user.PopupMessageCursor(Loc.GetString("hypospray-component-empty-message"));
                return true;
            }

            if (!solutionsSys.TryGetInjectableSolution(target.Uid, out var targetSolution))
            {
                user.PopupMessage(user,
                    Loc.GetString("hypospray-cant-inject", ("target", target)));
                return false;
            }

            user.PopupMessage(Loc.GetString(msgFormat ?? "hypospray-component-inject-other-message",
                ("other", target)));
            if (target != user)
            {
                target.PopupMessage(Loc.GetString("hypospray-component-feel-prick-message"));
                var meleeSys = EntitySystem.Get<MeleeWeaponSystem>();
                var angle = Angle.FromWorldVec(target.Transform.WorldPosition - user.Transform.WorldPosition);
                meleeSys.SendLunge(angle, user);
            }

            SoundSystem.Play(Filter.Pvs(user), _injectSound.GetSound(), user);

            // Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = ReagentUnit.Min(TransferAmount, targetSolution.AvailableVolume);

            if (realTransferAmount <= 0)
            {
                user.PopupMessage(user,
                    Loc.GetString("hypospray-component-transfer-already-full-message",
                        ("owner", target)));
                return true;
            }

            // Move units from attackSolution to targetSolution
            var removedSolution =
                EntitySystem.Get<SolutionContainerSystem>()
                    .SplitSolution(Owner.Uid, hypoSpraySolution, realTransferAmount);

            if (!targetSolution.CanAddSolution(removedSolution))
            {
                return true;
            }

            removedSolution.DoEntityReaction(target, ReactionMethod.Injection);

            EntitySystem.Get<SolutionContainerSystem>().TryAddSolution(target.Uid, targetSolution, removedSolution);

            static bool EligibleEntity(IEntity entity)
            {
                // TODO: Does checking for BodyComponent make sense as a "can be hypospray'd" tag?
                // In SS13 the hypospray ONLY works on mobs, NOT beakers or anything else.

                return entity.HasComponent<SolutionContainerManagerComponent>()
                       && entity.HasComponent<MobStateComponent>();
            }

            return true;
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            var solutionSys = Owner.EntityManager.EntitySysManager.GetEntitySystem<SolutionContainerSystem>();
            return solutionSys.TryGetSolution(Owner, SolutionName, out var solution)
                ? new HyposprayComponentState(solution.CurrentVolume, solution.MaxVolume)
                : new HyposprayComponentState(ReagentUnit.Zero, ReagentUnit.Zero);
        }
    }
}
