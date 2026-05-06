using KX13.ContentAuditor.DataAccess;
using KX13.ContentAuditor.DataAccess.Models;
using KX13.ContentAuditor.DataAccess.Parsers;
using System.Collections.Generic;

namespace KX13.ContentAuditor.Tests
{
    public class ClassFormDefinitionParserTests
    {
        [Test]
        public void TryParseFieldDefinitions_ParsesPageTypeFieldsAndSettings()
        {
        var (failureCollector, parser) = CreateParser();
            Guid fieldGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");
            const string xml = """
                <form>
                  <category name="Content" />
                  <field guid="11111111-1111-1111-1111-111111111111" column="RelatedPages" columntype="text" columnsize="500" columnprecision="0" required="true" visible="false" system="true">
                    <properties>
                      <fieldcaption>Related pages</fieldcaption>
                      <defaultvalue>home</defaultvalue>
                      <required>true</required>
                    </properties>
                    <settings>
                      <controlname>PageSelector</controlname>
                      <componentidentifier>Kentico.PageSelector</componentidentifier>
                      <ObjectType>cms.document</ObjectType>
                      <ReferenceType>dependsOn</ReferenceType>
                      <RootPath>/Articles</RootPath>
                      <MaxItems>5</MaxItems>
                    </settings>
                  </field>
                  <category name="Metadata" />
                  <field column="Summary" columntype="nvarchar" visible="true">
                    <properties>
                      <fieldcaption>Summary</fieldcaption>
                    </properties>
                  </field>
                </form>
                """;

            var fields = parser.TryParseFieldDefinitions(xml, "Page type", "Demo.Article", "Article");

            Assert.That(fields, Has.Count.EqualTo(2));

            Assert.Multiple(() =>
            {
                Assert.That(fields[0].FieldGuid, Is.EqualTo(fieldGuid));
                Assert.That(fields[0].FieldName, Is.EqualTo("RelatedPages"));
                Assert.That(fields[0].FieldCaption, Is.EqualTo("Related pages"));
                Assert.That(fields[0].Category, Is.EqualTo("Content"));
                Assert.That(fields[0].Order, Is.EqualTo(0));
                Assert.That(fields[0].IsRequired, Is.True);
                Assert.That(fields[0].IsVisible, Is.False);
                Assert.That(fields[0].IsSystemPageField, Is.True);
                Assert.That(fields[0].FormControlName, Is.EqualTo("PageSelector"));
                Assert.That(fields[0].FormComponentIdentifier, Is.EqualTo("Kentico.PageSelector"));
                Assert.That(fields[0].ReferenceToObjectType, Is.EqualTo("cms.document"));
                Assert.That(fields[0].ReferenceType, Is.EqualTo("dependsOn"));
                Assert.That(fields[0].FormControlSettings["RootPath"], Is.EqualTo("/Articles"));
                Assert.That(fields[0].FormControlSettings["MaxItems"], Is.EqualTo("5"));
                Assert.That(fields[1].Category, Is.EqualTo("Metadata"));
                Assert.That(fields[1].Order, Is.EqualTo(1));
                Assert.That(fields[1].IsVisible, Is.True);
                Assert.That(failureCollector.GetFailures(), Is.Empty);
            });
        }

        [Test]
        public void TryParseFormFieldDefinitions_ParsesFormSpecificMetadata()
        {
          var (failureCollector, parser) = CreateParser();
            const string xml = """
                <form>
                  <category name="Contact" />
                  <field column="Email" columntype="text" visible="true">
                    <properties>
                      <fieldcaption>Email</fieldcaption>
                      <explanationtext>We only use this for follow-up.</explanationtext>
                      <tooltip>Enter a valid address</tooltip>
                    </properties>
                    <settings>
                      <controlname>TextBoxControl</controlname>
                      <componentidentifier>Kentico.TextInput</componentidentifier>
                      <livesitecomponentidentifier>Custom.LiveSite.Email</livesitecomponentidentifier>
                    </settings>
                    <validationrule>{"rule":"Email"}</validationrule>
                    <validationerrormessage>Email is required.</validationerrormessage>
                    <visibilitycondition>{"field":"OptIn","operator":"eq","value":true}</visibilitycondition>
                  </field>
                </form>
                """;

            var fields = parser.TryParseFormFieldDefinitions(xml, "ContactUs", "Contact us");

            Assert.That(fields, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(fields[0].Category, Is.EqualTo("Contact"));
                Assert.That(fields[0].LiveSiteFormComponentIdentifier, Is.EqualTo("Custom.LiveSite.Email"));
                Assert.That(fields[0].ValidationRule, Is.EqualTo("{\"rule\":\"Email\"}"));
                Assert.That(fields[0].ValidationErrorMessage, Is.EqualTo("Email is required."));
                Assert.That(fields[0].VisibilityCondition, Is.EqualTo("{\"field\":\"OptIn\",\"operator\":\"eq\",\"value\":true}"));
                Assert.That(fields[0].ExplanationText, Is.EqualTo("We only use this for follow-up."));
                Assert.That(fields[0].Tooltip, Is.EqualTo("Enter a valid address"));
                Assert.That(failureCollector.GetFailures(), Is.Empty);
            });
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void TryParseFieldDefinitions_ReturnsEmptyListForNullOrBlankXml(string? xml)
        {
          var (failureCollector, parser) = CreateParser();
            var fields = parser.TryParseFieldDefinitions(xml, "Page type", "Demo.Article", "Article");

            Assert.Multiple(() =>
            {
                Assert.That(fields, Is.Empty);
                Assert.That(failureCollector.GetFailures(), Is.Empty);
            });
        }

        [TestCaseSource(nameof(MalformedFieldDefinitionXmlCases))]
        public void TryParseFieldDefinitions_RecordsRecoverableFailuresForMalformedXml(string xml)
        {
            var (failureCollector, parser) = CreateParser();
            var fields = parser.TryParseFieldDefinitions(xml, "Page type", "Broken.Article", "Broken");

            List<AuditFailure> failures = failureCollector.GetFailures();

            AssertRecoverableFailure(
                fields,
                failures,
                "Field definition parsing",
                "Page type",
                "Broken.Article",
                "Broken");
        }

        [TestCaseSource(nameof(MalformedFormFieldDefinitionXmlCases))]
        public void TryParseFormFieldDefinitions_RecordsRecoverableFailuresForMalformedXml(string xml)
        {
            var (failureCollector, parser) = CreateParser();
            var fields = parser.TryParseFormFieldDefinitions(xml, "BrokenForm", "Broken form context");

            List<AuditFailure> failures = failureCollector.GetFailures();

            AssertRecoverableFailure(
                fields,
                failures,
                "Form field parsing",
                "Form",
                "BrokenForm",
                "Broken form context");
        }

        private static IEnumerable<TestCaseData> MalformedFieldDefinitionXmlCases()
        {
            yield return new TestCaseData(
                """
                <form version="2">
                  <field column="CompanyID" columntype="integer" guid="4a07f7fa-7422-4179-9d89-c38a5c3b3ec4" isPK="true">
                    <properties>
                      <fieldcaption>CompanyID</fieldcaption>
                    </properties>
                  </field>
                  <field allowempty="true" column="Street" columnsize="100" columntype="text" guid="51b09bf6-6e58-4692-8577-869a6d1b531d" visible="true">
                    <properties>
                      <fieldcaption>Street</fieldcaption>
                    </properties>
                    <settings>
                      <AutoCompleteEnableCaching>False</AutoCompleteEnableCaching>
                      <controlname>TextBoxControl</controlname>
                      <Trim>False</Trim>
                  </field>
                </form>
                """)
                .SetName("TryParseFieldDefinitions_RecordsFailure_WhenSettingsTagIsTruncated");

            yield return new TestCaseData(
                """
                <form version="2">
                  <field column="SocialLinkID" columntype="integer" guid="d322b8d0-f13a-4eb7-9227-5e00b75336f8" isPK="true">
                    <properties>
                      <fieldcaption>SocialLinkID</fieldcaption>
                    </properties>
                  </field>
                  <field allowempty="true" column="Url" columnsize="200" columntype="text" guid="33664f59-fffd-4753-846d-64513fc6c1dc" visible="true">
                    <properties>
                      <fieldcaption>Research & Development URL</fieldcaption>
                    </properties>
                    <settings>
                      <controlname>URLSelector</controlname>
                    </settings>
                  </field>
                </form>
                """)
                .SetName("TryParseFieldDefinitions_RecordsFailure_WhenTextContainsUnescapedAmpersand");
        }

        private static IEnumerable<TestCaseData> MalformedFormFieldDefinitionXmlCases()
        {
            yield return new TestCaseData(
                """
                <form version="2">
                  <category name="Contact" />
                  <field column="Email" columntype="text" visible="true">
                    <properties>
                      <fieldcaption>Email</fieldcaption>
                      <explanationtext>We only use this for follow-up.</explanationtext>
                      <tooltip>Enter a valid address</tooltip>
                    </properties>
                    <settings>
                      <controlname>TextBoxControl</controlname>
                      <componentidentifier>Kentico.TextInput</componentidentifier>
                      <livesitecomponentidentifier>Custom.LiveSite.Email</livesitecomponentidentifier>
                    </settings>
                    <validationrule>{"rule":"Email"}
                    <validationerrormessage>Email is required.</validationerrormessage>
                  </field>
                </form>
                """)
                .SetName("TryParseFormFieldDefinitions_RecordsFailure_WhenValidationTagIsTruncated");

            yield return new TestCaseData(
                """
                <form version="2">
                  <category name="Contact" />
                  <field column="Email" columntype="text" visible="true">
                    <properties>
                      <fieldcaption>Email</fieldcaption>
                      <explanationtext>Use this for appointments & reminders.</explanationtext>
                      <tooltip>Enter a valid address</tooltip>
                    </properties>
                    <settings>
                      <controlname>TextBoxControl</controlname>
                      <componentidentifier>Kentico.TextInput</componentidentifier>
                      <livesitecomponentidentifier>Custom.LiveSite.Email</livesitecomponentidentifier>
                    </settings>
                    <validationrule>{"rule":"Email"}</validationrule>
                    <validationerrormessage>Email is required.</validationerrormessage>
                    <visibilitycondition>{"field":"OptIn","operator":"eq","value":true}</visibilitycondition>
                  </field>
                </form>
                """)
                .SetName("TryParseFormFieldDefinitions_RecordsFailure_WhenTextContainsUnescapedAmpersand");
        }

        private static void AssertRecoverableFailure<T>(
            IReadOnlyCollection<T> fields,
            List<AuditFailure> failures,
            string category,
            string entityType,
            string entityIdentifier,
            string context)
        {
            Assert.Multiple(() =>
            {
                Assert.That(fields, Is.Empty);
                Assert.That(failures, Has.Count.EqualTo(1));
                Assert.That(failures[0].Category, Is.EqualTo(category));
                Assert.That(failures[0].EntityType, Is.EqualTo(entityType));
                Assert.That(failures[0].EntityIdentifier, Is.EqualTo(entityIdentifier));
                Assert.That(failures[0].Context, Is.EqualTo(context));
                Assert.That(failures[0].ErrorMessage, Does.StartWith("XmlException:"));
            });
        }

        private static (AuditFailureCollector FailureCollector, ClassFormDefinitionParser Parser) CreateParser()
        {
              var failureCollector = new AuditFailureCollector();
            return (failureCollector, new ClassFormDefinitionParser(failureCollector));
        }
    }
}