namespace Content.Shared.Utility;

/// <summary>
/// Contains utility extensions for strings.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Defines utility extensions for strings.
    /// </summary>
    /// <param name="str">The string used as a receiver.</param>
    extension(string str)
    {
        /// <summary>
        /// Truncates a string to the desired length.
        /// </summary>
        /// <param name="threshold">The threshold past which all characters will be truncated.</param>
        /// <param name="trail">An optional trail that will be added to the end of the shortened string, such as an ellipsis.</param>
        /// <returns></returns>
        public string Shorten(int threshold, string trail = "")
        {
            return str.Length <= threshold ? str : str[..threshold].TrimEnd() + trail;
        }
    }
}
