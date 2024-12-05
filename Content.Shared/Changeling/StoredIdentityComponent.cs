using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Forensics;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Speech.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling;

[RegisterComponent, NetworkedComponent]
public sealed partial class StoredIdentityComponent : Component
{
    [DataField]
    public String? IdentityName;
    [DataField]
    public String? IdentityDescription;
    /// <summary>
    /// The DNA of a stored Identity
    /// </summary>
    [DataField]
    public DnaComponent? IdentityDna;

    /// <summary>
    /// The vocal information about the Identity
    /// </summary>
    [DataField]
    public VocalComponent? IdentityVocals;

    /// <summary>
    /// The appearance associated with the Identity
    /// </summary>
    [DataField]
    public HumanoidAppearanceComponent? IdentityAppearance;

    ///TODO: Figure out how to handle unique organs like Vox lungs, Moth/lizard dietary restrictions
    /// <summary>
    /// The Identities organs, such as their unique lung situation.
    /// </summary>
    [DataField]
    public Container IdentityOrgans;

    [DataField]
    public EntityPrototype? IdentityEntityPrototype;

}

