using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Speech.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Changeling;

[RegisterComponent]
public sealed partial class StoredIdentityComponent : Component
{
    /// <summary>
    /// The DNA of a stored Identity
    /// </summary>
    [DataField]
    public DnaData? IdentityDna;

    /// <summary>
    /// The vocal information about the Identity
    /// </summary>
    [DataField]
    public VocalComponent? IdentityVocals;

    /// <summary>
    /// The appearance associated with the Identity
    /// </summary>
    [DataField]
    public AppearanceComponent? IdentityAppearance;

    ///TODO: Figure out how to handle unique organs like Vox lungs, Moth/lizard dietary restrictions
    /// <summary>
    /// The Identities organs, such as their unique lung situation.
    /// </summary>
    [DataField]
    public Container IdentityOrgans;

}

