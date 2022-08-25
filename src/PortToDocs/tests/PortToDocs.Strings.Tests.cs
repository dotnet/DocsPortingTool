﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ApiDocsSync.Libraries.Tests
{
    public class PortToDocs_Strings_Tests : BasePortTests
    {
        public PortToDocs_Strings_Tests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TypeParam_MismatchedNames_DoNotPort()
        {
            // The only way a typeparam is getting ported is if the name is exactly the same in the TypeParameter section as in the intellisense xml.
            // Or, if the tool is invoked with DisabledPrompts=true.

            // TypeParam name = TRenamedValue
            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyType"">
      <summary>This is the summary.</summary>
    </member>
    <member name=""M:MyNamespace.MyType.MyTypeParamMethod``1"">
      <typeparam name=""TRenamedValue"">The renamed typeparam of MyTypeParamMethod.</typeparam>
      <summary>The summary of MyTypeParamMethod.</summary>
    </member>
  </members>
</doc>";

            // TypeParam name = TValue
            string originalDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyTypeParamMethod&lt;TValue&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyTypeParamMethod``1"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <TypeParameters>
        <TypeParameter Name=""TValue"" />
      </TypeParameters>
      <Docs>
        <typeparam name=""TValue"">To be added.</typeparam>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            // TypeParam summary not ported
            string expectedDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>This is the summary.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyTypeParamMethod&lt;TValue&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyTypeParamMethod``1"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <TypeParameters>
        <TypeParameter Name=""TValue"" />
      </TypeParameters>
      <Docs>
        <typeparam name=""TValue"">To be added.</typeparam>
        <summary>The summary of MyTypeParamMethod.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            Configuration configuration = new();
            configuration.IncludedAssemblies.Add(FileTestData.TestAssembly);

            TestWithStrings(originalIntellisense, originalDocs, expectedDocs, configuration);
        }

        [Fact]
        public void See_Cref()
        {
            // References to other APIs, using <see cref="DocId"/> in intellisense xml, need to be transformed to <see cref="X:DocId"/> in docs xml (notice the prefix), or <xref:DocId> in markdown.

            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyType"">
      <summary>See <see cref=""T:MyNamespace.MyType""/>.</summary>
    </member>
    <member name=""M:MyNamespace.MyType.MyMethod"">
      <summary>The summary of MyMethod. See <see cref=""M:MyNamespace.MyType.MyMethod"" />.</summary>
      <remarks>See <see cref=""M:MyNamespace.MyType.MyMethod"" />.</remarks>
    </member>
  </members>
</doc>";

            string originalDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>See <see cref=""T:MyNamespace.MyType"" />.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <summary>The summary of MyMethod. See <see cref=""M:MyNamespace.MyType.MyMethod"" />.</summary>
        <remarks>
          <format type=""text/markdown""><![CDATA[

## Remarks

See <xref:MyNamespace.MyType.MyMethod>.

          ]]></format>
        </remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            Configuration configuration = new()
            {
                MarkdownRemarks = true
            };
            configuration.IncludedAssemblies.Add(FileTestData.TestAssembly);

            TestWithStrings(originalIntellisense, originalDocs, expectedDocs, configuration);
        }

        [Fact]
        public void See_Cref_Primitive()
        {
            // Need to make sure that see crefs pointing to primitives are also converted properly.

            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyType"">
      <summary>Type summary.</summary>
    </member>
    <member name=""M:MyNamespace.MyType.MyMethod"">
      <summary>Summary: <see cref=""bool""/>, <see cref=""byte""/>, <see cref=""sbyte""/>, <see cref=""char""/>, <see cref=""decimal""/>, <see cref=""double""/>, <see cref=""float""/>, <see cref=""int""/>, <see cref=""uint""/>, <see cref=""nint""/>, <see cref=""nuint""/>, <see cref=""long""/>, <see cref=""ulong""/>, <see cref=""short""/>, <see cref=""ushort""/>, <see cref=""object""/>, <see cref=""dynamic""/>, <see cref=""string""/>.</summary>
      <remarks>Remarks: <see cref=""bool""/>, <see cref=""byte""/>, <see cref=""sbyte""/>, <see cref=""char""/>, <see cref=""decimal""/>, <see cref=""double""/>, <see cref=""float""/>, <see cref=""int""/>, <see cref=""uint""/>, <see cref=""nint""/>, <see cref=""nuint""/>, <see cref=""long""/>, <see cref=""ulong""/>, <see cref=""short""/>, <see cref=""ushort""/>, <see cref=""object""/>, <see cref=""dynamic""/>, <see cref=""string""/>.</remarks>
    </member>
  </members>
</doc>";

            string originalDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            // Notice that `dynamic` is converted to langword, to prevent converting it to System.Object.
            string expectedDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>Type summary.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <summary>Summary: <see cref=""T:System.Boolean"" />, <see cref=""T:System.Byte"" />, <see cref=""T:System.SByte"" />, <see cref=""T:System.Char"" />, <see cref=""T:System.Decimal"" />, <see cref=""T:System.Double"" />, <see cref=""T:System.Single"" />, <see cref=""T:System.Int32"" />, <see cref=""T:System.UInt32"" />, <see cref=""T:System.IntPtr"" />, <see cref=""T:System.UIntPtr"" />, <see cref=""T:System.Int64"" />, <see cref=""T:System.UInt64"" />, <see cref=""T:System.Int16"" />, <see cref=""T:System.UInt16"" />, <see cref=""T:System.Object"" />, <see langword=""dynamic"" />, <see cref=""T:System.String"" />.</summary>
        <remarks>
          <format type=""text/markdown""><![CDATA[

## Remarks

Remarks: `bool`, `byte`, `sbyte`, `char`, `decimal`, `double`, `float`, `int`, `uint`, `nint`, `nuint`, `long`, `ulong`, `short`, `ushort`, `object`, `dynamic`, `string`.

          ]]></format>
        </remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            Configuration configuration = new()
            {
                MarkdownRemarks = true
            };
            configuration.IncludedAssemblies.Add(FileTestData.TestAssembly);

            TestWithStrings(originalIntellisense, originalDocs, expectedDocs, configuration);
        }

        [Fact]
        public void See_Cref_Ctor_Remark()
        {
            // References to constructors, which look like
            // <see cref="M:Foo.Bar.#ctor"/> or <see cref="M:Foo.Bar.#ctor(System.Type)"/> in intellisense xml,
            // need to be transformed to <xref:Foo.Bar.%23ctor> or <xref:Foo.Bar.%23ctor(System.Type)> in markdown.

            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyType"">
      <summary>They summary.</summary>
    </member>
    <member name=""M:MyNamespace.MyType.#ctor"">
      <summary>Summary of parameterless constructor.</summary>
      <remarks>A link to itself: <see cref=""M:MyNamespace.MyType.#ctor""/>.</remarks>
    </member>
    <member name=""M:MyNamespace.MyType.#ctor(System.Object)"">
      <param name=""myParam"">Parameter summary.</param>
      <summary>Summary of constructor with parameter.</summary>
      <remarks>A link to itself: <see cref=""M:MyNamespace.MyType.#ctor(System.Object)""/>.</remarks>
    </member>
  </members>
</doc>";

            string originalDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName="".ctor"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.#ctor"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <Docs>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
    <Member MemberName="".ctor"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.#ctor(System.Object)"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <Docs>
        <param name=""myParam"">To be added.</param>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>They summary.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName="".ctor"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.#ctor"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <Docs>
        <summary>Summary of parameterless constructor.</summary>
        <remarks>
          <format type=""text/markdown""><![CDATA[

## Remarks

A link to itself: <xref:MyNamespace.MyType.%23ctor>.

          ]]></format>
        </remarks>
      </Docs>
    </Member>
    <Member MemberName="".ctor"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.#ctor(System.Object)"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <Docs>
        <param name=""myParam"">Parameter summary.</param>
        <summary>Summary of constructor with parameter.</summary>
        <remarks>
          <format type=""text/markdown""><![CDATA[

## Remarks

A link to itself: <xref:MyNamespace.MyType.%23ctor(System.Object)>.

          ]]></format>
        </remarks>
      </Docs>
    </Member>
  </Members>
</Type>";


            Configuration configuration = new()
            {
                MarkdownRemarks = true
            };
            configuration.IncludedAssemblies.Add(FileTestData.TestAssembly);

            TestWithStrings(originalIntellisense, originalDocs, expectedDocs, configuration);
        }

        [Fact]
        public void See_Cref_Generic()
        {
            // References to other APIs in remarks, should be converted to xref in markdown. Make sure generic APIs get converted properly. 

            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyGenericType`1"">
      <typeparam name=""T"">I am the type of MyGenericType.</typeparam>
      <summary>I have a funny suffix in my full name.</summary>
    </member>
    <member name=""M:MyNamespace.MyGenericType`1.MyMethod``2(System.Object)"">
      <typeparam name=""T"">The type T of the method.</typeparam>
      <typeparam name=""U"">The type U of the method.</typeparam>
      <summary>I have a reference to the generic type <see cref=""T:MyNamespace.MyGenericType`1""/> and to myself <see cref=""M:MyNamespace.MyGenericType`1.MyMethod``2(System.Object)""/>.</summary>
      <remarks>I have a reference to the generic type <see cref=""T:MyNamespace.MyGenericType`1""/> and to myself <see cref=""M:MyNamespace.MyGenericType`1.MyMethod``2(System.Object)""/>.</remarks>
    </member>
  </members>
</doc>";

            string originalDocs = @"<Type Name=""MyGenericType&lt;T&gt;"" FullName=""MyNamespace.MyGenericType&lt;T&gt;"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyGenericType`1"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <typeparam name=""T"">To be added.</typeparam>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod&lt;T,U&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyGenericType`1.MyMethod``2(System.Object)"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <typeparam name=""T"">To be added.</typeparam>
        <typeparam name=""U"">To be added.</typeparam>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedDocs = @"<Type Name=""MyGenericType&lt;T&gt;"" FullName=""MyNamespace.MyGenericType&lt;T&gt;"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyGenericType`1"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <typeparam name=""T"">I am the type of MyGenericType.</typeparam>
    <summary>I have a funny suffix in my full name.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod&lt;T,U&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyGenericType`1.MyMethod``2(System.Object)"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <typeparam name=""T"">The type T of the method.</typeparam>
        <typeparam name=""U"">The type U of the method.</typeparam>
        <summary>I have a reference to the generic type <see cref=""T:MyNamespace.MyGenericType`1"" /> and to myself <see cref=""M:MyNamespace.MyGenericType`1.MyMethod``2(System.Object)"" />.</summary>
        <remarks>
          <format type=""text/markdown""><![CDATA[

## Remarks

I have a reference to the generic type <xref:MyNamespace.MyGenericType%601> and to myself <xref:MyNamespace.MyGenericType%601.MyMethod%60%602(System.Object)>.

          ]]></format>
        </remarks>
      </Docs>
    </Member>
  </Members>
</Type>";


            Configuration configuration = new()
            {
                MarkdownRemarks = true
            };
            configuration.IncludedAssemblies.Add(FileTestData.TestAssembly);

            TestWithStrings(originalIntellisense, originalDocs, expectedDocs, configuration);
        }

        [Fact]
        public void SeeAlso_Cref()
        {
            // Normally, references to other APIs are indicated with <see cref="X:DocId"/> in xml, or with <xref:DocId> in markdown. But there are some rare cases where <seealso cref="X:DocId"/> is used, and we need to make sure to handle them just as see crefs.

            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyType"">
      <summary>See <seealso cref=""T:MyNamespace.MyType""/>.</summary>
    </member>
    <member name=""M:MyNamespace.MyType.MyMethod"">
      <summary>The summary of MyMethod. See <seealso cref=""M:MyNamespace.MyType.MyMethod"" />.</summary>
      <remarks>See <seealso cref=""M:MyNamespace.MyType.MyMethod"" />.</remarks>
    </member>
  </members>
</doc>";

            string originalDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>See <seealso cref=""T:MyNamespace.MyType"" />.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <summary>The summary of MyMethod. See <seealso cref=""M:MyNamespace.MyType.MyMethod"" />.</summary>
        <remarks>
          <format type=""text/markdown""><![CDATA[

## Remarks

See <xref:MyNamespace.MyType.MyMethod>.

          ]]></format>
        </remarks>
      </Docs>
    </Member>
  </Members>
</Type>";


            Configuration configuration = new()
            {
                MarkdownRemarks = true
            };
            configuration.IncludedAssemblies.Add(FileTestData.TestAssembly);

            TestWithStrings(originalIntellisense, originalDocs, expectedDocs, configuration);
        }

        [Fact]
        public void See_Langword()
        {
            // Reserved words are indicated with <see langword="word" />. They need to be copied as <see langword="word" /> in xml, or transformed to `word` in markdown.

            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyType"">
      <summary>Langword <see langword=""null""/>.</summary>
    </member>
    <member name=""M:MyNamespace.MyType.MyMethod"">
      <summary>The summary of MyMethod. Langword <see langword=""false""/>.</summary>
      <remarks>Langword <see langword=""true""/>.</remarks>
    </member>
  </members>
</doc>";

            string originalDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>Langword <see langword=""null"" />.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <summary>The summary of MyMethod. Langword <see langword=""false"" />.</summary>
        <remarks>
          <format type=""text/markdown""><![CDATA[

## Remarks

Langword `true`.

          ]]></format>
        </remarks>
      </Docs>
    </Member>
  </Members>
</Type>";


            Configuration configuration = new()
            {
                MarkdownRemarks = true
            };
            configuration.IncludedAssemblies.Add(FileTestData.TestAssembly);

            TestWithStrings(originalIntellisense, originalDocs, expectedDocs, configuration);
        }

        [Fact]
        public void ParamRefName()
        {
            // Parameter references are indicated with <paramref name="paramName" />. They need to be copied as <paramref name="paramName" /> in xml, or transformed to `paramName` in markdown.

            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyType"">
      <summary>Type summary.</summary>
    </member>
    <member name=""M:MyNamespace.MyType.MyMethod(System.String)"">
      <summary>The summary of MyMethod. Paramref <paramref name=""myParam""/>.</summary>
      <param name=""myParam"">My parameter description.</param>
      <remarks>Paramref <paramref name=""myParam""/>.</remarks>
    </member>
  </members>
</doc>";

            string originalDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod(System.String)"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Parameters>
        <Parameter Name=""myParam"" Type=""System.String"" />
      </Parameters>
      <Docs>
        <param name=""myParam"">To be added.</param>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>Type summary.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod(System.String)"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Parameters>
        <Parameter Name=""myParam"" Type=""System.String"" />
      </Parameters>
      <Docs>
        <param name=""myParam"">My parameter description.</param>
        <summary>The summary of MyMethod. Paramref <paramref name=""myParam"" />.</summary>
        <remarks>
          <format type=""text/markdown""><![CDATA[

## Remarks

Paramref `myParam`.

          ]]></format>
        </remarks>
      </Docs>
    </Member>
  </Members>
</Type>";


            Configuration configuration = new()
            {
                MarkdownRemarks = true
            };
            configuration.IncludedAssemblies.Add(FileTestData.TestAssembly);

            TestWithStrings(originalIntellisense, originalDocs, expectedDocs, configuration);
        }

        [Fact]
        public void TypeParamRefName()
        {
            // TypeParameter references are indicated with <typeparamref name="typeParamName" />. They need to be copied as <typeparamref name="typeParamName" /> in xml, or transformed to `typeParamName` in markdown.

            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyType`1"">
      <typeparam name=""T"">Description of the typeparam of the type.</typeparam>
      <summary>Type summary. Typeparamref <typeparamref name=""T""/>.</summary>
    </member>
    <member name=""M:MyNamespace.MyType.MyMethod``1"">
      <summary>The summary of MyMethod. Typeparamref <typeparamref name=""T""/>.</summary>
      <typeparam name=""T"">Description of the typeparam of the method.</typeparam>
      <remarks>Typeparamref <typeparamref name=""T""/>.</remarks>
    </member>
  </members>
</doc>";

            string originalDocs = @"<Type Name=""MyType&lt;T&gt;"" FullName=""MyNamespace.MyType&lt;T&gt;"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType`1"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <TypeParameters>
    <TypeParameter Name=""T"">
      <Constraints>
        <BaseTypeName>System.ValueType</BaseTypeName>
      </Constraints>
    </TypeParameter>
  </TypeParameters>
  <Docs>
    <typeparam name=""T"">To be added.</typeparam>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod&lt;T&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod``1"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <TypeParameters>
        <TypeParameter Name=""T"">
          <Constraints>
            <BaseTypeName>System.ValueType</BaseTypeName>
          </Constraints>
        </TypeParameter>
      </TypeParameters>
      <Docs>
        <typeparam name=""T"">To be added.</typeparam>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedDocs = @"<Type Name=""MyType&lt;T&gt;"" FullName=""MyNamespace.MyType&lt;T&gt;"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType`1"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <TypeParameters>
    <TypeParameter Name=""T"">
      <Constraints>
        <BaseTypeName>System.ValueType</BaseTypeName>
      </Constraints>
    </TypeParameter>
  </TypeParameters>
  <Docs>
    <typeparam name=""T"">Description of the typeparam of the type.</typeparam>
    <summary>Type summary. Typeparamref <typeparamref name=""T"" />.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod&lt;T&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod``1"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <TypeParameters>
        <TypeParameter Name=""T"">
          <Constraints>
            <BaseTypeName>System.ValueType</BaseTypeName>
          </Constraints>
        </TypeParameter>
      </TypeParameters>
      <Docs>
        <typeparam name=""T"">Description of the typeparam of the method.</typeparam>
        <summary>The summary of MyMethod. Typeparamref <typeparamref name=""T"" />.</summary>
        <remarks>
          <format type=""text/markdown""><![CDATA[

## Remarks

Typeparamref `T`.

          ]]></format>
        </remarks>
      </Docs>
    </Member>
  </Members>
</Type>";


            Configuration configuration = new()
            {
                MarkdownRemarks = true
            };
            configuration.IncludedAssemblies.Add(FileTestData.TestAssembly);

            TestWithStrings(originalIntellisense, originalDocs, expectedDocs, configuration);
        }

        [Fact]
        public void XmlRemarks()
        {
            // The default formatting for remarks is xml

            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyType"">
      <remarks>Remarks for type <see cref=""T:MyNamespace.MyType""/>.</remarks>
    </member>
    <member name=""M:MyNamespace.MyType.MyMethod"">
      <remarks>Remarks for <see cref=""M:MyNamespace.MyType.MyMethod""/>.</remarks>
    </member>
  </members>
</doc>";

            string originalDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>Remarks for type <see cref=""T:MyNamespace.MyType"" />.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <summary>To be added.</summary>
        <remarks>Remarks for <see cref=""M:MyNamespace.MyType.MyMethod"" />.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            Configuration configuration = new()
            {
                MarkdownRemarks = false
            };
            configuration.IncludedAssemblies.Add(FileTestData.TestAssembly);

            TestWithStrings(originalIntellisense, originalDocs, expectedDocs, configuration);
        }

        [Fact]
        public void FullInheritDoc()
        {
            string intellisenseParentType = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>System</name>
  </assembly>
  <members>
    <member name=""T:System.MyParentType"">
      <summary>This is the summary of the MyParentType class.</summary>
      <remarks>These are the remarks of the MyParentType class.</remarks>
    </member>
    <member name=""M:System.MyParentType.MyMethod"">
      <summary>This is the summary of the MyParentType.MyMethod method.</summary>
      <remarks>These are the remarks of the MyParentType.MyMethod method.</remarks>
    </member>
  </members>
</doc>";

            string intellisenseChildType = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyType"">
      <inheritdoc/>
    </member>
    <member name=""M:MyNamespace.MyType.MyMethod"">
      <inheritdoc cref=""M:System.MyParentType.MyMethod""/>
    </member>
  </members>
</doc>";

            string originalBaseType = @"<Type Name=""MyParentType"" FullName=""System.MyParentType"">
  <TypeSignature Language=""DocId"" Value=""T:System.MyParentType"" />
  <AssemblyInfo>
    <AssemblyName>System</AssemblyName>
  </AssemblyInfo>
  <Base>
    <BaseTypeName>System.MyParentType</BaseTypeName>
  </Base>
  <Interfaces />
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:System.MyParentType.MyMethod"" />
      <MemberType>Method</MemberType>
      <Implements />
      <AssemblyInfo>
        <AssemblyName>System</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Parameters />
      <Docs>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedBaseType = @"<Type Name=""MyParentType"" FullName=""System.MyParentType"">
  <TypeSignature Language=""DocId"" Value=""T:System.MyParentType"" />
  <AssemblyInfo>
    <AssemblyName>System</AssemblyName>
  </AssemblyInfo>
  <Base>
    <BaseTypeName>System.MyParentType</BaseTypeName>
  </Base>
  <Interfaces />
  <Docs>
    <summary>This is the summary of the MyParentType class.</summary>
    <remarks>These are the remarks of the MyParentType class.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:System.MyParentType.MyMethod"" />
      <MemberType>Method</MemberType>
      <Implements />
      <AssemblyInfo>
        <AssemblyName>System</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Parameters />
      <Docs>
        <summary>This is the summary of the MyParentType.MyMethod method.</summary>
        <remarks>These are the remarks of the MyParentType.MyMethod method.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string originalChildType = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Base>
    <BaseTypeName>System.MyParentType</BaseTypeName>
  </Base>
  <Interfaces />
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <Implements />
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Parameters />
      <Docs>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedChildType = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Base>
    <BaseTypeName>System.MyParentType</BaseTypeName>
  </Base>
  <Interfaces />
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
    <inheritdoc />
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <Implements />
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Parameters />
      <Docs>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
        <inheritdoc cref=""M:System.MyParentType.MyMethod"" />
      </Docs>
    </Member>
  </Members>
</Type>";

            List<string> intellisenseFiles = new()
            {
                intellisenseParentType,
                intellisenseChildType
            };

            List<StringTestData> docFiles = new()
            {
                new StringTestData(originalBaseType, expectedBaseType),
                new StringTestData(originalChildType, expectedChildType)
            };

            Configuration configuration = new();
            configuration.PreserveInheritDocTag = true;
            configuration.IncludedAssemblies.Add("MyAssembly");
            configuration.IncludedAssemblies.Add("System");

            TestWithStrings(intellisenseFiles, docFiles, configuration);
        }

        [Fact]
        public void PartialInheritDoc()
        {
            // The <inheritdoc/> tag can be used to inherit documentation without having to port it.
            // The exception to the rule are any items with explicit intellisense, which will always be ported.

            string intellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.IMyInterface"">
      <summary>The IMyInterface summary.</summary>
      <remarks>The IMyInterface remarks.</remarks>
    </member>
    <member name=""M:MyNamespace.IMyInterface.MyMethod(System.String,System.Int32)"">
      <summary>The IMyInterface.MyMethod summary.</summary>
      <remarks>The IMyInterface.MyMethod remarks.</remarks>
      <param name=""myParam1"">The IMyInterface.MyMethod myParam1 description.</param>
      <param name=""myParam2"">The IMyInterface.MyMethod myParam2 description.</param>
    </member>
    <member name=""F:MyNamespace.IMyInterface.MyField"">
      <summary>The IMyInterface.MyField summary.</summary>
      <remarks>The IMyInterface.MyField remarks.</remarks>
    </member>
    <member name=""T:MyNamespace.MyType"">
      <summary></summary>
      <inheritdoc/>
    </member>
    <member name=""M:MyNamespace.MyType.MyMethod(System.String,System.Int32)"">
      <param name=""myParam2"">The MyType.MyMethod myParam2 description.</param>
      <summary>The MyType.MyMethod summary.</summary>
      <inheritdoc/>
    </member>
    <member name=""F:MyNamespace.MyType.MyField"">
      <summary></summary>
      <remarks>The MyType.MyField remarks.</remarks>
      <inheritdoc cref=""F:MyNamespace.IMyInterface.MyField"" />
    </member>
  </members>
</doc>";

            string interfaceOriginalDocs = @"<Type Name=""IMyInterface"" FullName=""MyNamespace.IMyInterface"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.IMyInterface"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.IMyInterface.MyMethod(System.String,System.Int32)"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Parameters>
        <Parameter Name=""myParam1"" Type=""System.String"" />
        <Parameter Name=""myParam2"" Type=""System.Int32"" />
      </Parameters>
      <Docs>
        <param name=""myParam1"">To be added.</param>
        <param name=""myParam2"">To be added.</param>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyField"">
      <MemberSignature Language=""DocId"" Value=""F:MyNamespace.IMyInterface.MyField"" />
      <MemberType>Field</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Int32</ReturnType>
      </ReturnValue>
      <MemberValue>1</MemberValue>
      <Docs>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string interfaceExpectedDocs = @"<Type Name=""IMyInterface"" FullName=""MyNamespace.IMyInterface"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.IMyInterface"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>The IMyInterface summary.</summary>
    <remarks>The IMyInterface remarks.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.IMyInterface.MyMethod(System.String,System.Int32)"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Parameters>
        <Parameter Name=""myParam1"" Type=""System.String"" />
        <Parameter Name=""myParam2"" Type=""System.Int32"" />
      </Parameters>
      <Docs>
        <param name=""myParam1"">The IMyInterface.MyMethod myParam1 description.</param>
        <param name=""myParam2"">The IMyInterface.MyMethod myParam2 description.</param>
        <summary>The IMyInterface.MyMethod summary.</summary>
        <remarks>The IMyInterface.MyMethod remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyField"">
      <MemberSignature Language=""DocId"" Value=""F:MyNamespace.IMyInterface.MyField"" />
      <MemberType>Field</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Int32</ReturnType>
      </ReturnValue>
      <MemberValue>1</MemberValue>
      <Docs>
        <summary>The IMyInterface.MyField summary.</summary>
        <remarks>The IMyInterface.MyField remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string originalDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Interfaces>
    <Interface>
      <InterfaceName>MyNamespace.IMyInterface</InterfaceName>
    </Interface>
  </Interfaces>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod(System.String,System.Int32)"" />
      <MemberType>Method</MemberType>
      <Implements>
        <InterfaceMember>M:MyNamespace.MyInterface.MyMethod(System.String,System.Int32)</InterfaceMember>
      </Implements>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Parameters>
        <Parameter Name=""myParam1"" Type=""System.String"" />
        <Parameter Name=""myParam2"" Type=""System.Int32"" />
      </Parameters>
      <Docs>
        <param name=""myParam1"">To be added.</param>
        <param name=""myParam2"">To be added.</param>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyField"">
      <MemberSignature Language=""DocId"" Value=""F:MyNamespace.MyType.MyField"" />
      <MemberType>Field</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Int32</ReturnType>
      </ReturnValue>
      <MemberValue>1</MemberValue>
      <Docs>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Interfaces>
    <Interface>
      <InterfaceName>MyNamespace.IMyInterface</InterfaceName>
    </Interface>
  </Interfaces>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
    <inheritdoc />
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod(System.String,System.Int32)"" />
      <MemberType>Method</MemberType>
      <Implements>
        <InterfaceMember>M:MyNamespace.MyInterface.MyMethod(System.String,System.Int32)</InterfaceMember>
      </Implements>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Parameters>
        <Parameter Name=""myParam1"" Type=""System.String"" />
        <Parameter Name=""myParam2"" Type=""System.Int32"" />
      </Parameters>
      <Docs>
        <param name=""myParam1"">To be added.</param>
        <param name=""myParam2"">The MyType.MyMethod myParam2 description.</param>
        <summary>The MyType.MyMethod summary.</summary>
        <remarks>To be added.</remarks>
        <inheritdoc />
      </Docs>
    </Member>
    <Member MemberName=""MyField"">
      <MemberSignature Language=""DocId"" Value=""F:MyNamespace.MyType.MyField"" />
      <MemberType>Field</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Int32</ReturnType>
      </ReturnValue>
      <MemberValue>1</MemberValue>
      <Docs>
        <summary>To be added.</summary>
        <remarks>The MyType.MyField remarks.</remarks>
        <inheritdoc cref=""F:MyNamespace.IMyInterface.MyField"" />
      </Docs>
    </Member>
  </Members>
</Type>";

            List<StringTestData> docFiles = new()
            {
                new StringTestData(originalDocs, expectedDocs),
                new StringTestData(interfaceOriginalDocs, interfaceExpectedDocs)
            };

            Configuration configuration = new();
            configuration.PreserveInheritDocTag = true;
            configuration.IncludedAssemblies.Add("MyAssembly");

            TestWithStrings(intellisense, docFiles, configuration);
        }

        // [Fact]
        // TODO: Implement functionality for PreserveInheritDocTag=false
        //public void IgnoreInheritDoc()
        private void IgnoreInheritDoc()
        {
            // If PreserveInheritDocTag is set to false, then if the inheritdoc tag is found,
            // all the documentation strings are copied from the ancestors, and the inheritdoc tag
            // is not added to the docs xml.

            string intellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.IMyInterface"">
      <summary>The IMyInterface summary.</summary>
      <remarks>The IMyInterface remarks.</remarks>
    </member>
    <member name=""M:MyNamespace.IMyInterface.MyMethod(System.String,System.Int32)"">
      <summary>The IMyInterface.MyMethod summary.</summary>
      <remarks>The IMyInterface.MyMethod remarks.</remarks>
      <param name=""myParam1"">The IMyInterface.MyMethod myParam1 description.</param>
      <param name=""myParam2"">The IMyInterface.MyMethod myParam2 description.</param>
    </member>
    <member name=""F:MyNamespace.IMyInterface.MyField"">
      <summary>The IMyInterface.MyField summary.</summary>
      <remarks>The IMyInterface.MyField remarks.</remarks>
    </member>
    <member name=""T:MyNamespace.MyType"">
      <summary></summary>
      <inheritdoc/>
    </member>
    <member name=""M:MyNamespace.MyType.MyMethod(System.String,System.Int32)"">
      <param name=""myParam2"">The MyType.MyMethod myParam2 description.</param>
      <summary>The MyType.MyMethod summary.</summary>
      <inheritdoc/>
    </member>
    <member name=""F:MyNamespace.MyType.MyField"">
      <summary></summary>
      <remarks>The MyType.MyField remarks.</remarks>
      <inheritdoc cref=""F:MyNamespace.IMyInterface.MyField"" />
    </member>
  </members>
</doc>";

            string interfaceOriginalDocs = @"<Type Name=""IMyInterface"" FullName=""MyNamespace.IMyInterface"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.IMyInterface"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.IMyInterface.MyMethod(System.String,System.Int32)"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Parameters>
        <Parameter Name=""myParam1"" Type=""System.String"" />
        <Parameter Name=""myParam2"" Type=""System.Int32"" />
      </Parameters>
      <Docs>
        <param name=""myParam1"">To be added.</param>
        <param name=""myParam2"">To be added.</param>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyField"">
      <MemberSignature Language=""DocId"" Value=""F:MyNamespace.IMyInterface.MyField"" />
      <MemberType>Field</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Int32</ReturnType>
      </ReturnValue>
      <MemberValue>1</MemberValue>
      <Docs>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string interfaceExpectedDocs = @"<Type Name=""IMyInterface"" FullName=""MyNamespace.IMyInterface"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.IMyInterface"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>The IMyInterface summary.</summary>
    <remarks>The IMyInterface remarks.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.IMyInterface.MyMethod(System.String,System.Int32)"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Parameters>
        <Parameter Name=""myParam1"" Type=""System.String"" />
        <Parameter Name=""myParam2"" Type=""System.Int32"" />
      </Parameters>
      <Docs>
        <param name=""myParam1"">The IMyInterface.MyMethod myParam1 description.</param>
        <param name=""myParam2"">The IMyInterface.MyMethod myParam2 description.</param>
        <summary>The IMyInterface.MyMethod summary.</summary>
        <remarks>The IMyInterface.MyMethod remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyField"">
      <MemberSignature Language=""DocId"" Value=""F:MyNamespace.IMyInterface.MyField"" />
      <MemberType>Field</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Int32</ReturnType>
      </ReturnValue>
      <MemberValue>1</MemberValue>
      <Docs>
        <summary>The IMyInterface.MyField summary.</summary>
        <remarks>The IMyInterface.MyField remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string originalDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Interfaces>
    <Interface>
      <InterfaceName>MyNamespace.IMyInterface</InterfaceName>
    </Interface>
  </Interfaces>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod(System.String,System.Int32)"" />
      <MemberType>Method</MemberType>
      <Implements>
        <InterfaceMember>M:MyNamespace.MyInterface.MyMethod(System.String,System.Int32)</InterfaceMember>
      </Implements>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Parameters>
        <Parameter Name=""myParam1"" Type=""System.String"" />
        <Parameter Name=""myParam2"" Type=""System.Int32"" />
      </Parameters>
      <Docs>
        <param name=""myParam1"">To be added.</param>
        <param name=""myParam2"">To be added.</param>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyField"">
      <MemberSignature Language=""DocId"" Value=""F:MyNamespace.MyType.MyField"" />
      <MemberType>Field</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Int32</ReturnType>
      </ReturnValue>
      <MemberValue>1</MemberValue>
      <Docs>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Interfaces>
    <Interface>
      <InterfaceName>MyNamespace.IMyInterface</InterfaceName>
    </Interface>
  </Interfaces>
  <Docs>
    <summary>The IMyInterface summary.</summary>
    <remarks>The IMyInterface remarks.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod(System.String,System.Int32)"" />
      <MemberType>Method</MemberType>
      <Implements>
        <InterfaceMember>M:MyNamespace.MyInterface.MyMethod(System.String,System.Int32)</InterfaceMember>
      </Implements>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Parameters>
        <Parameter Name=""myParam1"" Type=""System.String"" />
        <Parameter Name=""myParam2"" Type=""System.Int32"" />
      </Parameters>
      <Docs>
        <param name=""myParam1"">The IMyInterface.MyMethod myParam1 description.</param>
        <param name=""myParam2"">The MyType.MyMethod myParam2 description.</param>
        <summary>The MyType.MyMethod summary.</summary>
        <remarks>The IMyInterface.MyMethod remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyField"">
      <MemberSignature Language=""DocId"" Value=""F:MyNamespace.MyType.MyField"" />
      <MemberType>Field</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Int32</ReturnType>
      </ReturnValue>
      <MemberValue>1</MemberValue>
      <Docs>
        <summary>The IMyInterface.MyField summary.</summary>
        <remarks>The MyType.MyField remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            List<StringTestData> docFiles = new()
            {
                new StringTestData(originalDocs, expectedDocs),
                new StringTestData(interfaceOriginalDocs, interfaceExpectedDocs)
            };

            Configuration configuration = new();
            configuration.PreserveInheritDocTag = false;
            configuration.IncludedAssemblies.Add("MyAssembly");

            TestWithStrings(intellisense, docFiles, configuration);
        }


        private static void TestWithStrings(string originalIntellisense, string originalDocs, string expectedDocs, Configuration configuration) =>
            TestWithStrings(originalIntellisense, new List<StringTestData>() { new StringTestData(originalDocs, expectedDocs) }, configuration);

        private static void TestWithStrings(string originalIntellisense, List<StringTestData> docFiles, Configuration configuration) =>
            TestWithStrings(new List<string>() { originalIntellisense }, docFiles, configuration);

        private static void TestWithStrings(List<string> intellisenseFiles, List<StringTestData> docFiles, Configuration configuration)
        {
            var porter = new ToDocsPorter(configuration);

            int iFileNumber = 0;
            foreach (string intellisenseFile in intellisenseFiles)
            {
                XDocument xIntellisense = XDocument.Parse(intellisenseFile);
                porter.LoadIntellisenseXmlFile(xIntellisense, $"IntelliSense{iFileNumber++}.xml");
            }

            UTF8Encoding utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

            int dFileNumber = 0;
            foreach (StringTestData docFile in docFiles)
            {
                porter.LoadDocsFile(docFile.XDoc, $"Doc{dFileNumber++}.xml", encoding: utf8NoBom);
            }

            porter.Start();

            foreach (StringTestData docFile in docFiles)
            {
                Assert.Equal(docFile.Expected, docFile.Actual);
            }
        }
    }


}
