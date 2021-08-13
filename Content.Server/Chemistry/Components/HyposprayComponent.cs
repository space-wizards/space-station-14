using Content.Server.Interaction.Components;
using Content.Server.MobState.States;
using Content.Server.Weapon.Melee;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution.Components;
using Content.Shared.Notification.Managers;
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
            var hypoSpraySolution = solutionsSys.GetInjectableSolution(Owner);

            if (hypoSpraySolution == null || hypoSpraySolution.CurrentVolume == 0)
            {
                user.PopupMessageCursor(Loc.GetString("hypospray-component-empty-message"));
                return true;
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

            SoundSystem.Play(Filter.Pvs(user), "/Audio/Items/hypospray.ogg", user);

            var targetSolution = solutionsSys.GetInjectableSolution(target);

            // Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = ReagentUnit.Min(TransferAmount, targetSolution!.EmptyVolume);

            if (realTransferAmount <= 0)
            {
                user.PopupMessage(user,
                    Loc.GetString("hypospray-component-transfer-already-full-message ",
                        ("owner", targetSolution.Owner)));
                return true;
            }

            // Move units from attackSolution to targetSolution
            var removedSolution =
                EntitySystem.Get<SolutionContainerSystem>()
                    .SplitSolution(hypoSpraySolution, realTransferAmount);

            if (!targetSolution.CanAddSolution(removedSolution))
            {
                return true;
            }

            removedSolution.DoEntityReaction(target, ReactionMethod.Injection);

            EntitySystem.Get<SolutionContainerSystem>().TryAddSolution(targetSolution, removedSolution);

            static bool EligibleEntity(IEntity entity)
            {
                // TODO: Does checking for BodyComponent make sense as a "can be hypospray'd" tag?
                // In SS13 the hypospray ONLY works on mobs, NOT beakers or anything else.
                return EntitySystem.Get<SolutionContainerSystem>().HasSolution(entity) && entity.HasComponent<MobStateComponent>();
            }

            return true;
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            var solution = EntitySystem.Get<SolutionContainerSystem>().GetInjectableSolution(Owner);
            if (solution == null)
                return new HyposprayComponentState(ReagentUnit.Zero, ReagentUnit.Zero);

            return new HyposprayComponentState(solution.CurrentVolume, solution.MaxVolume);
        }
    }
}
