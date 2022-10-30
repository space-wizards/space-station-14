using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Interaction.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Interaction;
using Content.Server.Weapons.Melee;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public sealed class HyposprayComponent : SharedHyposprayComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IEntitySystemManager _sysMan = default!;

        [DataField("clumsyFailChance")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ClumsyFailChance { get; set; } = 0.5f;

        [DataField("transferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 TransferAmount { get; set; } = FixedPoint2.New(5);

        [DataField("injectSound")]
        private SoundSpecifier _injectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

        protected override void Initialize()
        {
            base.Initialize();

            Dirty();
        }

        public bool TryDoInject(EntityUid? target, EntityUid user)
        {
            if (!EligibleEntity(target, _entMan))
                return false;

            string? msgFormat = null;

            if (target == user)
            {
                msgFormat = "hypospray-component-inject-self-message";
            }
            else if (EligibleEntity(user, _entMan) && _entMan.EntitySysManager.GetEntitySystem<InteractionSystem>().TryRollClumsy(user, ClumsyFailChance))
            {
                msgFormat = "hypospray-component-inject-self-clumsy-message";
                target = user;
            }

            var sysMan = IoCManager.Resolve<IEntitySystemManager>();
            var solutionsSys = sysMan.GetEntitySystem<SolutionContainerSystem>();
            var popupSystem = sysMan.GetEntitySystem<SharedPopupSystem>();
            solutionsSys.TryGetSolution(Owner, SolutionName, out var hypoSpraySolution);

            if (hypoSpraySolution == null || hypoSpraySolution.CurrentVolume == 0)
            {
                popupSystem.PopupCursor(Loc.GetString("hypospray-component-empty-message"), Filter.Entities(user));
                return true;
            }

            if (!solutionsSys.TryGetInjectableSolution(target.Value, out var targetSolution))
            {
                popupSystem.PopupCursor(Loc.GetString("hypospray-cant-inject", ("target", Identity.Entity(target.Value, _entMan))), Filter.Entities(user));
                return false;
            }

            popupSystem.PopupCursor(Loc.GetString(msgFormat ?? "hypospray-component-inject-other-message", ("other", target)), Filter.Entities(user));
            if (target != user)
            {
                popupSystem.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), target.Value, Filter.Entities(target.Value));
                // TODO: This should just be using melee attacks...
                // var meleeSys = sysMan.GetEntitySystem<MeleeWeaponSystem>();
                // var angle = Angle.FromWorldVec(_entMan.GetComponent<TransformComponent>(target.Value).WorldPosition - _entMan.GetComponent<TransformComponent>(user).WorldPosition);
                // meleeSys.SendLunge(angle, user);
            }

            _sysMan.GetEntitySystem<SharedAudioSystem>().Play(_injectSound, Filter.Pvs(user), user);

            // Get transfer amount. May be smaller than _transferAmount if not enough room
            var realTransferAmount = FixedPoint2.Min(TransferAmount, targetSolution.AvailableVolume);

            if (realTransferAmount <= 0)
            {
                popupSystem.PopupCursor(Loc.GetString("hypospray-component-transfer-already-full-message", ("owner", target)), Filter.Entities(user));
                return true;
            }

            // Move units from attackSolution to targetSolution
            var removedSolution =
                sysMan.GetEntitySystem<SolutionContainerSystem>()
                    .SplitSolution(Owner, hypoSpraySolution, realTransferAmount);

            if (!targetSolution.CanAddSolution(removedSolution))
            {
                return true;
            }

            removedSolution.DoEntityReaction(target.Value, ReactionMethod.Injection);

            sysMan.GetEntitySystem<SolutionContainerSystem>().TryAddSolution(target.Value, targetSolution, removedSolution);

            static bool EligibleEntity([NotNullWhen(true)] EntityUid? entity, IEntityManager entMan)
            {
                // TODO: Does checking for BodyComponent make sense as a "can be hypospray'd" tag?
                // In SS13 the hypospray ONLY works on mobs, NOT beakers or anything else.

                return entMan.HasComponent<SolutionContainerManagerComponent>(entity)
                       && entMan.HasComponent<MobStateComponent>(entity);
            }

            return true;
        }

        public override ComponentState GetComponentState()
        {
            var solutionSys = _entMan.EntitySysManager.GetEntitySystem<SolutionContainerSystem>();
            return solutionSys.TryGetSolution(Owner, SolutionName, out var solution)
                ? new HyposprayComponentState(solution.CurrentVolume, solution.MaxVolume)
                : new HyposprayComponentState(FixedPoint2.Zero, FixedPoint2.Zero);
        }
    }
}
