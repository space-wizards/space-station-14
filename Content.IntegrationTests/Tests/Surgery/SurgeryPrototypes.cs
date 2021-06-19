namespace Content.IntegrationTests.Tests.Surgery
{
    public class SurgeryPrototypes
    {
        public const string DrapesDummyId = "DrapesDummy";
        public const string IncisionDummyId = "IncisionDummy";
        public const string VesselCompressionDummyId = "VesselCompressionDummy";
        public const string RetractionDummyId = "RetractionDummy";
        public const string AmputationDummyId = "AmputationDummy";
        public const string CauteryDummyId = "CauteryDummy";

        public const string TestAmputationOperationId = "TestAmputation";
        public const string TestAmputationStepId = "TestAmputation";
        public const string TestCauterizationStepId = "TestCauterization";
        public const string TestDrillingStepId = "TestDrilling";
        public const string TestIncisionStepId = "TestIncision";
        public const string TestRetractionStepId = "TestRetraction";
        public const string TestVesselCompressionStepId = "TestVesselCompression";

        public const string TestCauterizationId = "TestCauterization";

        public const string TestAmputationEffectId = "TestAmputationEffect";

        public const string TestAmputationComponentId = "TestAmputation";

        public static readonly string Prototypes = $@"
- type: surgeryOperation
  id: {TestAmputationOperationId}
  steps:
  - {TestIncisionStepId}
  - {TestVesselCompressionStepId}
  - {TestRetractionStepId}
  - {TestAmputationOperationId}
  effect:
    !type:{TestAmputationEffectId}

- type: surgeryStep
  id: {TestAmputationStepId}

- type: surgeryStep
  id: {TestCauterizationStepId}

- type: surgeryStep
  id: {TestDrillingStepId}

- type: surgeryStep
  id: {TestIncisionStepId}

- type: surgeryStep
  id: {TestRetractionStepId}

- type: surgeryStep
  id: {TestVesselCompressionStepId}

- type: entity
  id: {DrapesDummyId}
  components:
  - type: SurgeryDrapes
  - type: Item

- type: entity
  id: {IncisionDummyId}
  components:
  - type: Item
  - type: SurgeryTool
    behavior:
      !type:StepSurgery
      step: {TestIncisionStepId}
    delay: 0

- type: entity
  id: {VesselCompressionDummyId}
  components:
  - type: Item
  - type: SurgeryTool
    behavior:
      !type:StepSurgery
      step: {TestVesselCompressionStepId}
    delay: 0

- type: entity
  id: {RetractionDummyId}
  components:
  - type: Item
  - type: SurgeryTool
    behavior:
      !type:StepSurgery
      step: {TestRetractionStepId}
    delay: 0

- type: entity
  id: {CauteryDummyId}
  components:
  - type: Item
  - type: SurgeryTool
    behavior:
      !type:Cauterization
      step: {TestCauterizationStepId}
    delay: 0

- type: entity
  id: {AmputationDummyId}
  components:
  - type: Item
  - type: SurgeryTool
    behavior:
      !type:StepSurgery
      step: {TestAmputationStepId}
    delay: 0";
    }
}
