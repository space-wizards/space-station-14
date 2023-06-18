using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;

namespace Content.Server.Chemistry.Components.SolutionManager
{
    [RegisterComponent]
    [Access(typeof(SolutionContainerSystem))]
    public sealed class SolutionContainerManagerComponent : Component
    {
        [DataField("solutions")]
        [Access(typeof(SolutionContainerSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
        public readonly Dictionary<string, Solution> Solutions = new();
		
		[DataField("matchContentsName")]
		public bool MatchContentsName = false;
		
        [DataField("matchNameFull")]
        public string MatchNameFull = "transformable-container-component-glass";

		[DataField("emptyName")]
        public string EmptyName = string.Empty;
		
		[DataField("emptyDescription")]
        public string EmptyDescription = string.Empty;
    }
}
