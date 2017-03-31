﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Validation.mvdXML
{
    public class MvdPropertyRuleValue
    {
        public string Name;
        public string Prop;
        public string Val;

        public MvdPropertyRuleValue(string varName, string varProp, string varVal)
        {
            Name = varName;
            Prop = varProp;
            Val = varVal;
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}='{2}'", Name, Prop, Val);
        }

        internal static List<MvdPropertyRuleValue> GetValues(string _s)
        {
            List<MvdPropertyRuleValue> vals = new List<MvdPropertyRuleValue>();
            var parts = _s.Split(new[] { " AND ", ";" }, StringSplitOptions.RemoveEmptyEntries);

            Regex re = new Regex(@"(?<varName>.+?)(\[(?<varProp>.*)])*='(?<varVal>.*)'");
            Regex re2 = new Regex(@"(?<varName>.+?)(\[(?<varProp>.*)])*=(?<varVal>.*)");
            foreach (var part in parts)
            {
                var v = re.Match(part.Trim());
                var v2 = re2.Match(part.Trim());

                if (v.Success)
                {
                    MvdPropertyRuleValue rv = new MvdPropertyRuleValue(
                        v.Groups["varName"].Value,
                        v.Groups["varProp"].Value,
                        v.Groups["varVal"].Value
                        );
                    vals.Add(rv);
                    
                }
                else if (v2.Success)
                {
                    MvdPropertyRuleValue rv = new MvdPropertyRuleValue(
                        v.Groups["varName"].Value,
                        "Value",
                        v.Groups["varVal"].Value
                        );
                    vals.Add(rv);
                }

            }
            return vals;
        }
    }
}
