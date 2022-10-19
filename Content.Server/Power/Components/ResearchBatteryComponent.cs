using Content.Server.UserInterface;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Battery node on the pow3r network. Needs other components to connect to actual networks.
    /// </summary>
    [RegisterComponent]
    [Virtual]
    public class ResearchBatteryComponent : Component
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(ResearchSMESUiKey.Key);
        private bool Powered => !_entities.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        private BatteryComponent? _battery;

        /// <summary>
        ///    The damage the battery will do to itself if overloads without shielding
        /// </summary>
        [DataField("damage", required: true)]
        public DamageSpecifier Damage = default!;

        /// <summary>
        ///    When research mode is off the research SMES will act as a regular SMES
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("researchMode")]
        public bool ResearchMode = false;

        /// <summary>
        ///    The current amount of charge stored for "analysis" - is used to increase the max cap, trigger an overload, and pay for shielding
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("analysedCharge")]
        public float AnalysedCharge = 0f;

        /// <summary>
        ///     Can be toggled but will switch off if not enough charge on the next cycle. Prevents overload damage regardless of AnalysedCharge
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("shieldingActive")]
        public bool ShieldingActive = false;

        /// <summary>
        ///     Cost of shielding per analysis cycle relative to the MaxAnalysisCharge.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("shieldingCost")]
        public float ShieldingCost = 0.002f;

        /// <summary>
        ///     How much charge is siphoned per change relative to that charge. This can be reconfigured via interface.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("analysisSiphon")]
        public float AnalysisSiphon = 0.1f;

        /// <summary>
        ///     The analysis charge is not capable of exceeding this amount.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxAnalysisCharge = 10000000f;
        public bool MaxCapReached = false;

        /// <summary>
        ///     The research battery component will not increase a batteries maxcap any more than this amount.
        /// </summary>
        [ViewVariables]
        public float MaxChargeCeiling = 100000000f;

        /// <summary>
        ///     The research battery component can produce a disk at a set goal.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float ResearchGoal = 20000000f;
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ResearchAchieved = false;
        [DataField("researchDiskReward", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: false)]
        public string ResearchDiskReward = string.Empty;

        /// <summary>
        ///     The amount of analysis charge discharged per analysis cycle. Relative to the analysis charge itself.
        /// </summary>
        public float AnalysisDischarge = 0.1f;

        /// <summary>
        ///     The cap increase for the battery per analysis cycle. Relative to the analysis charge.
        /// </summary>
        public float CapIncrease = 1f;

        /// <summary>
        ///     If the charge exceeds this amout, relative to the MaxAnalysis charge, without shielding, the SMES will take damage equal to the excess charge
        ///     divided by 100000 per cycle
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("overloadThreshold")]
        public float OverloadThreshold = 0.008f;

        public float LastRecordedCharge;

        private bool PlayerCanUseController(EntityUid playerEntity, bool needsPower = true)
        {
            //Need player entity to check if they are still able to use the dispenser
            if (playerEntity == default)
                return false;

            //Check if device is powered
            if (needsPower && !Powered)
                return false;

            return true;
        }

        protected override void Initialize()
        {
            base.Initialize();

            _entities.TryGetComponent(Owner, out _battery);

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
                UpdateUserInterface();
            }
        }

        public void UpdateUserInterface()
        {
            var state = GetUserInterfaceState();
            UserInterface?.SetState(state);
        }

        private ResearchSMESBoundUserInterfaceState GetUserInterfaceState()
        {
            var maxCharge = _battery is not null ? _battery.MaxCharge : 0f;
            var currentCharge = _battery is not null ? _battery.CurrentCharge : 0f;
            var analysisIncrease = (currentCharge - LastRecordedCharge) * AnalysisSiphon;

            return new ResearchSMESBoundUserInterfaceState(Powered,AnalysedCharge,ShieldingActive,ShieldingCost,AnalysisSiphon,MaxCapReached,OverloadThreshold,ResearchMode,analysisIncrease,MaxAnalysisCharge,AnalysisDischarge,ResearchAchieved, maxCharge);
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity is not { Valid: true } player)
            {
                return;
            }

            var msg = (UiButtonPressedMessage)obj.Message;

            if (!PlayerCanUseController(player, true))
                return;

            switch (msg.Button)
            {
                case UiButton.ToggleResearchMode:
                    ResearchMode = !ResearchMode;
                    break;
                case UiButton.ToggleShield:
                    ShieldingActive = !ShieldingActive;
                    break;
                case UiButton.IncreaseSiphon:
                    AnalysisSiphon = AnalysisSiphon < 1f ? AnalysisSiphon += 0.10f : 1f;
                    AnalysisSiphon = AnalysisSiphon > 1f ? AnalysisSiphon = 1f : AnalysisSiphon;
                    break;
                case UiButton.DecreaseSiphon:
                    AnalysisSiphon = AnalysisSiphon > 0f ? AnalysisSiphon -= 0.10f : 0f;
                    AnalysisSiphon = AnalysisSiphon < 0f ? AnalysisSiphon = 0f : AnalysisSiphon;
                    break;
            }

            UpdateUserInterface();
        }
    }
}
