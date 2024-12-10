using Content.Shared.PAI;
using Content.Shared.Interaction.Events;
namespace Content.Client.PAI

{
    public sealed class PAISystem : SharedPAISystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PAIComponent, UseInHandEvent>(OnUseInHand);
        }
        private void OnUseInHand(EntityUid uid, PAIComponent component, UseInHandEvent args)
        {
            args.Handled = true;
        }

    }
}
