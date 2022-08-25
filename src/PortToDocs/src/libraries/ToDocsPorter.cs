﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ApiDocsSync.Libraries.Docs;
using ApiDocsSync.Libraries.IntelliSenseXml;

namespace ApiDocsSync.Libraries
{
    public class ToDocsPorter
    {
        private readonly Configuration Config;
        private readonly DocsCommentsContainer DocsComments;
        private readonly IntelliSenseXmlCommentsContainer IntelliSenseXmlComments;

        private readonly List<string> ModifiedFiles = new List<string>();
        private readonly List<string> ModifiedTypes = new List<string>();
        private readonly List<string> ModifiedAPIs = new List<string>();
        private readonly List<string> ProblematicAPIs = new List<string>();
        private readonly List<string> AddedExceptions = new List<string>();

        private int TotalModifiedIndividualElements = 0;

        public ToDocsPorter(Configuration config)
        {
            Config = config;
            DocsComments = new DocsCommentsContainer(config);
            IntelliSenseXmlComments = new IntelliSenseXmlCommentsContainer(config);

        }

        public void CollectFiles()
        {
            Log.Info("Looking for IntelliSense xml files...");
            Config.VerifyIntellisenseXmlFiles();

            foreach (FileInfo fileInfo in IntelliSenseXmlComments.EnumerateFiles())
            {
                XDocument? xDoc = null;
                try
                {
                    xDoc = XDocument.Load(fileInfo.FullName);
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to load '{fileInfo.FullName}'. {ex}");
                }

                if (xDoc != null)
                {
                    IntelliSenseXmlComments.LoadIntellisenseXmlFile(xDoc, fileInfo.FullName);
                }
            }
            Log.Success("Finished looking for IntelliSense xml files.");
            Log.Line();

            Log.Info("Looking for Docs xml files...");
            Config.VerifyDocsFiles();

            foreach (FileInfo fileInfo in DocsComments.EnumerateFiles())
            {
                XDocument? xDoc = null;
                Encoding? encoding = null;
                try
                {
                    var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                    var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
                    using (StreamReader sr = new(fileInfo.FullName, utf8NoBom, detectEncodingFromByteOrderMarks: true))
                    {
                        xDoc = XDocument.Load(sr);
                        encoding = sr.CurrentEncoding;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to load '{fileInfo.FullName}'. {ex}");
                }

                if (xDoc != null && encoding != null)
                {
                    DocsComments.LoadDocsFile(xDoc, fileInfo.FullName, encoding);
                }
            }
            Log.Success("Finished looking for Docs xml files.");
            Log.Line();
        }

        public void LoadIntellisenseXmlFile(XDocument xDoc, string filePath) =>
            IntelliSenseXmlComments.LoadIntellisenseXmlFile(xDoc, filePath);

        public void LoadDocsFile(XDocument xDoc, string filePath, Encoding encoding) =>
            DocsComments.LoadDocsFile(xDoc, filePath, encoding);

        public void Start()
        {
            if (!IntelliSenseXmlComments.Members.Any())
            {
                Log.Error("No IntelliSense xml comments found.");
                return;
            }

            if (!DocsComments.Types.Any())
            {
                Log.Error("No Docs Type APIs found.");
                return;
            }

            PortMissingComments();
        }

        public void SaveToDisk() => DocsComments.SaveToDisk();

        // Prints a final summary of the execution findings.
        public void PrintSummary()
        {
            PrintUndocumentedAPIs();

            Log.Line();
            Log.Info($"Total modified files: {ModifiedFiles.Count}");
            if (Config.PrintSummaryDetails)
            {
                foreach (string file in ModifiedFiles)
                {
                    Log.Success($"    - {file}");
                }
                Log.Line();
            }

            Log.Info($"Total modified types: {ModifiedTypes.Count}");
            if (Config.PrintSummaryDetails)
            {
                foreach (string type in ModifiedTypes)
                {
                    Log.Success($"    - {type}");
                }
                Log.Line();
            }

            Log.Info($"Total modified APIs: {ModifiedAPIs.Count}");
            if (Config.PrintSummaryDetails)
            {
                foreach (string api in ModifiedAPIs)
                {
                    Log.Success($"    - {api}");
                }
            }

            Log.Line();
            Log.Info($"Total problematic APIs: {ProblematicAPIs.Count}");
            if (Config.PrintSummaryDetails)
            {
                foreach (string api in ProblematicAPIs)
                {
                    Log.Warning($"    - {api}");
                }
                Log.Line();
            }

            Log.Info($"Total added exceptions: {AddedExceptions.Count}");
            if (Config.PrintSummaryDetails)
            {
                foreach (string exception in AddedExceptions)
                {
                    Log.Success($"    - {exception}");
                }
                Log.Line();
            }

            Log.Info(false, "Total modified individual elements: ");
            Log.Success($"{TotalModifiedIndividualElements}");

            Log.Line();
            Log.Success("---------");
            Log.Success("FINISHED!");
            Log.Success("---------");
            Log.Line();

        }

        private void PortMissingComments()
        {
            Log.Info("Looking for IntelliSense xml comments that can be ported...");

            foreach (DocsType dTypeToUpdate in DocsComments.Types.Values)
            {
                PortMissingCommentsForType(dTypeToUpdate);
            }

            foreach (DocsMember dMemberToUpdate in DocsComments.Members.Values)
            {
                PortMissingCommentsForMember(dMemberToUpdate);
            }
        }

        // Tries to find an IntelliSense xml element from which to port documentation for the specified Docs type.
        private void PortMissingCommentsForType(DocsType dTypeToUpdate)
        {
            string docId = dTypeToUpdate.DocId;
            if (IntelliSenseXmlComments.Members.TryGetValue(docId, out IntelliSenseXmlMember? tsTypeToPort) && tsTypeToPort.Name == docId)
            {
                IntelliSenseXmlMember tsActualTypeToPort = tsTypeToPort;

                MissingComments mc = new()
                {
                    Summary = tsActualTypeToPort.Summary,
                    Returns = tsActualTypeToPort.Returns,
                    Remarks = tsActualTypeToPort.Remarks
                };

                // Rare case where the base type or interface docs should be used
                if (tsTypeToPort.InheritDoc)
                {
                    GetMissingCommentsForTypeFromInheritDoc(mc, dTypeToUpdate, tsTypeToPort);
                }

                TryPortMissingSummaryForAPI(dTypeToUpdate, mc.Summary);
                TryPortMissingRemarksForAPI(dTypeToUpdate, mc.Remarks);
                TryPortMissingParamsForAPI(dTypeToUpdate, tsActualTypeToPort, null); // Some types, like delegates, have params
                TryPortMissingTypeParamsForAPI(dTypeToUpdate, tsActualTypeToPort, null); // Type names ending with <T> have TypeParams
                if (dTypeToUpdate.BaseTypeName == "System.Delegate")
                {
                    TryPortMissingReturnsForMember(dTypeToUpdate, mc.Returns);
                }

                if (dTypeToUpdate.Changed)
                {
                    ModifiedTypes.Add(docId);
                    ModifiedFiles.Add(dTypeToUpdate.FilePath);
                }
            }
        }

        // Tries to find an IntelliSense xml element from which to port documentation for the specified Docs member.
        private void PortMissingCommentsForMember(DocsMember dMemberToUpdate)
        {
            DocsMember? dInterfacedMember = null;

            bool isProperty = dMemberToUpdate.IsProperty;
            bool isMethod = dMemberToUpdate.IsMethod;

            MissingComments mc = new();
            if (IntelliSenseXmlComments.Members.TryGetValue(dMemberToUpdate.DocId,
                out IntelliSenseXmlMember? tsMemberToPort) && tsMemberToPort != null)
            {
                mc.Summary = tsMemberToPort.Summary;
                if (isMethod)
                {
                    mc.Returns = tsMemberToPort.Returns;
                }
                mc.Remarks = tsMemberToPort.Remarks;
                if (isProperty)
                {
                    mc.Property = GetPropertyValue(tsMemberToPort.Value, tsMemberToPort.Returns);
                }

                // Rare case where the base type or interface docs should be used,
                // but only for elements that aren't explicitly documented in the actual API
                if (tsMemberToPort.InheritDoc)
                {
                    GetMissingCommentsForMemberFromInheritDoc(mc, dMemberToUpdate, tsMemberToPort);
                }
            }
            else if (TryGetEIIMember(dMemberToUpdate, out dInterfacedMember) && dInterfacedMember != null)
            {
                mc = GetMissingCommentsForMemberFromInterface(mc, dMemberToUpdate, dInterfacedMember);
            }

            if (tsMemberToPort != null || dInterfacedMember != null)
            {
                TryPortMissingSummaryForAPI(dMemberToUpdate, mc.Summary, mc.IsEII);
                TryPortMissingRemarksForAPI(dMemberToUpdate, mc.Remarks, mc.IsEII);
                TryPortMissingParamsForAPI(dMemberToUpdate, tsMemberToPort, dInterfacedMember);
                TryPortMissingTypeParamsForAPI(dMemberToUpdate, tsMemberToPort, dInterfacedMember);
                TryPortMissingExceptionsForMember(dMemberToUpdate, tsMemberToPort);

                if (isProperty)
                {
                    TryPortMissingPropertyForMember(dMemberToUpdate, mc.Property, mc.IsEII);
                }
                else if (isMethod)
                {
                    TryPortMissingReturnsForMember(dMemberToUpdate, mc.Returns, mc.IsEII);
                }

                if (dMemberToUpdate.Changed)
                {
                    ModifiedAPIs.Add(dMemberToUpdate.DocId);
                    ModifiedFiles.Add(dMemberToUpdate.FilePath);
                }
            }
        }

        private void GetMissingCommentsForTypeFromInheritDoc(MissingComments mc, DocsType dTypeToUpdate, IntelliSenseXmlMember tsTypeToPort)
        {
            if (Config.PreserveInheritDocTag)
            {
                // Creates the inheritdoc XElement if it does not exist
                dTypeToUpdate.InheritDocCref = tsTypeToPort.InheritDocCref;
                // No copying any inherited documentation, the inheritdoc tag takes care of it in MS Docs
                return;
            }

            // See if there is an inheritdoc cref indicating the exact member to use for docs
            if (!string.IsNullOrEmpty(tsTypeToPort.InheritDocCref) &&
                IntelliSenseXmlComments.Members.TryGetValue(tsTypeToPort.InheritDocCref,
                    out IntelliSenseXmlMember? tsInheritedMember) && tsInheritedMember != null)
            {
                mc.Summary = tsInheritedMember.Summary;
                mc.Returns = tsInheritedMember.Returns;
                mc.Remarks = tsInheritedMember.Remarks;
                
            }
            // Look for the base type from which this one inherits
            else if (DocsComments.Types.TryGetValue($"T:{dTypeToUpdate.BaseTypeName}", out DocsType? dBaseType) &&
                     dBaseType != null)
            {
                // If the base type is undocumented, try to document it
                // so there's something to extract for the child type
                if (dBaseType.IsUndocumented)
                {
                    PortMissingCommentsForType(dBaseType);
                }

                mc.Summary = dBaseType.Summary;
                mc.Returns = dBaseType.Returns;
                mc.Remarks = dBaseType.Remarks;
            }
        }

        private void GetMissingCommentsForMemberFromInheritDoc(MissingComments mc, DocsMember dMemberToUpdate, IntelliSenseXmlMember tsMemberToPort)
        {
            if (Config.PreserveInheritDocTag)
            {
                // Creates the inheritdoc XElement if it does not exist
                dMemberToUpdate.InheritDocCref = tsMemberToPort.InheritDocCref;
                // No copying any inherited documentation, the inheritdoc tag takes care of it in MS Docs
                return;
            }

            // See if there is an inheritdoc cref indicating the exact member to use for docs
            if (!string.IsNullOrEmpty(tsMemberToPort.InheritDocCref) &&
                IntelliSenseXmlComments.Members.TryGetValue(tsMemberToPort.InheritDocCref,
                    out IntelliSenseXmlMember? tsInheritedMember) && tsInheritedMember != null)
            {
                mc.Summary = tsInheritedMember.Summary;
                mc.Remarks = tsInheritedMember.Remarks;

                if (dMemberToUpdate.IsMethod)
                {
                    mc.Returns = tsInheritedMember.Returns;
                }
                else if (dMemberToUpdate.IsProperty)
                {
                    mc.Property = GetPropertyValue(tsInheritedMember.Value, tsInheritedMember.Returns);
                }
            }
            // Look for the base type and find the member from which this one inherits
            else if (DocsComments.Types.TryGetValue($"T:{dMemberToUpdate.ParentType.BaseTypeName}", out DocsType? dBaseType) &&
                     dBaseType != null)
            {
                // Get all the members of the parent type

                DocsMember? dBaseMember = null;
                foreach ((string _, DocsMember ptMember) in DocsComments.Members.Where(kvp => kvp.Value.ParentType.FullName == dBaseType.FullName))
                {
                    string currentDocId = ptMember.DocIdUnprefixed;
                    // Replace the prefix of the base type member API with the prefix of the member API to document
                    string replacedDocId = currentDocId.Replace(dBaseType.DocIdUnprefixed, dMemberToUpdate.DocIdUnprefixed);
                    if (replacedDocId == dMemberToUpdate.DocIdUnprefixed)
                    {
                        dBaseMember = ptMember;
                        break;
                    }
                }

                if (dBaseMember != null)
                {
                    // If the base member is undocumented, try to document it
                    // so there's something to extract for the child member
                    if (dBaseMember.IsUndocumented)
                    {
                        PortMissingCommentsForMember(dBaseMember);
                    }

                    mc.Summary = dBaseMember.Summary;
                    mc.Remarks = dBaseMember.Remarks;
                    if (dMemberToUpdate.IsMethod)
                    {
                        mc.Returns = dBaseMember.Returns;
                    }
                    else  if (dMemberToUpdate.IsProperty)
                    {
                        mc.Property = GetPropertyValue(dBaseMember.Value, dBaseMember.Returns);
                    }
                }
            }
        }

        private MissingComments GetMissingCommentsForMemberFromInterface(MissingComments mc, DocsMember dMemberToUpdate, DocsMember dInterfacedMember)
        {
            mc.Summary = dInterfacedMember.Summary;
            if (dMemberToUpdate.IsMethod)
            {
                mc.Returns = dInterfacedMember.Returns;
            }
            else if (dMemberToUpdate.IsProperty)
            {
                mc.Property = GetPropertyValue(dInterfacedMember.Value, dInterfacedMember.Returns);
            }

            if (!dInterfacedMember.Remarks.IsDocsEmpty())
            {
                // Only attempt to port if the member name is the same as the interfaced member docid without prefix
                if (dMemberToUpdate.MemberName == dInterfacedMember.DocId[2..])
                {
                    string dMemberToUpdateTypeDocIdNoPrefix = dMemberToUpdate.ParentType.DocId[2..];
                    string interfacedMemberTypeDocIdNoPrefix = dInterfacedMember.ParentType.DocId[2..];

                    // Special text for EIIs in Remarks
                    string eiiMessage = $"This member is an explicit interface member implementation. It can be used only when the <xref:{dMemberToUpdateTypeDocIdNoPrefix}> instance is cast to an <xref:{interfacedMemberTypeDocIdNoPrefix}> interface.";

                    string cleanedInterfaceRemarks = string.Empty;
                    if (!dInterfacedMember.Remarks.Contains(Configuration.ToBeAdded))
                    {
                        string interfaceMemberRemarks = dInterfacedMember.Remarks.RemoveSubstrings("##Remarks", "## Remarks", "<![CDATA[", "]]>").Trim();
                        string[] splitted = interfaceMemberRemarks.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        for (int i = 0; i < splitted.Length; i++)
                        {
                            string line = splitted[i];
                            cleanedInterfaceRemarks += line;
                            if (i < splitted.Length - 1)
                            {
                                cleanedInterfaceRemarks += Environment.NewLine;
                            }
                        }
                    }

                    // Only port the interface remarks if the user desired that
                    // Otherwise, always add the EII special message
                    mc.Remarks = eiiMessage + (!Config.SkipInterfaceRemarks ? Environment.NewLine + Environment.NewLine + cleanedInterfaceRemarks : string.Empty);

                    mc.IsEII = true;
                }
            }

            return mc;
        }

        // Issue: sometimes properties have their TS string in Value, sometimes in Returns
        private static string GetPropertyValue(string value, string returns)
        {
            string? property = null;
            if (!value.IsDocsEmpty())
            {
                property = value;
            }
            else if (!returns.IsDocsEmpty())
            {
                property = returns;
            }
            return property ?? Configuration.ToBeAdded;
        }

        // Attempts to obtain the member of the implemented interface.
        private bool TryGetEIIMember(IDocsAPI dApiToUpdate, out DocsMember? interfacedMember)
        {
            interfacedMember = null;

            if (!Config.SkipInterfaceImplementations && dApiToUpdate is DocsMember member)
            {
                string interfacedMemberDocId = member.ImplementsInterfaceMember;
                if (!string.IsNullOrEmpty(interfacedMemberDocId))
                {
                    DocsComments.Members.TryGetValue(interfacedMemberDocId, out interfacedMember);
                    return interfacedMember != null;
                }
            }

            return false;
        }

        // Ports the summary for the specified API if the field is undocumented.
        private void TryPortMissingSummaryForAPI(IDocsAPI dApiToUpdate, string? summary, bool isEII = false)
        {
            if (dApiToUpdate.Kind == APIKind.Type && !Config.PortTypeSummaries ||
                dApiToUpdate.Kind == APIKind.Member && !Config.PortMemberSummaries)
            {
                return;
            }

            // Only port if undocumented in MS Docs
            if (dApiToUpdate.Summary.IsDocsEmpty() && !summary.IsDocsEmpty())
            {
                dApiToUpdate.Summary = summary!;

                // Any member can have an empty summary
                PrintModifiedMember("summary", dApiToUpdate.FilePath, dApiToUpdate.DocId, isEII);
                TotalModifiedIndividualElements++;
            }
        }

        // Ports the remarks for the specified API if the field is undocumented.
        private void TryPortMissingRemarksForAPI(IDocsAPI dApiToUpdate, string? remarks, bool isEII = false)
        {
            if (dApiToUpdate.Kind == APIKind.Type && !Config.PortTypeRemarks ||
                dApiToUpdate.Kind == APIKind.Member && !Config.PortMemberRemarks)
            {
                return;
            }

            // Avoid porting remarks for enums, they are not allowed in dotnet-api-docs (cause build warnings)
            if (dApiToUpdate is DocsMember member &&
                member.ParentType.BaseTypeName == "System.Enum" &&
                member.MemberType == "Field")
            {
                return;
            }

            if (dApiToUpdate.Remarks.IsDocsEmpty() && !remarks.IsDocsEmpty())
            {
                dApiToUpdate.Remarks = Config.MarkdownRemarks ?
                    XmlHelper.GetFormattedAsMarkdown(remarks!, dApiToUpdate is DocsMember) :
                    XmlHelper.GetFormattedAsXml(remarks!, removeUndesiredEndlines: true);

                PrintModifiedMember("remarks", dApiToUpdate.FilePath, dApiToUpdate.DocId, isEII);
                TotalModifiedIndividualElements++;
            }
        }

        // Ports all the parameter descriptions for the specified API if any of them is undocumented.
        private void TryPortMissingParamsForAPI(IDocsAPI dApiToUpdate, IntelliSenseXmlMember? tsMemberToPort, DocsMember? interfacedMember)
        {
            if (dApiToUpdate.Kind == APIKind.Type && !Config.PortTypeParams /* Params of a Type */ ||
                dApiToUpdate.Kind == APIKind.Member && !Config.PortMemberParams)
            {
                return;
            }

            bool created;
            bool isEII = false;
            string name;
            string value;

            if (tsMemberToPort != null)
            {
                foreach (DocsParam dParam in dApiToUpdate.Params)
                {
                    if (dParam.Value.IsDocsEmpty())
                    {
                        created = false;
                        isEII = false;
                        name = string.Empty;
                        value = string.Empty;

                        IntelliSenseXmlParam? tsParam = tsMemberToPort.Params.FirstOrDefault(x => x.Name == dParam.Name);

                        // When not found, it's a bug in Docs (param name not the same as source/ref), so need to ask the user to indicate correct name
                        if (tsParam == null)
                        {
                            string msg;
                            if (tsMemberToPort.Params.Count() == 0)
                            {
                                msg = $"There were no IntelliSense xml comments for param {dParam.Name} in Member DocId {dApiToUpdate.DocId}";
                                ProblematicAPIs.Add(msg);
                                Log.Warning(msg);
                            }
                            else if (tsMemberToPort.Params.Count() != dApiToUpdate.Params.Count())
                            {
                                msg = $"The total number of params does not match between IntelliSense and Docs members {dApiToUpdate.DocId}";
                                ProblematicAPIs.Add(msg);
                                Log.Warning(msg);
                            }
                            else
                            {
                                created = TryPromptParam(dParam, tsMemberToPort, out IntelliSenseXmlParam? newTsParam);
                                if (newTsParam == null)
                                {
                                    msg = $"The param {dParam.Name} was not found in IntelliSense xml for {dApiToUpdate.DocId}";
                                    ProblematicAPIs.Add(msg);
                                    Log.Error(msg);
                                }
                                else
                                {
                                    // Now attempt to document it
                                    if (!newTsParam.Value.IsDocsEmpty())
                                    {
                                        // try to port IntelliSense xml comments
                                        dParam.Value = newTsParam.Value;
                                        name = newTsParam.Name;
                                        value = newTsParam.Value;
                                    }
                                    // or try to find if it implements a documented interface
                                    else if (interfacedMember != null)
                                    {
                                        DocsParam? interfacedParam = interfacedMember.Params.FirstOrDefault(x => x.Name == newTsParam.Name || x.Name == dParam.Name);
                                        if (interfacedParam != null)
                                        {
                                            dParam.Value = interfacedParam.Value;
                                            name = interfacedParam.Name;
                                            value = interfacedParam.Value;
                                            isEII = true;
                                        }
                                    }
                                }
                            }
                        }
                        // Attempt to port
                        else if (!tsParam.Value.IsDocsEmpty())
                        {
                            // try to port IntelliSense xml comments
                            dParam.Value = tsParam.Value;
                            name = tsParam.Name;
                            value = tsParam.Value;
                        }
                        // or try to find if it implements a documented interface
                        else if (interfacedMember != null)
                        {
                            DocsParam? interfacedParam = interfacedMember.Params.FirstOrDefault(x => x.Name == dParam.Name);
                            if (interfacedParam != null)
                            {
                                dParam.Value = interfacedParam.Value;
                                name = interfacedParam.Name;
                                value = interfacedParam.Value;
                                isEII = true;
                            }
                        }


                        if (!value.IsDocsEmpty())
                        {
                            PrintModifiedMember($"param '{name}'", dApiToUpdate.FilePath, dApiToUpdate.DocId, isEII);
                            TotalModifiedIndividualElements++;
                        }
                    }
                }
            }
            else if (interfacedMember != null)
            {
                foreach (DocsParam dParam in dApiToUpdate.Params)
                {
                    if (dParam.Value.IsDocsEmpty())
                    {
                        DocsParam? interfacedParam = interfacedMember.Params.FirstOrDefault(x => x.Name == dParam.Name);
                        if (interfacedParam != null && !interfacedParam.Value.IsDocsEmpty())
                        {
                            dParam.Value = interfacedParam.Value;
                            PrintModifiedMember($"param '{dParam.Name}'", dApiToUpdate.FilePath, dApiToUpdate.DocId, isEII);
                            TotalModifiedIndividualElements++;
                        }
                    }
                }
            }
        }

        // Ports all the type parameter descriptions for the specified API if any of them is undocumented.
        private void TryPortMissingTypeParamsForAPI(IDocsAPI dApiToUpdate, IntelliSenseXmlMember? tsMemberToPort, DocsMember? interfacedMember)
        {

            if (dApiToUpdate.Kind == APIKind.Type && !Config.PortTypeTypeParams /* TypeParams of a Type */ ||
                dApiToUpdate.Kind == APIKind.Member && !Config.PortMemberTypeParams)
            {
                return;
            }

            bool created;
            bool isEII = false;
            string name;
            string value;

            if (tsMemberToPort != null)
            {
                foreach (DocsTypeParam dTypeParam in dApiToUpdate.TypeParams)
                {
                    if (dTypeParam.Value.IsDocsEmpty())
                    {
                        created = false;
                        isEII = false;
                        name = string.Empty;
                        value = string.Empty;

                        IntelliSenseXmlTypeParam? tsTypeParam = tsMemberToPort.TypeParams.FirstOrDefault(x => x.Name == dTypeParam.Name);

                        // When not found, it's a bug in Docs (typeparam name not the same as source/ref), so need to ask the user to indicate correct name
                        if (tsTypeParam == null)
                        {
                            string msg;
                            if (tsMemberToPort.TypeParams.Count() == 0)
                            {
                                msg = $"There were no IntelliSense xml comments for typeparam {dTypeParam.Name} in Member DocId {dApiToUpdate.DocId}";
                                ProblematicAPIs.Add(msg);
                                Log.Warning(msg);
                            }
                            else if (tsMemberToPort.TypeParams.Count() != dApiToUpdate.TypeParams.Count())
                            {
                                msg = $"The total number of typeparams does not match between IntelliSense and Docs members {dApiToUpdate.DocId}";
                                ProblematicAPIs.Add(msg);
                                Log.Warning(msg);
                            }
                            else
                            {
                                created = TryPromptTypeParam(dTypeParam, tsMemberToPort, out IntelliSenseXmlTypeParam? newTsTypeParam);
                                if (newTsTypeParam == null)
                                {
                                    msg = $"The typeparam {dTypeParam.Name} was not found in IntelliSense xml for {dApiToUpdate.DocId}";
                                    ProblematicAPIs.Add(msg);
                                    Log.Error(msg);
                                }
                                else
                                {
                                    // Now attempt to document it
                                    if (!newTsTypeParam.Value.IsDocsEmpty())
                                    {
                                        // try to port IntelliSense xml comments
                                        dTypeParam.Value = newTsTypeParam.Value;
                                        name = newTsTypeParam.Name;
                                        value = newTsTypeParam.Value;
                                    }
                                    // or try to find if it implements a documented interface
                                    else if (interfacedMember != null)
                                    {
                                        DocsTypeParam? interfacedTypeParam = interfacedMember.TypeParams.FirstOrDefault(x => x.Name == newTsTypeParam.Name || x.Name == dTypeParam.Name);
                                        if (interfacedTypeParam != null)
                                        {
                                            dTypeParam.Value = interfacedTypeParam.Value;
                                            name = interfacedTypeParam.Name;
                                            value = interfacedTypeParam.Value;
                                            isEII = true;
                                        }
                                    }
                                }
                            }
                        }
                        // Attempt to port
                        else if (!tsTypeParam.Value.IsDocsEmpty())
                        {
                            // try to port IntelliSense xml comments
                            dTypeParam.Value = tsTypeParam.Value;
                            name = tsTypeParam.Name;
                            value = tsTypeParam.Value;
                        }
                        // or try to find if it implements a documented interface
                        else if (interfacedMember != null)
                        {
                            DocsTypeParam? interfacedTypeParam = interfacedMember.TypeParams.FirstOrDefault(x => x.Name == dTypeParam.Name);
                            if (interfacedTypeParam != null)
                            {
                                dTypeParam.Value = interfacedTypeParam.Value;
                                name = interfacedTypeParam.Name;
                                value = interfacedTypeParam.Value;
                                isEII = true;
                            }
                        }


                        if (!value.IsDocsEmpty())
                        {
                            PrintModifiedMember($"typeparam '{name}'", dApiToUpdate.FilePath, dApiToUpdate.DocId, isEII);
                            TotalModifiedIndividualElements++;
                        }
                    }
                }
            }
            else if (interfacedMember != null)
            {
                foreach (DocsTypeParam dTypeParam in dApiToUpdate.TypeParams)
                {
                    if (dTypeParam.Value.IsDocsEmpty())
                    {
                        DocsTypeParam? interfacedTypeParam = interfacedMember.TypeParams.FirstOrDefault(x => x.Name == dTypeParam.Name);
                        if (interfacedTypeParam != null && !interfacedTypeParam.Value.IsDocsEmpty())
                        {
                            dTypeParam.Value = interfacedTypeParam.Value;
                            PrintModifiedMember($"typeparam '{dTypeParam.Name}'", dApiToUpdate.FilePath, dApiToUpdate.DocId, isEII);
                            TotalModifiedIndividualElements++;
                        }
                    }
                }
            }
        }

        // Tries to document the passed property.
        private void TryPortMissingPropertyForMember(DocsMember dMemberToUpdate, string? property, bool isEII = false)
        {
            if (Config.PortMemberProperties && dMemberToUpdate.Value.IsDocsEmpty() && !property.IsDocsEmpty())
            {
                dMemberToUpdate.Value = property!;
                PrintModifiedMember("property", dMemberToUpdate.FilePath, dMemberToUpdate.DocId, isEII);
                TotalModifiedIndividualElements++;
            }
        }

        // Tries to document the returns element of the specified API: it can be a Method Member, or a Delegate Type.
        private void TryPortMissingReturnsForMember(IDocsAPI dMemberToUpdate, string? returns, bool isEII = false)
        {
            if (Config.PortMemberReturns)
            {
                // Bug: Sometimes a void return value shows up as not documented, skip those
                if (dMemberToUpdate.ReturnType == "System.Void")
                {
                    // It looks like this:
                    // <ReturnValue>
                    //  <ReturnType>System.Void</ReturnType>
                    // </ReturnValue>
                    //
                    // As a Docs bug, it's not worth logging
                    // ProblematicAPIs.AddIfNotExists($"Unexpected System.Void return value in Method=[{dMemberToUpdate.DocId}]");
                }
                else if (dMemberToUpdate.Returns.IsDocsEmpty() && !returns.IsDocsEmpty())
                {
                    dMemberToUpdate.Returns = returns!;
                    PrintModifiedMember("returns", dMemberToUpdate.FilePath, dMemberToUpdate.DocId, isEII);
                    TotalModifiedIndividualElements++;
                }
            }
        }

        // Ports all the exceptions for the specified API.
        // They are only processed if the user specified in the command arguments to NOT skip exceptions.
        // All exceptions get ported, because there is no easy way to determine if an exception is already documented or not.
        private void TryPortMissingExceptionsForMember(DocsMember dMemberToUpdate, IntelliSenseXmlMember? tsMemberToPort)
        {
            if (!Config.PortExceptionsExisting && !Config.PortExceptionsNew)
            {
                return;
            }

            if (tsMemberToPort != null)
            {
                // Exceptions are a special case: If a new one is found in code, but does not exist in docs, the whole element needs to be added
                foreach (IntelliSenseXmlException tsException in tsMemberToPort.Exceptions)
                {
                    DocsException? dException = dMemberToUpdate.Exceptions.FirstOrDefault(x => x.Cref == tsException.Cref);
                    bool created = false;

                    // First time adding the cref
                    if (dException == null && Config.PortExceptionsNew)
                    {
                        AddedExceptions.Add($"Exception=[{tsException.Cref}] in Member=[{dMemberToUpdate.DocId}]");
                        string text = XmlHelper.ReplaceExceptionPatterns(XmlHelper.GetNodesInPlainText(tsException.XEException));
                        dException = dMemberToUpdate.AddException(tsException.Cref, text);
                        created = true;
                    }
                    // If cref exists, check if the text has already been appended
                    else if (dException != null && Config.PortExceptionsExisting)
                    {
                        XElement formattedException = tsException.XEException;
                        string value = XmlHelper.ReplaceExceptionPatterns(XmlHelper.GetNodesInPlainText(formattedException));
                        if (!dException.WordCountCollidesAboveThreshold(value, Config.ExceptionCollisionThreshold))
                        {
                            AddedExceptions.Add($"Exception=[{tsException.Cref}] in Member=[{dMemberToUpdate.DocId}]");
                            dException.AppendException(value);
                            created = true;
                        }
                    }

                    if (dException != null)
                    {
                        if (created || (!tsException.Value.IsDocsEmpty() && dException.Value.IsDocsEmpty()))
                        {
                            PrintModifiedMember("exception", dException.ParentAPI.FilePath, dException.Cref, isEII: false);

                            TotalModifiedIndividualElements++;
                        }
                    }
                }
            }
        }

        // If a Param is found in a DocsType or a DocsMember that did not exist in the IntelliSense xml member, it's possible the param was unexpectedly saved in the IntelliSense xml comments with a different name, so the user gets prompted to look for it.
        private bool TryPromptParam(DocsParam oldDParam, IntelliSenseXmlMember tsMember, out IntelliSenseXmlParam? newTsParam)
        {
            newTsParam = null;

            if (Config.DisablePrompts)
            {
                Log.Error($"Prompts disabled. Will not process the '{oldDParam.Name}' param.");
                return false;
            }

            bool created = false;
            int option = -1;
            while (option == -1)
            {
                Log.Error($"Problem in param '{oldDParam.Name}' in member '{tsMember.Name}' in file '{oldDParam.ParentAPI.FilePath}'");
                Log.Error($"The param probably exists in code, but the exact name was not found in Docs. What would you like to do?");
                Log.Warning("    0 - Exit program.");
                Log.Info("    1 - Select the correct IntelliSense xml param from the existing ones.");
                Log.Info("    2 - Ignore this param and continue.");
                Log.Warning("      Note:Make sure to double check the affected Docs file after the tool finishes executing.");
                Log.Cyan(false, "Your answer [0,1,2]: ");

                if (!int.TryParse(Console.ReadLine(), out option))
                {
                    Log.Error("Not a number. Try again.");
                    option = -1;
                }
                else
                {
                    switch (option)
                    {
                        case 0:
                            {
                                Log.Info("Goodbye!");
                                Environment.Exit(0);
                                break;
                            }

                        case 1:
                            {
                                int paramSelection = -1;
                                while (paramSelection == -1)
                                {
                                    Log.Info($"IntelliSense xml params found in member '{tsMember.Name}':");
                                    Log.Warning("    0 - Exit program.");
                                    Log.Info("    1 - Ignore this param and continue.");
                                    int paramCounter = 2;
                                    foreach (IntelliSenseXmlParam param in tsMember.Params)
                                    {
                                        Log.Info($"    {paramCounter} - {param.Name}");
                                        paramCounter++;
                                    }

                                    Log.Cyan(false, $"Your answer to match param '{oldDParam.Name}'? [0..{paramCounter - 1}]: ");

                                    if (!int.TryParse(Console.ReadLine(), out paramSelection))
                                    {
                                        Log.Error("Not a number. Try again.");
                                        paramSelection = -1;
                                    }
                                    else if (paramSelection < 0 || paramSelection >= paramCounter)
                                    {
                                        Log.Error("Invalid selection. Try again.");
                                        paramSelection = -1;
                                    }
                                    else if (paramSelection == 0)
                                    {
                                        Log.Info("Goodbye!");
                                        Environment.Exit(0);
                                    }
                                    else if (paramSelection == 1)
                                    {
                                        Log.Info("Skipping this param.");
                                        break;
                                    }
                                    else
                                    {
                                        newTsParam = tsMember.Params[paramSelection - 2];
                                        Log.Success($"Selected: {newTsParam.Name}");
                                    }
                                }

                                break;
                            }

                        case 2:
                            {
                                Log.Info("Skipping this param.");
                                break;
                            }

                        default:
                            {
                                Log.Error("Invalid selection. Try again.");
                                option = -1;
                                break;
                            }
                    }
                }
            }

            return created;
        }

        // If a Param is found in a DocsType or a DocsMember that did not exist in the IntelliSense xml member, it's possible the param was unexpectedly saved in the IntelliSense xml comments with a different name, so the user gets prompted to look for it.
        private bool TryPromptTypeParam(DocsTypeParam oldDTypeParam, IntelliSenseXmlMember tsMember, out IntelliSenseXmlTypeParam? newTsTypeParam)
        {
            newTsTypeParam = null;

            if (Config.DisablePrompts)
            {
                Log.Error($"Prompts disabled. Will not process the '{oldDTypeParam.Name}' typeparam.");
                return false;
            }

            bool created = false;
            int option = -1;
            while (option == -1)
            {
                Log.Error($"Problem in typeparam '{oldDTypeParam.Name}' in member '{tsMember.Name}' in file '{oldDTypeParam.ParentAPI.FilePath}'");
                Log.Error($"The typeparam probably exists in code, but the exact name was not found in Docs. What would you like to do?");
                Log.Warning("    0 - Exit program.");
                Log.Info("    1 - Select the correct IntelliSense xml typeparam from the existing ones.");
                Log.Info("    2 - Ignore this typeparam and continue.");
                Log.Warning("      Note:Make sure to double check the affected Docs file after the tool finishes executing.");
                Log.Cyan(false, "Your answer [0,1,2]: ");

                if (!int.TryParse(Console.ReadLine(), out option))
                {
                    Log.Error("Not a number. Try again.");
                    option = -1;
                }
                else
                {
                    switch (option)
                    {
                        case 0:
                            {
                                Log.Info("Goodbye!");
                                Environment.Exit(0);
                                break;
                            }

                        case 1:
                            {
                                int typeParamSelection = -1;
                                while (typeParamSelection == -1)
                                {
                                    Log.Info($"IntelliSense xml typeparams found in member '{tsMember.Name}':");
                                    Log.Warning("    0 - Exit program.");
                                    Log.Info("    1 - Ignore this typeparam and continue.");
                                    int typeParamCounter = 2;
                                    foreach (IntelliSenseXmlTypeParam typeParam in tsMember.TypeParams)
                                    {
                                        Log.Info($"    {typeParamCounter} - {typeParam.Name}");
                                        typeParamCounter++;
                                    }

                                    Log.Cyan(false, $"Your answer to match typeparam '{oldDTypeParam.Name}'? [0..{typeParamCounter - 1}]: ");

                                    if (!int.TryParse(Console.ReadLine(), out typeParamSelection))
                                    {
                                        Log.Error("Not a number. Try again.");
                                        typeParamSelection = -1;
                                    }
                                    else if (typeParamSelection < 0 || typeParamSelection >= typeParamCounter)
                                    {
                                        Log.Error("Invalid selection. Try again.");
                                        typeParamSelection = -1;
                                    }
                                    else if (typeParamSelection == 0)
                                    {
                                        Log.Info("Goodbye!");
                                        Environment.Exit(0);
                                    }
                                    else if (typeParamSelection == 1)
                                    {
                                        Log.Info("Skipping this typeparam.");
                                        break;
                                    }
                                    else
                                    {
                                        newTsTypeParam = tsMember.TypeParams[typeParamSelection - 2];
                                        Log.Success($"Selected: {newTsTypeParam.Name}");
                                    }
                                }

                                break;
                            }

                        case 2:
                            {
                                Log.Info("Skipping this typeparam.");
                                break;
                            }

                        default:
                            {
                                Log.Error("Invalid selection. Try again.");
                                option = -1;
                                break;
                            }
                    }
                }
            }

            return created;
        }

        /// <summary>
        /// Standard formatted print message for a modified element.
        /// </summary>
        /// <param name="message">The friendly description of the modified API.</param>
        /// <param name="docsFilePath">The file where the modified API lives.</param>
        /// <param name="docId">The API unique identifier.</param>
        private void PrintModifiedMember(string elementName, string docsFilePath, string docId, bool isEII)
        {
            if (Config.PrintSummaryDetails)
            {
                Log.Warning($"    File: {docsFilePath}");
                Log.Warning($"        DocID: {docId}");
                Log.Warning($"        Modified element: {elementName}");
                if (isEII)
                {
                    Log.Magenta($"        Ported as explicit interface implementation");
                }
                Log.Info("---------------------------------------------------");
                Log.Line();
            }
        }

        // Prints all the undocumented APIs.
        // This is only done if the user specified in the command arguments to print undocumented APIs.
        private void PrintUndocumentedAPIs()
        {
            if (Config.PrintUndoc)
            {
                Log.Line();
                Log.Success("-----------------");
                Log.Success("UNDOCUMENTED APIS");
                Log.Success("-----------------");

                Log.Line();

                void TryPrintType(ref bool undocAPI, string typeDocId)
                {
                    if (!undocAPI)
                    {
                        Log.Info("    Type: {0}", typeDocId);
                        undocAPI = true;
                    }
                };

                void TryPrintMember(ref bool undocMember, string memberDocId)
                {
                    if (!undocMember)
                    {
                        Log.Info("            {0}", memberDocId);
                        undocMember = true;
                    }
                };

                int typeSummaries = 0;
                int memberSummaries = 0;
                int memberValues = 0;
                int memberReturns = 0;
                int memberParams = 0;
                int memberTypeParams = 0;
                int exceptions = 0;

                Log.Info("Undocumented APIs:");

                foreach (DocsType docsType in DocsComments.Types.Values)
                {
                    bool undocAPI = false;
                    if (docsType.Summary.IsDocsEmpty())
                    {
                        TryPrintType(ref undocAPI, docsType.DocId);
                        Log.Error($"        Type Summary: {docsType.Summary}");
                        typeSummaries++;
                    }
                }

                foreach (DocsMember member in DocsComments.Members.Values)
                {
                    bool undocMember = false;

                    if (member.Summary.IsDocsEmpty())
                    {
                        TryPrintMember(ref undocMember, member.DocId);

                        Log.Error($"        Member Summary: {member.Summary}");
                        memberSummaries++;
                    }

                    if (member.IsProperty)
                    {
                        if (member.Value == Configuration.ToBeAdded)
                        {
                            TryPrintMember(ref undocMember, member.DocId);

                            Log.Error($"        Property Value: {member.Value}");
                            memberValues++;
                        }
                    }
                    else if (member.IsMethod)
                    {
                        if (member.Returns == Configuration.ToBeAdded)
                        {
                            TryPrintMember(ref undocMember, member.DocId);

                            Log.Error($"        Method Returns: {member.Returns}");
                            memberReturns++;
                        }
                    }

                    foreach (DocsParam param in member.Params)
                    {
                        if (param.Value.IsDocsEmpty())
                        {
                            TryPrintMember(ref undocMember, member.DocId);

                            Log.Error($"        Member Param: {param.Name}: {param.Value}");
                            memberParams++;
                        }
                    }

                    foreach (DocsTypeParam typeParam in member.TypeParams)
                    {
                        if (typeParam.Value.IsDocsEmpty())
                        {
                            TryPrintMember(ref undocMember, member.DocId);

                            Log.Error($"        Member Type Param: {typeParam.Name}: {typeParam.Value}");
                            memberTypeParams++;
                        }
                    }

                    foreach (DocsException exception in member.Exceptions)
                    {
                        if (exception.Value.IsDocsEmpty())
                        {
                            TryPrintMember(ref undocMember, member.DocId);

                            Log.Error($"        Member Exception: {exception.Cref}: {exception.Value}");
                            exceptions++;
                        }
                    }
                }

                Log.Info($" Undocumented type summaries: {typeSummaries}");
                Log.Info($" Undocumented member summaries: {memberSummaries}");
                Log.Info($" Undocumented method returns: {memberReturns}");
                Log.Info($" Undocumented property values: {memberValues}");
                Log.Info($" Undocumented member params: {memberParams}");
                Log.Info($" Undocumented member type params: {memberTypeParams}");
                Log.Info($" Undocumented exceptions: {exceptions}");

                Log.Line();
            }
        }

        private class MissingComments
        {
            internal string Summary { get; set; }
            internal string Returns { get; set; }
            internal string Remarks { get; set; }
            internal string Property { get; set; }
            internal bool IsEII { get; set; }

            internal MissingComments()
            {
                Summary = Configuration.ToBeAdded;
                Returns = Configuration.ToBeAdded;
                Remarks = Configuration.ToBeAdded;
                Property = Configuration.ToBeAdded;
            }
        }
    }
}
