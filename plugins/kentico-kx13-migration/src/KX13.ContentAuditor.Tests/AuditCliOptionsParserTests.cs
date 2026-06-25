using KX13.ContentAuditor.CLI;

namespace KX13.ContentAuditor.Tests
{
    public class AuditCliOptionsParserTests
    {
        [Test]
        public void Parse_ReportsUnknownFlagAndStops()
        {
            AuditCliOptions options = AuditCliOptionsParser.Parse(["--unknown", "--output"]);

            Assert.That(options.Errors, Is.EqualTo(new[]
            {
                "Unexpected argument: '--unknown'."
            }));
        }

        [Test]
        public void Parse_DoesNotConsumeFollowingFlagAsValue()
        {
            AuditCliOptions options = AuditCliOptionsParser.Parse(["--output", "--sites"]);

            Assert.That(options.Errors, Is.EqualTo(new[]
            {
                "Flag '--output' requires a value."
            }));
        }

        [Test]
        public void Parse_WithoutExportFlagsMeansFullExport()
        {
            AuditCliOptions options = AuditCliOptionsParser.Parse([]);

            Assert.Multiple(() =>
            {
                Assert.That(options.ExportAll, Is.True);
                Assert.That(options.HasJsonExport, Is.True);
                Assert.That(options.GenerateReport, Is.False);
            });
        }

        [Test]
        public void Parse_PreservesOutputAndFilterValuesExactly()
        {
            AuditCliOptions options = AuditCliOptionsParser.Parse([
                "--output", "./custom output",
                "--site-name", "DancingGoatMvc",
                "--class-name", "Demo.*,CMS.MenuItem",
                "--page-path", "/Articles/"
            ]);

            Assert.Multiple(() =>
            {
                Assert.That(options.Errors, Is.Empty);
                Assert.That(options.OutputPath, Is.EqualTo("./custom output"));
                Assert.That(options.SiteName, Is.EqualTo("DancingGoatMvc"));
                Assert.That(options.ClassNamePattern, Is.EqualTo("Demo.*,CMS.MenuItem"));
                Assert.That(options.PagePathPrefix, Is.EqualTo("/Articles/"));
            });
        }
    }
}
