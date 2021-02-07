using Content.Shared.Chemistry;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    public enum ReactionMethod
    {
        Touch,
        Injection,
        Ingestion,
    }

    public interface IReagentReaction
    {
        ReagentUnit ReagentReactTouch(ReagentPrototype reagent, ReagentUnit volume) => ReagentUnit.Zero;
        ReagentUnit ReagentReactInjection(ReagentPrototype reagent, ReagentUnit volume) => ReagentUnit.Zero;
        ReagentUnit ReagentReactIngestion(ReagentPrototype reagent, ReagentUnit volume) => ReagentUnit.Zero;
    }
}
