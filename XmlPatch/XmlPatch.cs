using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;

namespace XmlPatch
{
    /// <summary>
    /// Предоставляет функционал для патчинга или мержинга XML-файлов.
    /// </summary>
    public static class XmlPatch
    {
        #region Xml patch definitions

        /// <summary>
        /// Основная схема. Определяется в файле патча для использования специальных атрибутов мержинга.
        /// </summary>
        internal const string SchemeNamespace = "http://schemas.xmlpatch";

        /// <summary>
        /// Дополнительная базовая схема для обычных xml-элементов в документе (аттрибуты, узлы), которым нужно определить специальный смысл. 
        /// При мержинге, такие xml-элементы не игнорируются, игнорируются только заданные префиксы для этой схемы.
        /// </summary>
        internal const string BaseModSchemeNamespace = SchemeNamespace + "/mod";

        /// <summary>
        /// Дополнительная схема для пометки атрибута, по которому будет производиться мержинг.
        /// Является более простой альтернативой атрибуту <see cref="FindNodeAttributeName"/> со значением вида "mergeNode[@mergeAttr='val']".
        /// Игнорируется, если задан атрибут <see cref="FindNodeAttributeName"/>.
        /// </summary>
        internal const string PrimaryAttributeSchemeNamespace = BaseModSchemeNamespace + "/attr/key";

        /// <summary>
        /// Дополнительная схема для задания типа мержинга узла <see cref="NodeAction"/>.
        /// <see cref="NodeAction"/> указывается после слеша в конце этой схемы.
        /// Является более простой альтернативой атрибуту <see cref="NodeActionAttributeName"/> со значением <see cref="NodeAction"/>.
        /// </summary>
        internal const string NodeActionSchemeNamespace = BaseModSchemeNamespace + "/node";

        /// <summary>
        /// Специальный атрибут, задающий тип мержинга узла.
        /// В качестве значения задается один из членов <see cref="NodeAction"/>.
        /// Это не обязательный атрибут. Значение по умолчанию <see cref="NodeAction.Merge"/>.
        /// Значение не чувствительно к регистру.
        /// </summary>
        internal const string NodeActionAttributeName = "action";

        /// <summary>
        /// Специальный атрибут, задающий выражение XPath для поиска в исходном документе подходящего узла для мержинга.
        /// Это не обязательный атрибут. Значение по умолчанию генерируется автоматически по наименованию текущего узла 
        /// и по атрибуту со схемой <see cref="PrimaryAttributeSchemeNamespace"/>, если таковой имеется.
        /// </summary>
        internal const string FindNodeAttributeName = "find";

        /// <summary>
        /// Специальный атрибут, задающий выражение для создания узла, если подходящий узел для мержинга не был найден, 
        /// либо тип мержинга имеет значение <see cref="NodeAction.Create"/>.
        /// Выражение задается в виде <see cref="CraftMode"/> + <see cref="CreateNodeAttributeCraftSeparator"/> + "Выражение XPath для поиска узла относительно текущего родительского узла".
        /// Это не обязательный атрибут. Значение по умолчанию "append<see cref="CreateNodeAttributeCraftSeparator">></see>." (добавить узел в конец текущего родительского узла).
        /// </summary>
        internal const string CreateNodeAttributeName = "create";

        /// <summary>
        /// Разделитель в значении атрибута <see cref="CreateNodeAttributeName"/>, 
        /// отделяющий значения <see cref="CraftMode"/> и "Выражение XPath для поиска узла относительно текущего родительского узла".
        /// </summary>
        private const string CreateNodeAttributeCraftSeparator = ">";

        #endregion Xml patch definitions

        /// <summary>
        /// Применить патч для XML файла.
        /// </summary>
        /// <param name="sourceFilePath">Оригинальный файл.</param>
        /// <param name="patchFilePath">Файл, с которым производится слияние, содержащий инструкции патча.</param>
        /// <param name="resultFilePath">Результирующий файл.</param>
        public static void Patch(string sourceFilePath, string patchFilePath, string resultFilePath)
        {
            if (sourceFilePath == null)
                throw new ArgumentNullException("sourceFilePath", "Source file path should be initialized.");

            if (patchFilePath == null)
                throw new ArgumentNullException("patchFilePath", "Patch file path should be initialized.");

            if (resultFilePath == null)
                throw new ArgumentNullException("resultFilePath", "Result file path should be initialized.");

            var sourceDoc = new XmlDocument();
            sourceDoc.Load(sourceFilePath);

            var patchDoc = new XmlDocument();
            patchDoc.Load(patchFilePath);

            Patch(sourceDoc, patchDoc);

            sourceDoc.Save(resultFilePath);
        }

        /// <summary>
        /// Применить патч для XML файла.
        /// </summary>
        /// <param name="sourceAndResultFile">Оригинальный и результирующий файл.</param>
        /// <param name="patchFilePath">Файл, с которым производится слияние, содержащий инструкции патча.</param>
        public static void Patch(string sourceAndResultFile, string patchFilePath)
        {
            Patch(sourceAndResultFile, patchFilePath, sourceAndResultFile);
        }

        /// <summary>
        /// Применить патч для XML документа.
        /// </summary>
        /// <param name="sourceDoc">Оригинальный и результирующий документ.</param>
        /// <param name="patchDoc">Документ, с которым производится слияние, содержащий инструкции патча.</param>
        public static void Patch(XmlDocument sourceDoc, XmlDocument patchDoc)
        {
            Patch((XmlNode)sourceDoc, patchDoc);
        }

        /// <summary>
        /// Применить патч для узла XML документа.
        /// </summary>
        /// <param name="sourceRoot">Оригинальный и результирующий узел.</param>
        /// <param name="patchRoot">Узел, с которым производится слияние, содержащий инструкции патча.</param>
        public static void Patch(XmlNode sourceRoot, XmlNode patchRoot)
        {
            if (sourceRoot == null)
                throw new ArgumentNullException("sourceRoot", "Source of xml-data should be initialized.");

            if (patchRoot == null)
                throw new ArgumentNullException("patchRoot", "Xml-data of a patch should be initialized.");

            var dict = new Dictionary<XmlNode, XmlNode> { { patchRoot, sourceRoot  } };

            var patchNodes = GetAllNodes(patchRoot is XmlDocument ? (XmlDocument)patchRoot : patchRoot.OwnerDocument);
            foreach (XmlNode patchNode in patchNodes)
            {
                Debug.Assert(patchNode.ParentNode != null, "patchNode must have a parent.");
                Debug.Assert(dict.ContainsKey(patchNode.ParentNode), "dict should contain the parent of patchNode.");
                XmlNode parentSource = dict[patchNode.ParentNode];

                string actionValue = TryGetAttributeValue(patchNode, NodeActionAttributeName, SchemeNamespace);
                if (actionValue == null && patchNode.NamespaceURI.StartsWith(NodeActionSchemeNamespace))
                    actionValue = patchNode.NamespaceURI.Replace(NodeActionSchemeNamespace, string.Empty).TrimStart('/');

                NodeAction action;
                if (actionValue == null)
                {
                    action = NodeAction.Merge;
                }
                else if (!Enum.TryParse(actionValue, true, out action))
                {
                    var possibleValues = string.Join(", ", Enum.GetNames(typeof(NodeAction)));
                    string msg = string.Format(
                        "Value of the node action is not valid - \"{0}\" (node \"{1}\"). The possible values: {2}",
                        actionValue,
                        patchNode,
                        possibleValues);
                    throw new XmlException(msg);
                }

                XmlNode[] foundSourceNodes;
                if (action != NodeAction.Create)
                {
                    string xname = TryGetAttributeValue(patchNode, FindNodeAttributeName, SchemeNamespace) ?? GetNodeXName(patchNode, true);
                    Debug.Assert(xname != null, "xname != null");
                    XmlNodeList foundNodes = parentSource.SelectNodes(xname);
                    Debug.Assert(foundNodes != null, "foundNodes != null");
                    // Фильтрация уже смерженных ранее узлов.
                    foundSourceNodes = foundNodes.OfType<XmlNode>().Where(node => !dict.ContainsValue(node)).ToArray();
                }
                else
                {
                    foundSourceNodes = null;
                }

                switch (action)
                {
                    case NodeAction.Merge:
                    case NodeAction.MergeAll:
                    case NodeAction.Create:
                        Debug.Assert(foundSourceNodes != null, "foundSourceNodes != null");
                        if (action == NodeAction.Create || foundSourceNodes.Length == 0)
                        {
                            XmlNode craftedNode = CraftSourceNode(patchNode, parentSource);
                            if (craftedNode == null)
                                break;

                            foundSourceNodes = new[] { craftedNode };
                        }

                        foreach (XmlNode foundSourceNode in foundSourceNodes)
                        {
                            MergeNodes(foundSourceNode, patchNode);
                            dict.Add(patchNode, foundSourceNode);
                            if (action != NodeAction.MergeAll)
                                break;
                        }
                        break;

                    case NodeAction.Remove:
                    case NodeAction.RemoveAll:
                        Debug.Assert(foundSourceNodes != null, "foundSourceNodes != null");
                        foreach (XmlNode foundSourceNode in foundSourceNodes)
                        {
                            parentSource.RemoveChild(foundSourceNode);
                            if (action != NodeAction.RemoveAll)
                                break;
                        }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Перечислить все узлы в XML документе.
        /// </summary>
        /// <param name="doc">XML документ.</param>
        /// <returns>Перечисление всех узлов в порядке иерархии.</returns>
        internal static IEnumerable<XmlNode> GetAllNodes(XmlDocument doc)
        {
            var list = new List<XmlNode> { doc.DocumentElement };
            list.AddRange(GetAllChildNodes(doc.DocumentElement));
            return list;
        }

        /// <summary>
        /// Перечислить все дочерние узлы в указанном XML узле.
        /// </summary>
        /// <param name="node">XML узел.</param>
        /// <returns>Перечисление всех узлов в порядке иерархии.</returns>
        private static IEnumerable<XmlNode> GetAllChildNodes(XmlNode node)
        {
            var list = new List<XmlNode>();
            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.NodeType == XmlNodeType.Element)
                    list.Add(childNode);

                list.AddRange(GetAllChildNodes(childNode));
            }

            return list;
        }

        /// <summary>
        /// Получить XPath селектор для XML узла относительно его родителя.
        /// </summary>
        /// <param name="node">XML узел, селектор которого нужно получить.</param>
        /// <param name="withPrimaryAttribute">Нужно ли включить в XPath ключевой атрибут <see cref="PrimaryAttributeSchemeNamespace"/>, если таковой имеется.</param>
        /// <returns>XPath выражение для выборки узла <see cref="node"/> относительно его родителя.</returns>
        private static string GetNodeXName(XmlNode node, bool withPrimaryAttribute)
        {
            bool isModNode = node.NamespaceURI.StartsWith(BaseModSchemeNamespace);
            string xname = isModNode ? node.LocalName : node.Name;

            if (withPrimaryAttribute)
            {
                XmlAttribute primaryAttr = TryGetModAttribute(node, PrimaryAttributeSchemeNamespace);
                if (primaryAttr != null)
                    xname += string.Format("[@{0} = '{1}']", primaryAttr.LocalName, primaryAttr.Value);
            }

            return xname;
        }

        /// <summary>
        /// Попытаться получить значение атрибута в XML узле.
        /// </summary>
        /// <param name="node">XML узел, в котором ищется атрибут, значение которого нужно получить.</param>
        /// <param name="localName">Локальное имя атрибута.</param>
        /// <param name="namespaceUri">Пространство имен атрибута.</param>
        /// <returns>Значение атрибута, либо <c>null</c>, если атрибут не найден.</returns>
        private static string TryGetAttributeValue(XmlNode node, string localName, string namespaceUri)
        {
            if (node.Attributes == null)
                return null;

            var attr = node.Attributes[localName, namespaceUri];
            if (attr == null)
                return null;

            return attr.Value;
        }

        /// <summary>
        /// Получить атрибут с указанным пространством имен в XML узле.
        /// <para/>Используется для атрибутов с пространством имен <see cref="BaseModSchemeNamespace"/>.
        /// </summary>
        /// <param name="node">XML узел, в котором ищется атрибут.</param>
        /// <param name="namespaceUri">Пространство имен атрибута.</param>
        /// <returns>Первый найденный атрибут, либо <c>null</c>, если атрибут с пространством имен <see cref="namespaceUri"/> отсутствует.</returns>
        private static XmlAttribute TryGetModAttribute(XmlNode node, string namespaceUri)
        {
            if (node.Attributes == null)
                return null;

            var attr = node.Attributes.OfType<XmlAttribute>().FirstOrDefault(x => x.NamespaceURI == namespaceUri);
            return attr;
        }

        /// <summary>
        /// Смержить два узла.
        /// </summary>
        /// <param name="sourceNode">Исходный и результирующий узел.</param>
        /// <param name="mergeNode">Узел, с которым происходит мержинг.</param>
        private static void MergeNodes(XmlNode sourceNode, XmlNode mergeNode)
        {
            var mergeNodeAttributes = mergeNode.Attributes;
            var sourceNodeAttributes = sourceNode.Attributes;
            if (mergeNodeAttributes != null && sourceNodeAttributes != null)
            {
                foreach (XmlAttribute mergeAttr in mergeNodeAttributes)
                {
                    if (mergeAttr.NamespaceURI == SchemeNamespace)
                        continue;

                    if (mergeAttr.Prefix == "xmlns" && mergeAttr.Value != null && mergeAttr.Value.StartsWith(SchemeNamespace))
                        continue;

                    bool isModAttr = mergeAttr.NamespaceURI.StartsWith(BaseModSchemeNamespace);

                    var sourceAttr = sourceNodeAttributes[isModAttr ? mergeAttr.LocalName : mergeAttr.Name];
                    if (sourceAttr == null)
                    {
                        var sourceDoc = sourceNode.OwnerDocument;
                        Debug.Assert(sourceDoc != null, "sourceNode must belong to the OwnerDocument.");
                        sourceAttr = isModAttr 
                                        ? sourceDoc.CreateAttribute(null, mergeAttr.LocalName, null) 
                                        : sourceDoc.CreateAttribute(mergeAttr.Prefix, mergeAttr.LocalName, mergeAttr.NamespaceURI);
                        sourceAttr.Value = mergeAttr.Value;
                        sourceNodeAttributes.Append(sourceAttr);
                    }

                    sourceAttr.Value = mergeAttr.Value;
                }
            }

            if (!string.IsNullOrEmpty(mergeNode.Value))
                sourceNode.Value = mergeNode.Value;

            if (mergeNode.ChildNodes.Count == 1 && mergeNode.FirstChild.NodeType != XmlNodeType.Element)
                sourceNode.InnerXml = mergeNode.InnerXml;
        }

        /// <summary>
        /// Создать новый узел в оригинальном документе на основе узла из документа патча. 
        /// </summary>
        /// <param name="patchNode">Узел, содержащий инструкции патча.</param>
        /// <param name="parentSource">Родительский узел оригинального документа, относительно которого нужно создать новый узел.</param>
        /// <returns>Новый узел в оригинальном документе с наименованием узла <see cref="patchNode"/>, но не смерженный с ним.</returns>
        private static XmlNode CraftSourceNode(XmlNode patchNode, XmlNode parentSource)
        {
            XmlNode refSourceNode;
            CraftMode craftMode;
            string craftAttrValue = TryGetAttributeValue(patchNode, CreateNodeAttributeName, SchemeNamespace);
            if (craftAttrValue != null)
            {
                if (!craftAttrValue.Contains(CreateNodeAttributeCraftSeparator))
                    throw new XmlException(string.Format("Unsupported attribute value (missing {0}): {1}", CreateNodeAttributeCraftSeparator, craftAttrValue));

                string[] craftAttrArgs = craftAttrValue.Split(new[] { CreateNodeAttributeCraftSeparator }, 2, StringSplitOptions.None);
                string craftModeStr = craftAttrArgs[0];
                string xpath = craftAttrArgs[1];

                refSourceNode = parentSource.SelectSingleNode(xpath);
                if (refSourceNode == null)
                    return null;

                if (!Enum.TryParse(craftModeStr, true, out craftMode))
                {
                    var possibleValues = string.Join(", ", Enum.GetNames(typeof(CraftMode)));
                    string msg = string.Format(
                        "Value of the create mode is not valid - \"{0}\" (node \"{1}\"). The possible values: {2}",
                        craftModeStr,
                        patchNode,
                        possibleValues);
                    throw new XmlException(msg);
                }
            }
            else
            {
                refSourceNode = parentSource;
                craftMode = CraftMode.Append;
            }

            string nodeName = GetNodeXName(patchNode, false);
            Debug.Assert(refSourceNode.OwnerDocument != null, "refSourceNode must belong to the OwnerDocument.");
            var node = refSourceNode.OwnerDocument.CreateElement(nodeName);

            switch (craftMode)
            {
                case CraftMode.After:
                case CraftMode.Before:
                    {
                        var parentNode = refSourceNode.ParentNode;
                        if (parentNode == null)
                            throw new XmlException(string.Format("Сan not perform the operation \"{0}\" because the item \"{1}\" has no parent node", craftMode, refSourceNode));

                        if (craftMode == CraftMode.After)
                            parentNode.InsertAfter(node, refSourceNode);
                        else
                            parentNode.InsertBefore(node, refSourceNode);
                    }
                    break;

                case CraftMode.Append:
                    refSourceNode.AppendChild(node);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return node;
        }
    }
}
