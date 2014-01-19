namespace XmlPatch
{
    /// <summary>
    /// “ип создани€ нового узла относительно существующего.
    /// </summary>
    internal enum CraftMode
    {
        /// <summary>
        /// ¬ставить новый узел после существующего.
        /// </summary>
        After,

        /// <summary>
        /// ¬ставить новый узел перед существующим.
        /// </summary>
        Before,

        /// <summary>
        /// ƒобавить новый узел в потомки существующего.
        /// </summary>
        Append
    }
}