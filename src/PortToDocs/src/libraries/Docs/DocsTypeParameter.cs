﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ApiDocsSync.PortToDocs.Docs
{
    /// <summary>
    /// Each one of these TypeParameter objects islocated inside the TypeParameters section inside the Member.
    /// </summary>
    internal class DocsTypeParameter
    {
        private readonly XElement XETypeParameter;
        public string Name
        {
            get
            {
                return XmlHelper.GetAttributeValue(XETypeParameter, "Name");
            }
        }
        private XElement? Constraints
        {
            get
            {
                return XETypeParameter.Element("Constraints");
            }
        }
        private List<string>? _constraintsParameterAttributes;
        public List<string> ConstraintsParameterAttributes
        {
            get
            {
                if (_constraintsParameterAttributes == null)
                {
                    if (Constraints != null)
                    {
                        _constraintsParameterAttributes = Constraints.Elements("ParameterAttribute").Select(x => XmlHelper.GetNodesInPlainText(x)).ToList();
                    }
                    else
                    {
                        _constraintsParameterAttributes = new List<string>();
                    }
                }
                return _constraintsParameterAttributes;
            }
        }

        public string ConstraintsBaseTypeName
        {
            get
            {
                if (Constraints != null)
                {
                    return XmlHelper.GetChildElementValue(Constraints, "BaseTypeName");
                }
                return string.Empty;
            }
        }

        public DocsTypeParameter(XElement xeTypeParameter)
        {
            XETypeParameter = xeTypeParameter;
        }
    }
}
