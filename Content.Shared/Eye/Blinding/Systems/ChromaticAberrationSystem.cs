using Content.Shared.Eye.Blinding;
using Content.Shared.Eye.Blinding.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Client.Eye.Blinding;

public sealed class ChromaticAberrationSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChromaticAberrationComponent, ComponentInit>(OnComponentInit);
    }
	
	private void SetProtanopia(ChromaticAberrationComponent component)
	{
		component.A1 = 0.1121f;
		component.A2 = 0.8853f;
		component.A3 = -0.0005f;
		component.B1 = 0.1127f;
		component.B2 = 0.8897f;
		component.B3 = -0.0001f;
		component.C1 = 0.0045f;
		component.C2 = 0f;
		component.C3 = 1.0019f;
	}
	
	private void SetDeuteranopia(ChromaticAberrationComponent component)
	{
		component.A1 = 0.292f;
		component.A2 = 0.7054f;
		component.A3 = -0.0003f;
		component.B1 = 0.2934f;
		component.B2 = 0.7089f;
		component.B3 = 0f;
		component.C1 = -0.02098f;
		component.C2 = 0.02559f;
		component.C3 = 1.0019f;
	}
	
	private void SetTritanopia(ChromaticAberrationComponent component)
	{
		component.A1 = 1.01595f;
		component.A2 = 0.1351f;
		component.A3 = -0.1488f;
		component.B1 = -0.01542f;
		component.B2 = 0.8683f;
		component.B3 = 0.1448f;
		component.C1 = 0.1002f;
		component.C2 = 0.8168f;
		component.C3 = 0.1169f;
	}
	
	private void PickColorBlindness(ChromaticAberrationComponent component)
	{
		var type = _random.Next(3);
		if (type == 0)
		{
			SetProtanopia(component);
			return;
		}
		if (type == 1)
		{
			SetDeuteranopia(component);
			return;
		}
		if (type == 2)
		{
			SetTritanopia(component);
			return;
		}
	}
	
	private void RandomiseColorBlindness(ChromaticAberrationComponent component)
	{
		component.A1 = _random.NextFloat(-0.5f, 1f);
		component.A2 = _random.NextFloat(-0.5f, 1f);
		component.A3 = _random.NextFloat(-0.5f, 1f);
		component.B1 = _random.NextFloat(-0.5f, 1f);
		component.B2 = _random.NextFloat(-0.5f, 1f);
		component.B3 = _random.NextFloat(-0.5f, 1f);
		component.C1 = _random.NextFloat(-0.5f, 1f);
		component.C2 = _random.NextFloat(-0.5f, 1f);
		component.C3 = _random.NextFloat(-0.5f, 1f);
	}
	
	private void OnComponentInit(EntityUid uid, ChromaticAberrationComponent component, ComponentInit args)
    {
        if (component.SetPreset is null)
			return;
		if(component.SetPreset == "Protanopia")
			SetProtanopia(component);
		if(component.SetPreset == "Deuteranopia")
			SetDeuteranopia(component);
		if(component.SetPreset == "Tritanopia")
			SetTritanopia(component);
		if(component.SetPreset == "Pick")
			PickColorBlindness(component);
		if(component.SetPreset == "Random")
			RandomiseColorBlindness(component);
		
		component.SetPreset = null;
		Dirty(component);
    }
}
