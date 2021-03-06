﻿using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace Amadevus.RecordGenerator.Test
{
    public class GeneratorVersionDifferentTest : GeneratorCodeFixVerifier
    {
        [Theory]
        [ClassData(typeof(TestCases))]
        public void Given_Source_Then_Verify_Diagnostic_And_Codefix(
            string description, GeneratorSourcePackage sourcePackage, DiagnosticResult[] diagnosticResults)
        {
            VerifyCSharpDiagnostic(sourcePackage.GetInputSources(), diagnosticResults);
            VerifyCSharpGeneratorFix(sourcePackage);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new GenerateRecordPartialCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new RecordGeneratorAnalyzer();
        }

        private class TestCases : TheoryDataProvider
        {
            public override IEnumerable<ITheoryDatum> GetDataSets()
            {
                var oldVersion = "0.0.123.456";
                var newVersion = Properties.VersionString;
                const string @namespace = "RecordGeneratorTests";
                const string typeName = "Person";

                var source = @"
namespace RecordGeneratorTests
{
    [Record]
    partial class Person
    {
        public string FirstName { get; }

        public string LastName { get; }
    }
}";
                var partial = @"// Record partial generated by RecordGenerator
// WARNING any changes made to this file will be lost when generator is run again

namespace RecordGeneratorTests
{
    [System.CodeDom.Compiler.GeneratedCode(""RecordGenerator"", ""GENERATOR_VERSION"")]
    partial class Person
    {
        public Person(string FirstName, string LastName)
        {
            this.FirstName = FirstName;
            this.LastName = LastName;
        }

        public Person WithFirstName(string FirstName)
        {
            return new Person
(FirstName, LastName);
        }

        public Person WithLastName(string LastName)
        {
            return new Person
(FirstName, LastName);
        }
    }
}";


                yield return new GeneratorTheoryData
                {
                    Description = "same version",
                    SourcePackage = new GeneratorSourcePackage
                    {
                        OldSource = source,
                        AdditionalSources = new[]
                        {
                            GenerateRecordAttributeDeclarationCodeFixProvider.RecordAttributeDeclarationSource(@namespace),
                            partial.ReplaceRecordGeneratorVersion(newVersion)
                        }
                    }.AndFixedSameAsOld(),
                    ExpectedDiagnostics = new DiagnosticResult[] { }
                };
                yield return new GeneratorTheoryData
                {
                    Description = "different version",
                    SourcePackage = new GeneratorSourcePackage
                    {
                        OldSource = source,
                        AdditionalSources = new[]
                        {
                            GenerateRecordAttributeDeclarationCodeFixProvider.RecordAttributeDeclarationSource(@namespace),
                            partial.ReplaceRecordGeneratorVersion(oldVersion)
                        },
                        ChangedSource = partial.ReplaceRecordGeneratorVersion(newVersion)
                    }.AndFixedSameAsOld(),
                    ExpectedDiagnostics = new[]
                    {
                        new DiagnosticResult(GeneratorVersionDifferentDiagnostic.Descriptor, typeName, oldVersion, newVersion)
                        {
                            Locations =
                            new[] {
                                new DiagnosticResultLocation("Test0.cs", 5, 19)
                            }
                        }
                    }
                };
            }
        }

        public class GeneratorTheoryData : ITheoryDatum
        {
            public string Description { get; set; }

            public GeneratorSourcePackage SourcePackage { get; set; }

            public DiagnosticResult[] ExpectedDiagnostics { get; set; }

            public object[] ToParameterArray()
            {
                return
                    new object[]
                    {
                        Description,
                        SourcePackage,
                        ExpectedDiagnostics
                    };
            }
        }
    }
}
