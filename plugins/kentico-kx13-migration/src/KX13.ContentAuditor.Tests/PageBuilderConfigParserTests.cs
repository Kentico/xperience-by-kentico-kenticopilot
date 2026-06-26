using KX13.ContentAuditor.DataAccess;
using KX13.ContentAuditor.DataAccess.Models;
using KX13.ContentAuditor.DataAccess.Parsers;
namespace KX13.ContentAuditor.Tests
{
    public class PageBuilderConfigParserTests
    {
        [Test]
        public void TryParse_ReturnsNullWhenBothInputsAreEmpty()
        {
          var (failureCollector, parser) = CreateParser();
          PageBuilderConfiguration? config = parser.TryParsePageBuilderConfiguration("  ", null, "/Home", "Main site");

          Assert.Multiple(() =>
          {
              Assert.That(config, Is.Null);
              Assert.That(failureCollector.GetFailures(), Is.Empty);
          });
        }

        [Test]
        public void TryParse_ParsesAreasSectionsWidgetsAndTemplateProperties()
        {
          var (failureCollector, parser) = CreateParser();
            const string widgetsJson = """
                {
                  "editableAreas": [
                    {
                      "identifier": "main",
                      "sections": [
                        {
                          "type": "Section.TwoColumns",
                          "propertiesType": "SectionProps",
                          "properties": {
                            "background": "sand",
                            "spacing": 8
                          },
                          "zones": [
                            {
                              "identifier": "left",
                              "widgets": [
                                {
                                  "type": "Widget.Hero",
                                  "propertiesType": "HeroProps",
                                  "conditionType": "Persona.Visitor",
                                  "variants": [
                                    {
                                      "identifier": "22222222-2222-2222-2222-222222222222",
                                      "name": "Default",
                                      "properties": {
                                        "title": "Welcome",
                                        "count": 7,
                                        "featured": true,
                                        "subtitle": null,
                                        "tags": ["one", 2, false],
                                        "link": {
                                          "url": "/contact",
                                          "newTab": false
                                        }
                                      },
                                      "conditionTypeParameters": {
                                        "group": "AllVisitors"
                                      }
                                    },
                                    {
                                      "identifier": "33333333-3333-3333-3333-333333333333",
                                      "name": "IgnoredForTopLevelProperties",
                                      "properties": {
                                        "title": "Second"
                                      }
                                    }
                                  ]
                                }
                              ]
                            }
                          ]
                        }
                      ]
                    }
                  ]
                }
                """;
            const string templateJson = """
                {
                  "identifier": "Template.Landing",
                  "propertiesType": "TemplateProps",
                  "properties": {
                    "layout": "wide",
                    "columns": 2
                  }
                }
                """;

            PageBuilderConfiguration? config = parser.TryParsePageBuilderConfiguration(widgetsJson, templateJson, "/Landing", "Landing page");

            Assert.That(config, Is.Not.Null);

            WidgetConfig widget = config!.EditableAreas[0].Sections[0].Zones[0].Widgets[0];
            Assert.Multiple(() =>
            {
                Assert.That(config.EditableAreas[0].Identifier, Is.EqualTo("main"));
                Assert.That(config.EditableAreas[0].Sections[0].TypeIdentifier, Is.EqualTo("Section.TwoColumns"));
                Assert.That(config.EditableAreas[0].Sections[0].Zones[0].Identifier, Is.EqualTo("left"));
                Assert.That(widget.TypeIdentifier, Is.EqualTo("Widget.Hero"));
                Assert.That(widget.PropertiesTypeName, Is.EqualTo("HeroProps"));
                Assert.That(widget.PersonalizationConditionTypeIdentifier, Is.EqualTo("Persona.Visitor"));
                Assert.That(widget.Variants[0].Identifier, Is.EqualTo(Guid.Parse("22222222-2222-2222-2222-222222222222")));
                Assert.That(widget.Properties["title"], Is.EqualTo("Welcome"));
                Assert.That(widget.Properties["count"], Is.EqualTo(7L));
                Assert.That(widget.Properties["featured"], Is.EqualTo(true));
                Assert.That(widget.Properties["subtitle"], Is.Null);
                Assert.That(widget.Properties["tags"], Is.TypeOf<List<object?>>());
                Assert.That(widget.Properties["link"], Is.TypeOf<Dictionary<string, object?>>());
                Assert.That(config.Template!.Identifier, Is.EqualTo("Template.Landing"));
                Assert.That(config.Template.PropertiesTypeName, Is.EqualTo("TemplateProps"));
                Assert.That(config.Template.Properties["layout"], Is.EqualTo("wide"));
                Assert.That(config.Template.Properties["columns"], Is.EqualTo(2L));
                Assert.That(failureCollector.GetFailures(), Is.Empty);
            });
        }

        [Test]
        public void TryParse_AcceptsTrailingCommasAndComments()
        {
          var (failureCollector, parser) = CreateParser();
            const string widgetsJson = """
                {
                  // comment before editable areas
                  "editableAreas": [
                    {
                      "identifier": "main",
                      "sections": [
                        {
                          "type": "Section.Single",
                          "zones": [
                            {
                              "identifier": "zone-1",
                              "widgets": [
                                {
                                  "type": "Widget.Text",
                                  "variants": [
                                    {
                                      "name": "Default",
                                      "properties": {
                                        "text": "Hello"
                                      },
                                    },
                                  ],
                                },
                              ],
                            },
                          ],
                        },
                      ],
                    },
                  ],
                }
                """;

            PageBuilderConfiguration? config = parser.TryParsePageBuilderConfiguration(widgetsJson, null, "/Home", "Main site");

            Assert.Multiple(() =>
            {
                Assert.That(config, Is.Not.Null);
                Assert.That(config!.EditableAreas[0].Sections[0].Zones[0].Widgets[0].Properties["text"], Is.EqualTo("Hello"));
                Assert.That(failureCollector.GetFailures(), Is.Empty);
            });
        }

        [TestCaseSource(nameof(MalformedPageBuilderJsonCases))]
        public void TryParse_RecordsRecoverableFailuresForMalformedJson(string? widgetsJson, string? templateJson)
        {
          var (failureCollector, parser) = CreateParser();
          PageBuilderConfiguration? config = parser.TryParsePageBuilderConfiguration(widgetsJson, templateJson, "/Broken", "Broken page");

            List<AuditFailure> failures = failureCollector.GetFailures();

          AssertRecoverableFailure(config, failures, "/Broken", "Broken page");
        }

        private static IEnumerable<TestCaseData> MalformedPageBuilderJsonCases()
        {
          yield return new TestCaseData(
            """
            {"editableAreas":[{"identifier":"area01","sections":[{"identifier":"8b8e012d-0e1b-48a7-91ce-92e8cc119361","type":"MedioClinic.Section.SingleColumn","properties":null,"zones":[{"identifier":"21cbb3ed-fd0e-4006-b02e-59307a718c91","name":"first","widgets":[{"identifier":"bc415166-f230-49fe-b303-195aaae7795a","type":"MedioClinic.Widget.Text","variants":[{"identifier":"3fe87975-c922-45f8-b7cb-7f17343a1c03","properties":{"text":"<h1>Allergy test center partner program</h1>"}}]}]}]}]}
            """,
            null)
            .SetName("TryParse_RecordsFailure_WhenWidgetsJsonIsTruncated");

          yield return new TestCaseData(
            """
            {"editableAreas":[{"identifier":"area01","sections":[{"identifier":"de14e1eb-1891-401b-b191-b0d0b486e785","type":"MedioClinic.Section.SingleColumn","properties":null,"zones":[{"identifier":"425238ed-f946-4628-8f21-cc66c3c3089f","widgets":[{"identifier":"9d69d1f4-46ff-4034-a31e-58cb5c060935","type":"MedioClinic.Widget.Text","variants":[{"identifier":"f4b6d10f-b2ac-4788-a73b-fbcbea7a8ac2","properties":{"text":"<h1>Nuevo centro m e9dico en Florida</h1>"}}]}]}]}]}]}
            """,
            null)
            .SetName("TryParse_RecordsFailure_WhenWidgetsJsonContainsInvalidEscaping");

          yield return new TestCaseData(
            """
            {"editableAreas":[{"identifier":"area01","sections":[{"identifier":"de14e1eb-1891-401b-b191-b0d0b486e785","type":"MedioClinic.Section.SingleColumn","properties":null,"zones":[{"identifier":"425238ed-f946-4628-8f21-cc66c3c3089f","widgets":[{"identifier":"9d69d1f4-46ff-4034-a31e-58cb5c060935","type":"MedioClinic.Widget.Text","variants":[{"identifier":"f4b6d10f-b2ac-4788-a73b-fbcbea7a8ac2","properties":{"text":"<h1>New medical center in Florida</h1>"}}]}]}]}]}]}
            """,
            """
            {"identifier":"MedioClinic.PageTemplate.Event","properties":{"eventLocationAirport":"MCO"
            """)
            .SetName("TryParse_RecordsFailure_WhenTemplateJsonIsTruncated");
        }

        private static void AssertRecoverableFailure(
          PageBuilderConfiguration? config,
          List<AuditFailure> failures,
          string entityIdentifier,
          string context)
        {
          Assert.Multiple(() =>
          {
            Assert.That(config, Is.Null);
            Assert.That(failures, Has.Count.EqualTo(1));
            Assert.That(failures[0].Category, Is.EqualTo("Page Builder parsing"));
            Assert.That(failures[0].EntityType, Is.EqualTo("Content tree node"));
            Assert.That(failures[0].EntityIdentifier, Is.EqualTo(entityIdentifier));
            Assert.That(failures[0].Context, Is.EqualTo(context));
            Assert.That(failures[0].ErrorMessage, Does.StartWith("JsonReaderException:"));
          });
        }

        private static (AuditFailureCollector FailureCollector, PageBuilderConfigParser Parser) CreateParser()
        {
          var failureCollector = new AuditFailureCollector();
          return (failureCollector, new PageBuilderConfigParser(failureCollector));
        }
    }
}