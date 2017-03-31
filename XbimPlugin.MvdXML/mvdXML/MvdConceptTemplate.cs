﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.XPath;

namespace Validation.mvdXML
{
    public class MvdConceptTemplate : MVDNamedIdentifiedItem
    {
        public MvdConceptTemplate(MvdXMLDocument mvdXMLDocument, XPathNavigator xPathNavigator)
            : base (mvdXMLDocument, xPathNavigator)
        {
            status = xPathNavigator.GetAttribute("status", "");
            applicableSchema = xPathNavigator.GetAttribute("applicableSchema", "");
            applicableEntity = xPathNavigator.GetAttribute("applicableEntity", "");


            XPathNavigator childNav = xPathNavigator.Clone();
            childNav.MoveToChild("Rules", mvdXMLDocument.fileNameSpace);

            Debug.Write("ConceptTemplate: " + uuid);

            var ret = childNav.MoveToFirstChild();
            Rules = new List<MvdRule>();
            while (ret)
            {
                Rules.Add(new MvdRule(childNav, null));
                ret = childNav.MoveToNext();
            }
            Debug.WriteLine("Done");

        }

        public List<MvdRule> Rules;

        public string status { get; set; }
        public string applicableSchema { get; set; }
        public string applicableEntity { get; set; }

        internal string StringReport()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ConceptTemplate: {0}\r\n", Name);
            foreach (var item in Rules)
            {
                sb.Append(item.StringReport(1));
            }
            return sb.ToString();
        }

        internal string GetPropRuleQS(string parname)
        {
            foreach (var item in Rules)
            {
                MvdRule v = item.FindParameterValuePair("RuleID", parname);
                if (v != null)
                    return v.QueryStack();
            }
            return "";
        }
    }
}
