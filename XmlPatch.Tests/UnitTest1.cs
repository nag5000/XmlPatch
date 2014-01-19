// ReSharper disable ConvertToConstant.Local

using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace XmlPatch.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void SelectAllNodesFromDocumentTest()
        {
            var doc = new XmlDocument();
            doc.LoadXml(@"<root>
                            <settings>
                                <node attr='foo' />
                                <node gaga='asd'>123456</node>
                            </settings>
                            <settings2>
                                <node>blabla</node>
                            </settings2>
                        </root>");

            var nodes = XmlPatch.GetAllNodes(doc);
            Assert.AreEqual(6, nodes.Count());
        }

        [TestMethod]
        public void SimpleMergeTest()
        {
            CheckResult(
                        @"<root>
                            <settings>
                                <node/>
                                <node/>
                            </settings>
                            <settings2>
                                <node/>
                            </settings2>
                        </root>",

                        @"<root>
                            <settings2>
                                <newnode/>
                            </settings2>
                        </root>",

                        @"<root>
                            <settings>
                                <node/>
                                <node/>
                            </settings>
                            <settings2>
                                <node/>
                                <newnode/>
                            </settings2>
                        </root>"
                );
        }

        [TestMethod]
        public void SimpleMergeByModAttrsTest()
        {
            CheckResult(
                @"<root>
                    <settings>
                        <node/>
                        <node/>
                    </settings>
                    <settings2>
                        <node/>
                    </settings2>
                </root>",

                string.Format(
                @"<root xmlns:merge=""{0}/{1}"">
                    <settings2>
                        <merge:newnode />
                        <newnode />
                    </settings2>
                </root>", XmlPatch.NodeActionSchemeNamespace, NodeAction.Merge),

                @"<root>
                    <settings>
                        <node/>
                        <node/>
                    </settings>
                    <settings2>
                        <node/>
                        <newnode />
                        <newnode />
                    </settings2>
                </root>"
                );
        }

        [TestMethod]
        public void CheckingAlreadyExistingNodeTest()
        {
            string sourceXml = @"<root>
                                    <settings>
                                        <node/>
                                        <node/>
                                    </settings>
                                    <settings2>
                                        <node/>
                                        <newnode/>
                                    </settings2>
                                </root>";

            string patchXml = @"<root>
                                   <settings2>
                                       <newnode/>
                                   </settings2>
                              </root>";

            string resultXml = sourceXml;

            CheckResult(sourceXml, patchXml, resultXml);
        }

        [TestMethod]
        public void AccountAlreadyMergedNodesTest()
        {
            CheckResult(
                    @"<root>
                        <settings>
                            <node/>
                            <node/>
                        </settings>
                        <settings2>
                            <node/>
                            <newnode/>
                        </settings2>
                    </root>",

                    @"<root>
                        <settings2>
                            <newnode/>
                            <newnode/>
                        </settings2>
                    </root>",

                    @"<root>
                        <settings>
                            <node/>
                            <node/>
                        </settings>
                        <settings2>
                            <node/>
                            <newnode/>
                            <newnode/>
                        </settings2>
                    </root>"
                    );
        }

        [TestMethod]
        public void DefaultPatchAttributesDoNotChangeOriginalFileTest()
        {
            string sourceXml = @"<root>
                                    <settings>
                                        <node/>
                                        <node/>
                                    </settings>
                                    <settings2>
                                        <node/>
                                        <newnode/>
                                        <newnode/>
                                    </settings2>
                                </root>";

            string patchXml = string.Format(
                @"<root xmlns:patch=""{0}"">
                                   <settings2>
                                       <newnode patch:{1}=""{2}"" />
                                       <newnode/>
                                   </settings2>
                              </root>", XmlPatch.SchemeNamespace, XmlPatch.NodeActionAttributeName, NodeAction.Merge);

            string resultXml = sourceXml;

            CheckResult(sourceXml, patchXml, resultXml);
        }

        [TestMethod]
        public void RemoveNodesSimpleTest()
        {
            string sourceXml = @"<root>
                                    <settings>
                                        <node/>
                                        <node/>
                                    </settings>
                                    <settings2>
                                        <node/>
                                        <newnode/>
                                        <newnode/>
                                    </settings2>
                                </root>";

            string patchXml = string.Format(
                             @"<root xmlns:patch=""{0}"">
                                   <settings2>
                                       <newnode patch:{1}=""{2}"" />
                                       <newnode patch:{1}=""{2}"" />
                                   </settings2>
                              </root>", XmlPatch.SchemeNamespace, XmlPatch.NodeActionAttributeName, NodeAction.Remove);

            string expectedXml = @"<root>
                                    <settings>
                                        <node/>
                                        <node/>
                                    </settings>
                                    <settings2>
                                        <node/>
                                    </settings2>
                                 </root>";

            CheckResult(sourceXml, patchXml, expectedXml);
        }

        [TestMethod]
        public void RemoveNodesByModAttrsSimpleTest()
        {
            string sourceXml = @"<root>
                                    <settings>
                                        <node/>
                                        <node/>
                                    </settings>
                                    <settings2>
                                        <node/>
                                        <newnode/>
                                        <newnode/>
                                    </settings2>
                                </root>";

            string patchXml = string.Format(
                             @"<root xmlns:remove=""{0}/{1}"">
                                   <settings2>
                                       <remove:newnode />
                                       <remove:newnode />
                                   </settings2>
                              </root>", XmlPatch.NodeActionSchemeNamespace, NodeAction.Remove);

            string expectedXml = @"<root>
                                    <settings>
                                        <node/>
                                        <node/>
                                    </settings>
                                    <settings2>
                                        <node/>
                                    </settings2>
                                 </root>";

            CheckResult(sourceXml, patchXml, expectedXml);
        }

        [TestMethod]
        public void RemoveAllNodesByModAttrsSimpleTest()
        {
            string sourceXml = @"<root>
                                    <settings>
                                        <node/>
                                        <newnode/>
                                        <node/>
                                    </settings>
                                    <settings2>
                                        <node/>
                                        <newnode/>
                                        <newnode/>
                                    </settings2>
                                </root>";

            string patchXml = string.Format(
                             @"<root xmlns:removeall=""{0}/{1}"">
                                   <settings2>
                                       <removeall:newnode />
                                   </settings2>
                              </root>", XmlPatch.NodeActionSchemeNamespace, NodeAction.RemoveAll);

            string expectedXml = @"<root>
                                    <settings>
                                        <node/>
                                        <newnode/>
                                        <node/>
                                    </settings>
                                    <settings2>
                                        <node/>
                                    </settings2>
                                 </root>";

            CheckResult(sourceXml, patchXml, expectedXml);
        }

        [TestMethod]
        public void MergeByPrimaryAttributeTest()
        {
            string sourceXml = @"<root>
                                    <settings>
                                        <node/>
                                        <node/>
                                    </settings>
                                    <settings2>
                                        <node/>
                                        <node attr=""val""/>
                                        <node/>
                                    </settings2>
                                </root>";

            string patchXml = string.Format(
                             @"<root xmlns:patch=""{0}"">
                                   <settings2>
                                       <node patch:{1}=""node[@attr='val']"" foo=""123"">blabla</node>
                                   </settings2>
                              </root>", XmlPatch.SchemeNamespace, XmlPatch.FindNodeAttributeName);

            string expectedXml = @"<root>
                                    <settings>
                                        <node/>
                                        <node/>
                                    </settings>
                                    <settings2>
                                        <node/>
                                        <node attr=""val"" foo=""123"">blabla</node>
                                        <node/>
                                    </settings2>
                                 </root>";

            CheckResult(sourceXml, patchXml, expectedXml);
        }

        [TestMethod]
        public void MergeByPrimaryAttributeNotFoundTest()
        {
            string sourceXml = @"<root>
                                    <settings>
                                        <node/>
                                        <node/>
                                    </settings>
                                    <settings2>
                                        <node/>
                                        <node attr=""val2""/>
                                        <node/>
                                    </settings2>
                                </root>";

            string patchXml = string.Format(
                             @"<root xmlns:patch=""{0}"">
                                   <settings2>
                                       <node patch:{1}=""node[@attr='val12345']"" attr=""val"" foo=""123"">blabla</node>
                                   </settings2>
                              </root>", XmlPatch.SchemeNamespace, XmlPatch.FindNodeAttributeName);

            string expectedXml = @"<root>
                                    <settings>
                                        <node/>
                                        <node/>
                                    </settings>
                                    <settings2>
                                        <node/>
                                        <node attr=""val2""/>
                                        <node/>
                                        <node attr=""val"" foo=""123"">blabla</node>
                                    </settings2>
                                 </root>";

            CheckResult(sourceXml, patchXml, expectedXml);
        }

        [TestMethod]
        public void MergeByPrimaryAttributeWithChangeAttrValueTest()
        {
            string sourceXml = @"<root>
                                    <settings>
                                        <node/>
                                        <node/>
                                    </settings>
                                    <settings2>
                                        <node/>
                                        <node attr=""val2""/>
                                        <node/>
                                    </settings2>
                                </root>";

            string patchXml = string.Format(
                             @"<root xmlns:patch=""{0}"">
                                   <settings2>
                                       <node patch:{1}=""node[@attr='val2']"" attr=""val"" foo=""123"">blabla</node>
                                   </settings2>
                              </root>", XmlPatch.SchemeNamespace, XmlPatch.FindNodeAttributeName);

            string expectedXml = @"<root>
                                    <settings>
                                        <node/>
                                        <node/>
                                    </settings>
                                    <settings2>
                                        <node/>
                                        <node attr=""val"" foo=""123"">blabla</node>
                                        <node/>
                                    </settings2>
                                 </root>";

            CheckResult(sourceXml, patchXml, expectedXml);
        }

        [TestMethod]
        public void MergeByPrimaryAttributeNotFoundWithCraftAppendTest()
        {
            string sourceXml = @"<root>
                                    <settings>
                                        <node/>
                                        <node/>
                                    </settings>
                                    <settings2>
                                        <node/>
                                        <node attr=""val2""/>
                                        <node/>
                                    </settings2>
                                </root>";

            // "append>." равносильно отсутствию атрибута.
            string patchXml = string.Format(
                             @"<root xmlns:patch=""{0}"">
                                   <settings2>
                                       <node patch:{1}=""node[@attr='val']"" patch:{2}=""append>."" attr=""val2"" foo=""123"">blabla</node>
                                   </settings2>
                              </root>", XmlPatch.SchemeNamespace, XmlPatch.FindNodeAttributeName, XmlPatch.CreateNodeAttributeName);

            string expectedXml = @"<root>
                                    <settings>
                                        <node/>
                                        <node/>
                                    </settings>
                                    <settings2>
                                        <node/>
                                        <node attr=""val2""/>
                                        <node/>
                                        <node attr=""val2"" foo=""123"">blabla</node>
                                    </settings2>
                                 </root>";

            CheckResult(sourceXml, patchXml, expectedXml);
        }

        [TestMethod]
        public void MergeByPrimaryAttributeNotFoundWithCraftAfterTest()
        {
            string sourceXml = @"<root>
                                    <settings>
                                        <node/>
                                        <node/>
                                    </settings>
                                    <settings2>
                                        <node/>
                                        <node attr=""val2""/>
                                        <node/>
                                    </settings2>
                                </root>";

            string patchXml = string.Format(
                             @"<root xmlns:patch=""{0}"">
                                   <settings2>
                                       <node patch:{1}=""node[@attr='val']"" patch:{2}=""after>node[last()]"" attr=""val2"" foo=""123"">blabla</node>
                                   </settings2>
                              </root>", XmlPatch.SchemeNamespace, XmlPatch.FindNodeAttributeName, XmlPatch.CreateNodeAttributeName);

            string expectedXml = @"<root>
                                    <settings>
                                        <node/>
                                        <node/>
                                    </settings>
                                    <settings2>
                                        <node/>
                                        <node attr=""val2""/>
                                        <node/>
                                        <node attr=""val2"" foo=""123"">blabla</node>
                                    </settings2>
                                 </root>";

            CheckResult(sourceXml, patchXml, expectedXml);
        }

        private void CheckResult(string sourceXml, string patchXml, string expectedResultXml)
        {
            var sourceDoc = new XmlDocument();
            sourceDoc.LoadXml(sourceXml);

            var patchDoc = new XmlDocument();
            patchDoc.LoadXml(patchXml);

            XmlPatch.Patch(sourceDoc, patchDoc);

            var expectedDoc = new XmlDocument();
            expectedDoc.LoadXml(expectedResultXml);

            Assert.AreEqual(expectedDoc.InnerXml, sourceDoc.InnerXml);
        }
    }
}
