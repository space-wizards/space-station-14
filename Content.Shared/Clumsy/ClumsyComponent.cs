using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Clumsy;

/// <summary>
/// A simple clumsy tag-component.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ClumsyComponent : Component
{

    // Standard options. Try to fit these in if you can!

    /// <summary>
    ///     Sound to play when clumsy interactions fail.
    /// </summary>
    [DataField]
    public SoundSpecifier ClumsySound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

    /// <summary>
    ///     Default chance to fail a clumsy interaction.
    ///     If a system needs to use something else, add a new variable in the component, do not modify this percentage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ClumsyDefaultCheck = 0.5f;

    /// <summary>
    ///     Default stun time.
    ///     If a system needs to use something else, add a new variable in the component, do not modify this number.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ClumsyDefaultStunTime = TimeSpan.FromSeconds(2.5);

    // Specific options

    /// <summary>
    ///     Sound to play after hitting your head on a table. Ouch!
    /// </summary>
    [DataField]
    public SoundCollectionSpecifier TableBonkSound = new SoundCollectionSpecifier("TrayHit");

    /// <summary>
    ///     Stun time after failing to shoot a gun.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan GunShootFailStunTime = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     Stun time after failing to shoot a gun.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier? GunShootFailDamage;

    /// <summary>
    ///     Noise to play after failing to shoot a gun. Boom!
    /// </summary>
    [DataField]
    public SoundSpecifier GunShootFailSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/bang.ogg");
	
	/// <summary>
	///		Imp. Whether or not to apply Clumsy to hyposprays.
	/// </summary>
	[DataField]
	public bool ClumsyHypo = true;
	
	/// <summary>
	///		Imp. Whether or not to apply Clumsy to defibs.
	/// </summary>
	[DataField]
	public bool ClumsyDefib = true;
	
	/// <summary>
	///		Imp. Whether or not to apply Clumsy to guns.
	/// </summary>
	[DataField]
	public bool ClumsyGuns = true;
	
	/// <summary>
	///		Imp. Whether or not to apply Clumsy to vaulting.
	/// </summary>
	[DataField]
	public bool ClumsyVaulting = true;
	
	/// <summary>
	///		Imp. Lets you define a new "failed" message for each event.
	/// </summary>
	[DataField]
	public string HypoFailedMessage = "hypospray-component-inject-self-clumsy-message";
	
	[DataField]
	public string GunFailedMessage = "gun-clumsy";
	
	[DataField]
	public string VaulingFailedMessageSelf = "bonkable-success-message-user";
	
	[DataField]
	public string VaulingFailedMessageOthers = "bonkable-success-message-others";
	
	[DataField]
	public string VaulingFailedMessageForced = "forced-bonkable-success-message";
}
