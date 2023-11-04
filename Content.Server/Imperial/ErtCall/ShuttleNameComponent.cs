namespace Content.Server.ErtCall
{
    [RegisterComponent]
    [Access(typeof(ShuttleNameSystem))]
    public sealed partial class ShuttleNameComponent : Component
    {
        /// <summary>
        /// Firs segmet of shuttle name
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("corpName")]
        public string CorpName = "NT";
        /// <summary>
        /// Max number of corp name
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("maxCorpNumber")]
        public int MaxCorpNumber = 99;
        /// <summary>
        /// name of spec of shuttle
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("specName")]
        public string SpecName = "ERT";
        /// <summary>
        /// max number of spec
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("maxSpecNumber")]
        public int MaxSpecNumber = 9999;
        /// <summary>
        /// suffix in end of name (true if you need this)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("suffixName")]
        public bool SuffixName = true;
        /// <summary>
        /// suffix in end of name (true if you need this)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("needShipName")]
        public bool NeedShipName = true;
        /// <summary>
        /// first name of ship
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly), DataField("shipNameFirst")]
        public String ShipNameFirst = "shuttleNameFirst";
        /// <summary>
        /// last name of ship
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly), DataField("shipNameLast")]
        public String ShipNameLast = "shuttleNameLast";
    }
}
