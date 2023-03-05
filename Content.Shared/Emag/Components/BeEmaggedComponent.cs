namespace Content.Shared.Emag.Components;
	[RegisterComponent]
     public sealed class BeEmaggedComponent : Component
     {

      [ViewVariables(VVAccess.ReadWrite)]
      public bool Enabled = true;
		

     }