namespace XmlPatch
{
    /// <summary>
    /// Тип действия для оригинальных XML узлов, найденных по соответствующему узлу патча.
    /// </summary>
    internal enum NodeAction
    {
        /// <summary>
        /// Стандартный мержинг для первого найденного узла.
        /// </summary>
        Merge,

        /// <summary>
        /// Создать новый узел в документе, игнорируя мержинг по найденным.
        /// </summary>
        Create,

        /// <summary>
        /// Удалить узел из документа.
        /// </summary>
        Remove,

        /// <summary>
        /// Стандартный мержинг всех найденных узлов в документе.
        /// </summary>
        MergeAll,

        /// <summary>
        /// Удалить все найденные узлы из документа.
        /// </summary>
        RemoveAll
    }
}