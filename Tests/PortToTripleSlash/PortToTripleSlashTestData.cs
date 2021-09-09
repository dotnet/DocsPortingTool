﻿using Xunit;

namespace Libraries.Tests
{
    internal class PortToTripleSlashTestData : TestData
    {
        private const string SourceOriginal = "SourceOriginal.cs";
        private const string SourceExpected = "SourceExpected.cs";
        private const string ProjectDirName = "Project";
        private string TestDataRootDirPath => @"../../../PortToTripleSlash/TestData";

        private DirectoryInfo ProjectDir { get; set; }
        internal string ProjectFilePath { get; set; }

        internal PortToTripleSlashTestData(
            TestDirectory tempDir,
            string testDataDir,
            string assemblyName,
            string namespaceName)
        {
            Assert.False(string.IsNullOrWhiteSpace(assemblyName));

            namespaceName = string.IsNullOrEmpty(namespaceName) ? assemblyName : namespaceName;

            ProjectDir = tempDir.CreateSubdirectory(ProjectDirName);

            DocsDir = tempDir.CreateSubdirectory(DocsDirName);
            DirectoryInfo docsAssemblyDir = DocsDir.CreateSubdirectory(namespaceName);

            string testDataPath = Path.Combine(TestDataRootDirPath, testDataDir);

            foreach (string origin in Directory.EnumerateFiles(testDataPath, "*.xml"))
            {
                string fileName = Path.GetFileName(origin);
                string destination = Path.Combine(docsAssemblyDir.FullName, fileName);
                File.Copy(origin, destination);
            }

            string originCsOriginal = Path.Combine(testDataPath, SourceOriginal);
            ActualFilePath = Path.Combine(ProjectDir.FullName, SourceOriginal);
            File.Copy(originCsOriginal, ActualFilePath);

            string originCsExpected = Path.Combine(testDataPath, SourceExpected);
            ExpectedFilePath = Path.Combine(tempDir.FullPath, SourceExpected);
            File.Copy(originCsExpected, ExpectedFilePath);

            string originCsproj = Path.Combine(testDataPath, $"{assemblyName}.csproj");
            ProjectFilePath = Path.Combine(ProjectDir.FullName, $"{assemblyName}.csproj");
            File.Copy(originCsproj, ProjectFilePath);
        }
    }
}
