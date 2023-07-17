namespace Content.Server.Fax;

public static class FaxConstants
{
    // Commands

    /**
     * Used to get other faxes connected to current network
     */
    public const string FaxPingCommand = "fax_ping";

    /**
     * Used as response to ping command
     */
    public const string FaxPongCommand = "fax_pong";

    /**
     * Used when fax sending data to destination fax
     */
    public const string FaxPrintCommand = "fax_print";

    // Data

    public const string FaxNameData = "fax_data_name";
    public const string FaxSyndicateData = "fax_data_i_am_syndicate";

    public const string FaxPaperDataToCopy = "fax_data_to_copy";
    public const string FaxPaperMetaData = "fax_data_meta";
}
