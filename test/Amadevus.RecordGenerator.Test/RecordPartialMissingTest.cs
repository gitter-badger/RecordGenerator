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
    public class RecordPartialMissingTest : GeneratorCodeFixVerifier
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
                const string @namespace = "RecordGeneratorTests";
                const string typeName = "Person";
                yield return new GeneratorTheoryData
                {
                    Description = "basic string properties",
                    SourcePackage = new GeneratorSourcePackage
                    {
                        OldSource = @"
namespace RecordGeneratorTests
{
    [Record]
    partial class Person
    {
        public string FirstName { get; }

        public string LastName { get; }
    }
}",
                        AddedSource = @"// Record partial generated by RecordGenerator

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
}".ReplaceRecordGeneratorVersion(Properties.VersionString),
                        AdditionalSources = new[] { GenerateRecordAttributeDeclarationCodeFixProvider.RecordAttributeDeclarationSource(@namespace) }
                    }.AndFixedSameAsOld(),
                    ExpectedDiagnostics = new[]
                    {
                        new DiagnosticResult(RecordPartialMissingDiagnostic.Descriptor, typeName)
                        {
                            Locations =
                            new[] {
                                new DiagnosticResultLocation("Test0.cs", 5, 19)
                            }
                        }
                    }
                };
                yield return new GeneratorTheoryData
                {
                    Description = "no partial in original",
                    SourcePackage = new GeneratorSourcePackage
                    {
                        OldSource = @"
namespace RecordGeneratorTests
{
    [Record]
    class Person
    {
        public string FirstName { get; }

        public string LastName { get; }
    }
}",
                        FixedSource = @"
namespace RecordGeneratorTests
{
    [Record]
    partial class Person
    {
        public string FirstName { get; }

        public string LastName { get; }
    }
}",
                        AddedSource = @"// Record partial generated by RecordGenerator

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
}".ReplaceRecordGeneratorVersion(Properties.VersionString),
                        AdditionalSources = new[] { GenerateRecordAttributeDeclarationCodeFixProvider.RecordAttributeDeclarationSource(@namespace) }
                    },
                    ExpectedDiagnostics = new[]
                    {
                        new DiagnosticResult(RecordPartialMissingDiagnostic.Descriptor, typeName)
                        {
                            Locations =
                            new[] {
                                new DiagnosticResultLocation("Test0.cs", 5, 11)
                            }
                        }
                    }
                };
                yield return new GeneratorTheoryData
                {
                    Description = "multiple (also type-parametrized) properties",
                    SourcePackage = new GeneratorSourcePackage
                    {
                        OldSource = @"
namespace RecordGeneratorTests
{
    [Record]
    partial class Person<T>
    {
        public T OtherProperty { get; }

        public string FirstName { get; }

        public string LastName { get; }

        public string Address { get; }

        public DateTime Birthday { get; }
    }
}",
                        AddedSource = @"// Record partial generated by RecordGenerator

namespace RecordGeneratorTests
{
    [System.CodeDom.Compiler.GeneratedCode(""RecordGenerator"", ""GENERATOR_VERSION"")]
    partial class Person<T>
    {
        public Person(T OtherProperty, string FirstName, string LastName, string Address, DateTime Birthday)
        {
            this.OtherProperty = OtherProperty;
            this.FirstName = FirstName;
            this.LastName = LastName;
            this.Address = Address;
            this.Birthday = Birthday;
        }

        public Person<T> WithOtherProperty(T OtherProperty)
        {
            return new Person<T>(OtherProperty, FirstName, LastName, Address, Birthday);
        }

        public Person<T> WithFirstName(string FirstName)
        {
            return new Person<T>(OtherProperty, FirstName, LastName, Address, Birthday);
        }

        public Person<T> WithLastName(string LastName)
        {
            return new Person<T>(OtherProperty, FirstName, LastName, Address, Birthday);
        }

        public Person<T> WithAddress(string Address)
        {
            return new Person<T>(OtherProperty, FirstName, LastName, Address, Birthday);
        }

        public Person<T> WithBirthday(DateTime Birthday)
        {
            return new Person<T>(OtherProperty, FirstName, LastName, Address, Birthday);
        }
    }
}".ReplaceRecordGeneratorVersion(Properties.VersionString),
                        AdditionalSources = new[] { GenerateRecordAttributeDeclarationCodeFixProvider.RecordAttributeDeclarationSource(@namespace) }
                    }.AndFixedSameAsOld(),
                    ExpectedDiagnostics = new[]
                    {
                        new DiagnosticResult(RecordPartialMissingDiagnostic.Descriptor, typeName)
                        {
                            Locations =
                            new[] {
                                new DiagnosticResultLocation("Test0.cs", 5, 19)
                            }
                        }
                    }
                };
                yield return new GeneratorTheoryData
                {
                    Description = "nested namespaces, usings",
                    SourcePackage = new GeneratorSourcePackage
                    {
                        OldSource = @"
using System;
using System.Linq;

namespace RecordGeneratorTests
{
    namespace Outer
    {
        using SomethingInner;

        namespace Inner.InnerMost
        {
            [Record]
            partial class Person
            {
                public string FirstName { get; }
            }
        }
    }
}",
                        AddedSource = @"// Record partial generated by RecordGenerator

using System;
using System.Linq;

namespace RecordGeneratorTests
{
    namespace Outer
    {
        using SomethingInner;

        namespace Inner.InnerMost
        {
            [System.CodeDom.Compiler.GeneratedCode(""RecordGenerator"", ""GENERATOR_VERSION"")]
            partial class Person
            {
                public Person(string FirstName)
                {
                    this.FirstName = FirstName;
                }

                public Person WithFirstName(string FirstName)
                {
                    return new Person
(FirstName);
                }
            }
        }
    }
}".ReplaceRecordGeneratorVersion(Properties.VersionString),
                        AdditionalSources = new[] { GenerateRecordAttributeDeclarationCodeFixProvider.RecordAttributeDeclarationSource(@namespace) }
                    }.AndFixedSameAsOld(),
                    ExpectedDiagnostics = new[]
                    {
                        new DiagnosticResult(RecordPartialMissingDiagnostic.Descriptor, typeName)
                        {
                            Locations =
                            new[] {
                                new DiagnosticResultLocation("Test0.cs", 14, 27)
                            }
                        }
                    }
                };
                // TODO fix type-enclosed records, github issue #8 ( https://github.com/amis92/RecordGenerator/issues/8 )
                //yield return
                var unused =
                new GeneratorTheoryData
                {
                    Description = "nested types",
                    SourcePackage = new GeneratorSourcePackage
                    {
                        OldSource = @"
namespace RecordGeneratorTests
{
    class Outer
    {
        class Inner<T>
        {
            [Record]
            class Person
            {
                public string FirstName { get; }
            }
        }
    }
}",
                        FixedSource = @"
namespace RecordGeneratorTests
{
    partial class Outer
    {
        partial class Inner<T>
        {
            [Record]
            partial class Person
            {
                public string FirstName { get; }
            }
        }
    }
}",
                        AddedSource = @"// Record partial generated by RecordGenerator

namespace RecordGeneratorTests
{
    partial class Outer
    {
        partial class Inner<T>
        {
            [System.CodeDom.Compiler.GeneratedCode(""RecordGenerator"", ""GENERATOR_VERSION"")]
            partial class Person
            {
                public Person(string FirstName)
                {
                    this.FirstName = FirstName;
                }

                public Person WithFirstName(string FirstName)
                {
                    return new Person
(FirstName);
                }
            }
        }
    }
}".ReplaceRecordGeneratorVersion(Properties.VersionString),
                        AdditionalSources = new[] { GenerateRecordAttributeDeclarationCodeFixProvider.RecordAttributeDeclarationSource(@namespace) }
                    },
                    ExpectedDiagnostics = new[]
                    {
                        new DiagnosticResult(RecordPartialMissingDiagnostic.Descriptor, typeName)
                        {
                            Locations =
                            new[] {
                                new DiagnosticResultLocation("Test0.cs", 9, 19)
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