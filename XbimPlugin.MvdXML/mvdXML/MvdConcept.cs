﻿using System.Collections.Generic;
using System.Text;
using System.Xml.XPath;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;

namespace Validation.mvdXML
{
    public class MvdConcept : MVDNamedIdentifiedItem
    {
        public MvdConcept(MvdXMLDocument mvdXMLDocument, XPathNavigator xPathNavigator)
            : base (mvdXMLDocument, xPathNavigator)
        {
            Override = (xPathNavigator.GetAttribute("override", "") == "true") ? true : false;
            Author = xPathNavigator.GetAttribute("author", "");
            if (Author != "")
            {

            }

            XPathNavigator childNav = xPathNavigator.Clone();
            childNav.MoveToChild("Template", mvdXMLDocument.fileNameSpace);
            TemplateUUID = childNav.GetAttribute("ref", "");
            // reqs
            childNav = xPathNavigator.Clone();
            childNav.MoveToChild("Requirements", mvdXMLDocument.fileNameSpace);
            var ret = childNav.MoveToChild("Requirement", mvdXMLDocument.fileNameSpace);
            while (ret)
            {
                MvdConceptERReference v = new MvdConceptERReference(mvdXMLDocument, childNav, uuid);
                ret = childNav.MoveToNext("Requirement", mvdXMLDocument.fileNameSpace);
            }

            // rules
            childNav = xPathNavigator.Clone();
            childNav.MoveToChild("Rules", mvdXMLDocument.fileNameSpace);
            ret = childNav.MoveToChild("TemplateRule", mvdXMLDocument.fileNameSpace);

            TemplateRuleProperties = new List<MvdRulePropertySet>();
            while (ret)
            {
                string param = childNav.GetAttribute("Parameters", "");
                MvdRulePropertySet s = new MvdRulePropertySet(param);
                TemplateRuleProperties.Add(s);
                ret = childNav.MoveToNext("TemplateRule", mvdXMLDocument.fileNameSpace);
            }
            mvdXMLDocument.AddConcept(this);
            mvdXMLDocument.LoadConceptTemplate(TemplateUUID);
        }

        public MvdConceptTemplate ConceptTemplate
        {
            get
            {
                if (!TopXmlDoc.ConceptTemplates.ContainsKey(TemplateUUID))
                    return null;
                return TopXmlDoc.ConceptTemplates[TemplateUUID];
            }
        }

        public string Author { get; set; }

        public string TemplateUUID { get; set; }
        public bool Override { get; set; }
        public List<MvdRulePropertySet> TemplateRuleProperties { get; set; }
        internal string StringReport()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Concept: {0} ref template: {1} root: {2} root applies: {3}\r\n", Name, TemplateUUID, ConceptRoot.uuid, ConceptRoot.applicableRootEntity);
            MvdConceptTemplate t = null;
            if (TopXmlDoc.ConceptTemplates.ContainsKey(TemplateUUID))
            {
                t = TopXmlDoc.ConceptTemplates[TemplateUUID];
            }

            if (TemplateRuleProperties.Count > 0)
            {
                sb.AppendFormat("Rules:\r\n", Name, TemplateUUID);
                foreach (var item in TemplateRuleProperties)
                {
                    sb.Append(item.StringReport());
                    foreach (var parname in item.ParValues())
                    {
                        sb.AppendFormat("\t\t{0} -> {1}\r\n", t.GetPropRuleQS(parname.Name), parname.Val);
                    }
                }
            }
            if (false)
            {
                if (t != null)
                    sb.Append(t.StringReport());
                sb.AppendLine("template missing");
            }
            return sb.ToString();
        }

        public MvdConceptRoot ConceptRoot { get; set; }

        internal bool AppliesTo(IPersistIfcEntity SelectedEntity)
        {
            IfcType ifcType = IfcMetaData.IfcType(ConceptRoot.applicableRootEntity.ToUpperInvariant());
            IfcType seltype = IfcMetaData.IfcType(SelectedEntity);
            return (ifcType == seltype || ifcType.IfcSubTypes.Contains(seltype));
        }
    }
}
