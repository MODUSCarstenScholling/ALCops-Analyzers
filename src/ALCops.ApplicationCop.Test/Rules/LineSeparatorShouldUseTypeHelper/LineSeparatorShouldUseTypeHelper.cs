using ALCops.ApplicationCop.CodeFixes;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using RoslynTestKit;

namespace ALCops.ApplicationCop.Test
{
    public class LineSeparatorShouldUseTypeHelper : NavCodeAnalysisBase
    {
        private static readonly byte[] ConfiguredReplacementSettings =
            System.Text.Encoding.UTF8.GetBytes(
                """
                {
                    "NamingPatterns": {
                        "LocalVariable": {
                            "AllowPattern": "^[a-z][A-Za-z0-9]*$"
                        }
                    },
                    "CodeFixOverrides": {
                        "AC0025": {
                            "Variable": "typeHelper: Codeunit \"My Type Helper\";",
                            "Methods": {
                                "LFSeparator": "GetLfSeparator",
                                "CRLFSeparator": "GetCrlfSeparator"
                            }
                        }
                    }
                }
                """);

        private AnalyzerTestFixture _fixture;
        private static readonly Analyzers.LineSeparatorShouldUseTypeHelper _analyzer = new();
        private string _testCasePath;

        [SetUp]
        public void Setup()
        {
            _fixture = RoslynFixtureFactory.Create<Analyzers.LineSeparatorShouldUseTypeHelper>();

            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    Path.Combine("Rules", nameof(LineSeparatorShouldUseTypeHelper)));
        }

        [Test]
        [TestCase("CRLFSeparatorChar")]
        [TestCase("CRLFSeparatorText")]
        [TestCase("LFSeparatorChar")]
        [TestCase("LFSeparatorCode")]
        [TestCase("LFSeparatorText")]
        public async Task HasDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.LineSeparatorShouldUseTypeHelper);
        }

        [Test]
        [TestCase("LFSeparatorCodeElementAccess3")]
        [TestCase("LFSeparatorTextElementAccess3")]
        public async Task NoDiagnostic(string testCase)
        {
            var code = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            _fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.LineSeparatorShouldUseTypeHelper);
        }

        [Test]
        [TestCase("CRLFSeparatorChar")]
        [TestCase("CRLFSeparatorText")]
        [TestCase("LFSeparatorChar")]
        [TestCase("LFSeparatorWithExistingGlobalTypeHelper")]
        [TestCase("LFSeparatorWithExistingLocalTypeHelper")]
        public async Task HasFix(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<LineSeparatorShouldUseTypeHelperCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer]
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.LineSeparatorShouldUseTypeHelper);
        }

        [Test]
        [TestCase("LFSeparatorConfiguredReplacement")]
        public async Task HasFixWithConfiguredReplacement(string testCase)
        {
            var currentCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "current.al"))
                .ConfigureAwait(false);

            var expectedCode = await File.ReadAllTextAsync(Path.Combine(_testCasePath, nameof(HasFix), testCase, "expected.al"))
                .ConfigureAwait(false);

            var fixture = RoslynFixtureFactory.Create<LineSeparatorShouldUseTypeHelperCodeFixProvider>(
                new CodeFixTestFixtureConfig
                {
                    AdditionalAnalyzers = [_analyzer],
                    FileSystem = new MemoryFileSystem(new Dictionary<string, byte[]>
                    {
                        { "alcops.json", ConfiguredReplacementSettings }
                    })
                });

            fixture.TestCodeFix(currentCode, expectedCode, DiagnosticDescriptors.LineSeparatorShouldUseTypeHelper);
        }
    }
}