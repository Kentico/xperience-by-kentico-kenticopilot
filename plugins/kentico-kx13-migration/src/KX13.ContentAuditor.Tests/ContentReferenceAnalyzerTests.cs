using KX13.ContentAuditor.DataAccess.Analysis;
using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.Tests
{
    public class ContentReferenceAnalyzerTests
    {
        private readonly ContentReferenceAnalyzer analyzer = new();

        [Test]
        public void AnalyzeFieldReferences_DetectsSupportedReferenceTypesFromJsonAndFallbackValues()
        {
            Guid pageGuidFromJson = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            Guid pageGuidFromPlainText = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            Guid mediaGuid = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

            var fields = new List<FieldDefinition>
            {
                new() { FieldName = "SelectedPageJson", FormComponentIdentifier = "Kentico.PageSelector" },
                new() { FieldName = "SelectedPagePlain", FormComponentIdentifier = "PageSelector" },
                new() { FieldName = "SelectedPathJson", FormComponentIdentifier = "Kentico.PathSelector" },
                new() { FieldName = "SelectedPathPlain", FormComponentIdentifier = "PathSelector" },
                new() { FieldName = "MediaAsset", FormControlName = "MediaSelectionControl" },
                new() { FieldName = "Author", FormComponentIdentifier = "Kentico.ObjectSelector" },
                new() { FieldName = "RelatedType", ReferenceToObjectType = "cms.document" }
            };
            var fieldValues = new Dictionary<string, object?>
            {
                ["SelectedPageJson"] = $"[{{\"nodeGuid\":\"{pageGuidFromJson}\"}}]",
                ["SelectedPagePlain"] = pageGuidFromPlainText.ToString(),
                ["SelectedPathJson"] = "[{\"nodeAliasPath\":\"/Articles/First\"}]",
                ["SelectedPathPlain"] = "/Articles/Second",
                ["MediaAsset"] = $"~/getmedia/{mediaGuid}/hero.jpg",
                ["Author"] = "administrator",
                ["RelatedType"] = "Demo.Article"
            };

            List<ContentReference> references = analyzer.AnalyzeFieldReferences(fields, fieldValues);

            Assert.That(references, Has.Count.EqualTo(7));
            Assert.Multiple(() =>
            {
                Assert.That(references.Single(r => r.SourcePropertyName == "SelectedPageJson").ReferencedNodeGuid, Is.EqualTo(pageGuidFromJson));
                Assert.That(references.Single(r => r.SourcePropertyName == "SelectedPagePlain").ReferencedNodeGuid, Is.EqualTo(pageGuidFromPlainText));
                Assert.That(references.Single(r => r.SourcePropertyName == "SelectedPathJson").ReferencedNodeAliasPath, Is.EqualTo("/Articles/First"));
                Assert.That(references.Single(r => r.SourcePropertyName == "SelectedPathPlain").ReferencedNodeAliasPath, Is.EqualTo("/Articles/Second"));
                Assert.That(references.Single(r => r.SourcePropertyName == "MediaAsset").ReferencedMediaFileGuid, Is.EqualTo(mediaGuid));
                Assert.That(references.Single(r => r.SourcePropertyName == "Author").ReferencedObjectCodeName, Is.EqualTo("administrator"));
                Assert.That(references.Single(r => r.SourcePropertyName == "RelatedType").ReferenceType, Is.EqualTo(ContentReferenceType.PageTypeField));
            });
        }

        [Test]
        public void BuildReferenceGraph_ResolvesTargetsAndStoresWidgetReferences()
        {
            Guid sourceGuid = Guid.Parse("11111111-aaaa-aaaa-aaaa-111111111111");
            Guid targetGuid = Guid.Parse("22222222-bbbb-bbbb-bbbb-222222222222");
            Guid mediaGuid = Guid.Parse("33333333-cccc-cccc-cccc-333333333333");

            var widget = new WidgetConfig
            {
                TypeIdentifier = "Widget.Hero",
                Properties = new Dictionary<string, object?>
                {
                    ["SelectedPages"] = new List<object?>
                    {
                        new Dictionary<string, object?> { ["nodeGuid"] = targetGuid.ToString() }
                    },
                    ["SelectedPaths"] = new List<object?>
                    {
                        new Dictionary<string, object?> { ["nodeAliasPath"] = "/Articles/Target" }
                    },
                    ["MediaItems"] = new List<object?>
                    {
                        new Dictionary<string, object?> { ["fileGuid"] = mediaGuid.ToString() }
                    },
                    ["Authors"] = new List<object?>
                    {
                        new Dictionary<string, object?> { ["objectCodeName"] = "administrator" }
                    }
                }
            };

            var sourceNode = new ContentTreeNode
            {
                NodeId = 10,
                NodeGuid = sourceGuid,
                NodeAliasPath = "/Home",
                PageTypeClassName = "Demo.Home",
                PageTypeFieldReferences = new List<ContentReference>
                {
                    new()
                    {
                        SourcePropertyName = "RelatedPage",
                        ReferenceType = ContentReferenceType.PageSelector,
                        ReferencedNodeGuid = targetGuid
                    },
                    new()
                    {
                        SourcePropertyName = "RelatedPath",
                        ReferenceType = ContentReferenceType.PathSelector,
                        ReferencedNodeAliasPath = "/Articles/Target"
                    }
                },
                PageBuilderConfig = new PageBuilderConfiguration
                {
                    EditableAreas =
                    [
                        new EditableAreaConfig
                        {
                            Identifier = "main",
                            Sections =
                            [
                                new SectionConfig
                                {
                                    TypeIdentifier = "Section.Single",
                                    Zones =
                                    [
                                        new WidgetZoneConfig
                                        {
                                            Identifier = "zone-1",
                                            Widgets = [widget]
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            };
            var targetNode = new ContentTreeNode
            {
                NodeId = 20,
                NodeGuid = targetGuid,
                NodeAliasPath = "/Articles/Target",
                PageTypeClassName = "Demo.Article"
            };

            List<PageContentReferenceEntry> graph = analyzer.BuildReferenceGraph([sourceNode, targetNode]);

            Assert.That(widget.ContentReferences, Has.Count.EqualTo(4));

            PageContentReferenceEntry fieldEntry = graph.Single(entry => entry.PropertyName == "RelatedPage");
            PageContentReferenceEntry widgetPageEntry = graph.Single(entry => entry.WidgetTypeIdentifier == "Widget.Hero" && entry.PropertyName == "SelectedPages");
            PageContentReferenceEntry widgetPathEntry = graph.Single(entry => entry.WidgetTypeIdentifier == "Widget.Hero" && entry.PropertyName == "SelectedPaths");

            Assert.Multiple(() =>
            {
                Assert.That(fieldEntry.TargetNodeGuid, Is.EqualTo(targetGuid));
                Assert.That(fieldEntry.TargetPageTypeClassName, Is.EqualTo("Demo.Article"));
                Assert.That(widgetPageEntry.TargetNodeGuid, Is.EqualTo(targetGuid));
                Assert.That(widgetPageEntry.TargetPageTypeClassName, Is.EqualTo("Demo.Article"));
                Assert.That(widgetPathEntry.TargetNodeAliasPath, Is.EqualTo("/Articles/Target"));
                Assert.That(widgetPathEntry.TargetNodeGuid, Is.EqualTo(targetGuid));
                Assert.That(graph.Single(entry => entry.PropertyName == "MediaItems").TargetMediaFileGuid, Is.EqualTo(mediaGuid));
            });
        }
    }
}