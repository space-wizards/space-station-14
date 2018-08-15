using Content.Shared.Input;
using SS14.Shared.Input;

namespace Content.Client.Input
{
    /// <summary>
    ///     Contains a helper function for setting up all content 
    ///     contexts, and modifying existing engine ones.
    /// </summary>
    public static class ContentContexts
    {
        public static void SetupContexts(IInputContextContainer contexts)
        {
            var human = contexts.GetContext("human");
            human.AddFunction(ContentKeyFunctions.SwapHands);
            human.AddFunction(ContentKeyFunctions.Drop);
            human.AddFunction(ContentKeyFunctions.ActivateItemInHand);
            human.AddFunction(ContentKeyFunctions.OpenCharacterMenu);
            human.AddFunction(ContentKeyFunctions.ExamineEntity);
            human.AddFunction(ContentKeyFunctions.UseItemInHand);
        }
    }
}
