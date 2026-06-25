using KX13.ContentAuditor.DataAccess.Analysis;
using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.Tests
{
    public class PageBuilderComponentDiscoveryTests
    {
        private readonly PageBuilderComponentDiscovery discovery = new();

        [Test]
        public void DiscoverComponents_MergesRepeatedIdentifiersAndKeepsSpecificPropertyTypes()
        {
            var firstNode = new ContentTreeNode
            {
                PageTypeClassName = "Zoo.Article",
                PageBuilderConfig = new PageBuilderConfiguration
                {
                    Template = new PageTemplateConfig
                    {
                        Identifier = "Template.Landing",
                        Properties = new Dictionary<string, object?>
                        {
                            ["layout"] = "wide"
                        }
                    },
                    EditableAreas =
                    [
                        new EditableAreaConfig
                        {
                            Identifier = "main",
                            Sections =
                            [
                                new SectionConfig
                                {
                                    TypeIdentifier = "Section.TwoColumns",
                                    Properties = new Dictionary<string, object?>
                                    {
                                        ["reverse"] = true
                                    },
                                    Zones =
                                    [
                                        new WidgetZoneConfig
                                        {
                                            Identifier = "zone-1",
                                            Widgets =
                                            [
                                                new WidgetConfig
                                                {
                                                    TypeIdentifier = "Widget.Hero",
                                                    Properties = new Dictionary<string, object?>
                                                    {
                                                        ["title"] = "Hello",
                                                        ["sortOrder"] = 1,
                                                        ["optionalText"] = "present"
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
            };
            var secondNode = new ContentTreeNode
            {
                PageTypeClassName = "Alpha.Article",
                PageBuilderConfig = new PageBuilderConfiguration
                {
                    Template = new PageTemplateConfig
                    {
                        Identifier = "Template.Landing",
                        Properties = new Dictionary<string, object?>
                        {
                            ["layout"] = null
                        }
                    },
                    EditableAreas =
                    [
                        new EditableAreaConfig
                        {
                            Identifier = "main",
                            Sections =
                            [
                                new SectionConfig
                                {
                                    TypeIdentifier = "Section.TwoColumns",
                                    Properties = new Dictionary<string, object?>
                                    {
                                        ["reverse"] = true
                                    },
                                    Zones =
                                    [
                                        new WidgetZoneConfig
                                        {
                                            Identifier = "zone-1",
                                            Widgets =
                                            [
                                                new WidgetConfig
                                                {
                                                    TypeIdentifier = "Widget.Hero",
                                                    Properties = new Dictionary<string, object?>
                                                    {
                                                        ["title"] = "World",
                                                        ["sortOrder"] = 2L,
                                                        ["optionalText"] = null
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
            };

            List<PageBuilderComponentDefinition> components = discovery.DiscoverComponents([firstNode, secondNode]);

            Assert.That(components, Has.Count.EqualTo(3));

            PageBuilderComponentDefinition template = components.Single(component => component.Kind == PageBuilderComponentKind.PageTemplate);
            PageBuilderComponentDefinition section = components.Single(component => component.Kind == PageBuilderComponentKind.Section);
            PageBuilderComponentDefinition widget = components.Single(component => component.Kind == PageBuilderComponentKind.Widget);

            Assert.Multiple(() =>
            {
                Assert.That(template.Identifier, Is.EqualTo("Template.Landing"));
                Assert.That(section.Identifier, Is.EqualTo("Section.TwoColumns"));
                Assert.That(widget.Identifier, Is.EqualTo("Widget.Hero"));
                Assert.That(widget.AllowedForPageTypes, Is.EqualTo(new[] { "Alpha.Article", "Zoo.Article" }));
                Assert.That(template.AllowedForPageTypes, Is.EqualTo(new[] { "Alpha.Article", "Zoo.Article" }));
                Assert.That(widget.PropertyDefinitions.Single(property => property.PropertyName == "title").PropertyTypeName, Is.EqualTo("string"));
                Assert.That(widget.PropertyDefinitions.Single(property => property.PropertyName == "sortOrder").PropertyTypeName, Is.EqualTo("number"));
                Assert.That(widget.PropertyDefinitions.Single(property => property.PropertyName == "optionalText").PropertyTypeName, Is.EqualTo("string"));
                Assert.That(section.PropertyDefinitions.Single(property => property.PropertyName == "reverse").PropertyTypeName, Is.EqualTo("boolean"));
            });
        }
    }
}