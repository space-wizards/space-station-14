using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.UI
{
    public sealed class OxygenBarControl : Control
    {
        private ProgressBar _oxygenBar;

        protected override void Initialize()
        {
            base.Initialize();

            _oxygenBar = GetChild<ProgressBar>("OxygenBar");
        }

        public void UpdateOxygenLevel(float currentOxygen, float maxOxygen)
        {
            _oxygenBar.MaxValue = maxOxygen;
            _oxygenBar.Value = currentOxygen;
        }
    }
}
