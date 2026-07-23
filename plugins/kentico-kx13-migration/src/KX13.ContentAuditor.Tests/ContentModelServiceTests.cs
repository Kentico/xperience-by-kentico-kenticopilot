using KX13.ContentAuditor.Application.Services;
using KX13.ContentAuditor.DataAccess;
using KX13.ContentAuditor.DataAccess.Analysis;
using KX13.ContentAuditor.DataAccess.Models;
using KX13.ContentAuditor.DataAccess.Repositories;

using NSubstitute;

namespace KX13.ContentAuditor.Tests
{
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public class ContentModelServiceTests
    {
        private readonly AuditFailureCollector failureCollector = new();
        private readonly ISiteRepository siteRepository = Substitute.For<ISiteRepository>();
        private readonly IPageTypeRepository pageTypeRepository = Substitute.For<IPageTypeRepository>();
        private readonly IContentTreeRepository contentTreeRepository = Substitute.For<IContentTreeRepository>();
        private readonly ICustomTableRepository customTableRepository = Substitute.For<ICustomTableRepository>();
        private readonly IFormRepository formRepository = Substitute.For<IFormRepository>();
        private readonly ICustomModuleRepository customModuleRepository = Substitute.For<ICustomModuleRepository>();
        private readonly IRelationshipRepository relationshipRepository = Substitute.For<IRelationshipRepository>();

        [Test]
        public async Task BuildFullContentModelAsync_PopulatesAllHighValueOutputs()
        {
            PageType articlePageType = BuildArticlePageType();
            var pageTypes = new List<PageType> { articlePageType };
            Site site = BuildSite(1, "MainSite", "en-US");
            Guid sourceGuid = Guid.Parse("10000000-0000-0000-0000-000000000001");
            Guid targetGuid = Guid.Parse("20000000-0000-0000-0000-000000000002");
            var nodes = new List<ContentTreeNode>
            {
                new()
                {
                    NodeId = 10,
                    NodeGuid = sourceGuid,
                    NodeAliasPath = "/Home",
                    PageTypeClassName = articlePageType.ClassName,
                    PageBuilderConfig = BuildPageBuilderConfig(targetGuid)
                },
                new()
                {
                    NodeId = 11,
                    NodeParentId = 10,
                    NodeGuid = targetGuid,
                    NodeAliasPath = "/Articles/Target",
                    PageTypeClassName = articlePageType.ClassName
                }
            };
            var customTables = new List<CustomTable> { new() { ClassId = 50, ClassName = "custom.Contacts" } };
            var customModules = new List<CustomModule> { new() { ResourceId = 60, ResourceName = "CustomModule" } };
            var forms = new List<Form> { new() { FormId = 70, FormName = "ContactUs" } };
            var relationshipNames = new List<RelationshipName>
            {
                new() { RelationshipNameId = 80, Name = "RelatedPages" }
            };
            var relationships = new List<Relationship>
            {
                new() { RelationshipId = 90, RelationshipNameId = 80, RelationshipName = "RelatedPages", LeftClassName = "Demo.Article", Order = 0 }
            };

            ConfigureCommonLookups(pageTypes, new List<Site> { site });
            contentTreeRepository.GetContentTreeAsync(1, Arg.Any<AuditFilterOptions?>(), "en-US")
                .Returns(Task.FromResult(nodes));
            contentTreeRepository.GetCoupledDataForSiteNodesAsync(1, "Demo_Article", "ArticleID", "en-US")
                .Returns(Task.FromResult(new List<(int NodeId, Dictionary<string, object?> FieldValues)>
                {
                    (10, new Dictionary<string, object?>
                    {
                        ["SelectedPage"] = targetGuid.ToString()
                    })
                }));
            customTableRepository.GetAllCustomTablesAsync().Returns(Task.FromResult(customTables));
            customTableRepository.GetCustomTablesForSiteAsync(1).Returns(Task.FromResult(customTables));
            customModuleRepository.GetCustomModulesAsync().Returns(Task.FromResult(customModules));
            formRepository.GetAllFormsAsync().Returns(Task.FromResult(forms));
            formRepository.GetSiteFormsAsync(1).Returns(Task.FromResult(forms));
            relationshipRepository.GetRelationshipNamesAsync().Returns(Task.FromResult(relationshipNames));
            relationshipRepository.GetRelationshipsForSiteAsync(1).Returns(Task.FromResult(relationships));

            ContentModelService service = CreateService();

            KX13ProjectContent model = await service.BuildFullContentModelAsync();

            Assert.Multiple(() =>
            {
                Assert.That(model.Sites, Has.Count.EqualTo(1));
                Assert.That(model.AllPageTypes, Has.Count.EqualTo(1));
                Assert.That(model.AllCustomTables, Has.Count.EqualTo(1));
                Assert.That(model.AllCustomModules, Has.Count.EqualTo(1));
                Assert.That(model.AllForms, Has.Count.EqualTo(1));
                Assert.That(model.PageBuilderComponentCatalogue.Select(component => component.Identifier), Is.SupersetOf(new[] { "Template.Landing", "Section.Main", "Widget.Hero" }));
                Assert.That(model.ContentReferenceGraph.Any(entry => entry.PropertyName == "SelectedPage" && entry.TargetNodeGuid == targetGuid), Is.True);
                Assert.That(model.ContentReferenceGraph.Any(entry => entry.WidgetTypeIdentifier == "Widget.Hero" && entry.TargetNodeGuid == targetGuid), Is.True);
                Assert.That(model.Relationships, Has.Count.EqualTo(1));
                Assert.That(model.RelationshipNames, Has.Count.EqualTo(1));
                Assert.That(model.Failures, Is.Empty);
            });
        }

        [Test]
        public async Task BuildFullContentModelAsync_IncludesCollectedFailuresInModel()
        {
            pageTypeRepository.GetAllPageTypesAsync(Arg.Any<AuditFilterOptions?>()).Returns(Task.FromResult(new List<PageType>()));
            siteRepository.GetSitesAsync(Arg.Any<AuditFilterOptions?>()).Returns(Task.FromResult(new List<Site>()));
            customTableRepository.GetAllCustomTablesAsync().Returns(Task.FromResult(new List<CustomTable>()));
            customModuleRepository.GetCustomModulesAsync().Returns(Task.FromResult(new List<CustomModule>()));
            formRepository.GetAllFormsAsync().Returns(Task.FromResult(new List<Form>()));
            relationshipRepository.GetRelationshipNamesAsync().Returns(Task.FromResult(new List<RelationshipName>()));

            failureCollector.Record(
                "Field definition parsing",
                "Page type",
                "Broken.Article",
                "Broken page type",
                new System.Xml.XmlException("Unexpected end of file."));

            ContentModelService service = CreateService();

            KX13ProjectContent model = await service.BuildFullContentModelAsync();

            Assert.Multiple(() =>
            {
                Assert.That(model.Failures, Has.Count.EqualTo(1));
                Assert.That(model.Failures[0].Category, Is.EqualTo("Field definition parsing"));
                Assert.That(model.Failures[0].EntityIdentifier, Is.EqualTo("Broken.Article"));
                Assert.That(model.Failures[0].Context, Is.EqualTo("Broken page type"));
                Assert.That(model.Failures[0].ErrorMessage, Does.StartWith("XmlException:"));
            });
        }

        [Test]
        public async Task BuildFullContentModelAsync_WithClassFilterRequestsPageTypesTwice()
        {
            var filter = new AuditFilterOptions { ClassNamePattern = "Demo.*" };
            var filteredPageTypes = new List<PageType> { BuildArticlePageType() };
            var unfilteredPageTypes = new List<PageType> { BuildArticlePageType(), new() { ClassName = "CMS.MenuItem" } };

            pageTypeRepository.GetAllPageTypesAsync(filter).Returns(Task.FromResult(filteredPageTypes));
            pageTypeRepository.GetAllPageTypesAsync().Returns(Task.FromResult(unfilteredPageTypes));
            siteRepository.GetSitesAsync(filter).Returns(Task.FromResult(new List<Site>()));
            customTableRepository.GetAllCustomTablesAsync().Returns(Task.FromResult(new List<CustomTable>()));
            customModuleRepository.GetCustomModulesAsync().Returns(Task.FromResult(new List<CustomModule>()));
            formRepository.GetAllFormsAsync().Returns(Task.FromResult(new List<Form>()));
            relationshipRepository.GetRelationshipNamesAsync().Returns(Task.FromResult(new List<RelationshipName>()));

            pageTypeRepository.ClearReceivedCalls();

            ContentModelService service = CreateService();

            await service.BuildFullContentModelAsync(filter);

            List<AuditFilterOptions?> pageTypeCalls = pageTypeRepository.ReceivedCalls()
                .Where(call => call.GetMethodInfo().Name == nameof(IPageTypeRepository.GetAllPageTypesAsync))
                .Select(call => call.GetArguments().SingleOrDefault() as AuditFilterOptions)
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(pageTypeCalls, Has.Count.EqualTo(2));
                Assert.That(pageTypeCalls.Count(arg => ReferenceEquals(arg, filter)), Is.EqualTo(1));
                Assert.That(pageTypeCalls.Count(arg => arg is null), Is.EqualTo(1));
            });
        }

        [Test]
        public async Task BuildSitesAsync_LinksNodesLoadsCoupledDataAndComputesFieldReferences()
        {
            PageType articlePageType = BuildArticlePageType();
            Site site = BuildSite(1, "MainSite", "en-US");
            var pageTypes = new List<PageType> { articlePageType };
            Guid originalGuid = Guid.Parse("30000000-0000-0000-0000-000000000003");
            var nodes = new List<ContentTreeNode>
            {
                new()
                {
                    NodeId = 1,
                    NodeGuid = originalGuid,
                    NodeAliasPath = "/Original",
                    PageTypeClassName = articlePageType.ClassName
                },
                new()
                {
                    NodeId = 2,
                    NodeParentId = 1,
                    NodeGuid = Guid.Parse("40000000-0000-0000-0000-000000000004"),
                    NodeAliasPath = "/Linked/InTree",
                    NodeLinkedNodeId = 1,
                    PageTypeClassName = articlePageType.ClassName
                },
                new()
                {
                    NodeId = 3,
                    NodeParentId = 1,
                    NodeGuid = Guid.Parse("50000000-0000-0000-0000-000000000005"),
                    NodeAliasPath = "/Linked/External",
                    NodeLinkedNodeId = 99,
                    PageTypeClassName = articlePageType.ClassName
                }
            };

            ConfigureCommonLookups(pageTypes, new List<Site> { site });
            contentTreeRepository.GetContentTreeAsync(1, Arg.Any<AuditFilterOptions?>(), "en-US")
                .Returns(Task.FromResult(nodes));
            contentTreeRepository.GetNodesByIdsAsync(Arg.Any<IEnumerable<int>>())
                .Returns(Task.FromResult(new List<ContentTreeNode>
                {
                    new()
                    {
                        NodeId = 99,
                        NodeGuid = Guid.Parse("60000000-0000-0000-0000-000000000006"),
                        NodeAliasPath = "/Original/External",
                        PageTypeClassName = articlePageType.ClassName
                    }
                }));
            contentTreeRepository.GetCoupledDataForSiteNodesAsync(1, "Demo_Article", "ArticleID", "en-US")
                .Returns(Task.FromResult(new List<(int NodeId, Dictionary<string, object?> FieldValues)>
                {
                    (2, new Dictionary<string, object?> { ["SelectedPage"] = originalGuid.ToString() }),
                    (3, new Dictionary<string, object?> { ["SelectedPage"] = originalGuid.ToString() })
                }));
            customTableRepository.GetCustomTablesForSiteAsync(1).Returns(Task.FromResult(new List<CustomTable> { new() { ClassName = "custom.Contacts" } }));
            formRepository.GetSiteFormsAsync(1).Returns(Task.FromResult(new List<Form> { new() { FormName = "ContactUs" } }));

            ContentModelService service = CreateService();

            List<Site> sites = await service.BuildSitesAsync();
            List<ContentTreeNode> flattened = Flatten(sites[0].ContentTree).ToList();
            ContentTreeNode linkedInTree = flattened.Single(node => node.NodeId == 2);
            ContentTreeNode linkedExternal = flattened.Single(node => node.NodeId == 3);

            Assert.Multiple(() =>
            {
                Assert.That(sites[0].AssignedPageTypes, Has.Count.EqualTo(1));
                Assert.That(sites[0].AssignedCustomTables, Has.Count.EqualTo(1));
                Assert.That(sites[0].Forms, Has.Count.EqualTo(1));
                Assert.That(flattened.All(node => node.PageType == articlePageType), Is.True);
                Assert.That(linkedInTree.LinkedOriginalNodeAliasPath, Is.EqualTo("/Original"));
                Assert.That(linkedInTree.LinkedOriginalClassName, Is.EqualTo("Demo.Article"));
                Assert.That(linkedExternal.LinkedOriginalNodeAliasPath, Is.EqualTo("/Original/External"));
                Assert.That(linkedExternal.LinkedOriginalClassName, Is.EqualTo("Demo.Article"));
                Assert.That(linkedInTree.CustomFieldValues["SelectedPage"], Is.EqualTo(originalGuid.ToString()));
                Assert.That(linkedInTree.PageTypeFieldReferences.Single().ReferencedNodeGuid, Is.EqualTo(originalGuid));
                Assert.That(linkedExternal.PageTypeFieldReferences.Single().ReferencedNodeGuid, Is.EqualTo(originalGuid));
            });
        }

        [Test]
        public async Task BuildRelationshipsAsync_SortsRelationshipsAndResolvesUniqueAdHocNames()
        {
            Guid fieldGuid = Guid.Parse("70000000-0000-0000-0000-000000000007");
            var pageTypes = new List<PageType>
            {
                new()
                {
                    ClassName = "Demo.Article",
                    Fields = new List<FieldDefinition>
                    {
                        new() { FieldGuid = fieldGuid, FieldName = "RelatedArticles" }
                    }
                }
            };
            var sites = new List<Site>
            {
                BuildSite(2, "BetaSite", "en-US"),
                BuildSite(1, "AlphaSite", "en-US")
            };
            string adHocName = $"custom_{fieldGuid}";

            siteRepository.GetSitesAsync(Arg.Any<AuditFilterOptions?>()).Returns(Task.FromResult(sites));
            pageTypeRepository.GetAllPageTypesAsync().Returns(Task.FromResult(pageTypes));
            relationshipRepository.GetRelationshipNamesAsync().Returns(Task.FromResult(new List<RelationshipName>
            {
                new() { RelationshipNameId = 10, Name = adHocName },
                new() { RelationshipNameId = 20, Name = "NamedRelation" },
                new() { RelationshipNameId = 30, Name = "Unused" }
            }));
            relationshipRepository.GetRelationshipsForSiteAsync(2).Returns(Task.FromResult(new List<Relationship>
            {
                new() { RelationshipId = 2, RelationshipNameId = 20, RelationshipName = "NamedRelation", LeftClassName = "Demo.Banner", Order = 2 }
            }));
            relationshipRepository.GetRelationshipsForSiteAsync(1).Returns(Task.FromResult(new List<Relationship>
            {
                new() { RelationshipId = 1, RelationshipNameId = 20, RelationshipName = "NamedRelation", LeftClassName = "Demo.Article", Order = 3 },
                new() { RelationshipId = 3, RelationshipNameId = 10, RelationshipName = adHocName, LeftClassName = "Demo.Article", IsAdHoc = true, Order = 1 }
            }));

            ContentModelService service = CreateService();

            KX13ProjectContent model = await service.BuildRelationshipsAsync();
            RelationshipName resolvedAdHoc = model.RelationshipNames.Single(name => name.RelationshipNameId == 10);

            Assert.Multiple(() =>
            {
                Assert.That(model.Relationships.Select(relationship => relationship.RelationshipId), Is.EqualTo(new[] { 3, 1, 2 }));
                Assert.That(model.Relationships[0].SiteId, Is.EqualTo(1));
                Assert.That(model.Relationships[0].SiteName, Is.EqualTo("AlphaSite"));
                Assert.That(model.Relationships[2].SiteId, Is.EqualTo(2));
                Assert.That(model.Relationships[2].SiteName, Is.EqualTo("BetaSite"));
                Assert.That(model.RelationshipNames.Select(name => name.RelationshipNameId), Is.EqualTo(new[] { 10, 20 }));
                Assert.That(resolvedAdHoc.IsAdHoc, Is.True);
                Assert.That(resolvedAdHoc.SourceFieldGuid, Is.EqualTo(fieldGuid));
                Assert.That(resolvedAdHoc.SourcePageTypeClassName, Is.EqualTo("Demo.Article"));
                Assert.That(resolvedAdHoc.SourceFieldName, Is.EqualTo("RelatedArticles"));
            });
        }

        [Test]
        public async Task BuildRelationshipsAsync_LeavesAmbiguousOrUnparseableAdHocNamesUnresolved()
        {
            Guid sharedGuid = Guid.Parse("80000000-0000-0000-0000-000000000008");
            var pageTypes = new List<PageType>
            {
                new()
                {
                    ClassName = "Demo.Article",
                    Fields = new List<FieldDefinition> { new() { FieldGuid = sharedGuid, FieldName = "PrimaryLink" } }
                },
                new()
                {
                    ClassName = "Demo.News",
                    Fields = new List<FieldDefinition> { new() { FieldGuid = sharedGuid, FieldName = "SecondaryLink" } }
                }
            };
            string ambiguousName = $"ambiguous_{sharedGuid}";

            siteRepository.GetSitesAsync(Arg.Any<AuditFilterOptions?>()).Returns(Task.FromResult(new List<Site> { BuildSite(1, "MainSite", "en-US") }));
            pageTypeRepository.GetAllPageTypesAsync().Returns(Task.FromResult(pageTypes));
            relationshipRepository.GetRelationshipNamesAsync().Returns(Task.FromResult(new List<RelationshipName>
            {
                new() { RelationshipNameId = 10, Name = ambiguousName },
                new() { RelationshipNameId = 20, Name = "not-a-guid" }
            }));
            relationshipRepository.GetRelationshipsForSiteAsync(1).Returns(Task.FromResult(new List<Relationship>
            {
                new() { RelationshipId = 1, RelationshipNameId = 10, RelationshipName = ambiguousName, LeftClassName = "Demo.Article", IsAdHoc = true },
                new() { RelationshipId = 2, RelationshipNameId = 20, RelationshipName = "not-a-guid", LeftClassName = "Demo.Article", IsAdHoc = true }
            }));

            ContentModelService service = CreateService();

            KX13ProjectContent model = await service.BuildRelationshipsAsync();

            Assert.Multiple(() =>
            {
                Assert.That(model.RelationshipNames.Single(name => name.RelationshipNameId == 10).SourcePageTypeClassName, Is.Null);
                Assert.That(model.RelationshipNames.Single(name => name.RelationshipNameId == 10).SourceFieldName, Is.Null);
                Assert.That(model.RelationshipNames.Single(name => name.RelationshipNameId == 20).SourceFieldGuid, Is.Null);
            });
        }

        private ContentModelService CreateService() => new(
            failureCollector,
            siteRepository,
            pageTypeRepository,
            contentTreeRepository,
            customTableRepository,
            formRepository,
            customModuleRepository,
            relationshipRepository,
            new PageBuilderComponentDiscovery(),
            new ContentReferenceAnalyzer());

        private void ConfigureCommonLookups(List<PageType> pageTypes, List<Site> sites)
        {
            pageTypeRepository.GetAllPageTypesAsync(Arg.Any<AuditFilterOptions?>()).Returns(Task.FromResult(pageTypes));
            pageTypeRepository.GetPageTypesForSiteAsync(1).Returns(Task.FromResult(pageTypes));
            siteRepository.GetSitesAsync(Arg.Any<AuditFilterOptions?>()).Returns(Task.FromResult(sites));
            siteRepository.GetSiteCulturesAsync(1).Returns(Task.FromResult(new List<string> { "en-US" }));
            relationshipRepository.GetRelationshipNamesAsync().Returns(Task.FromResult(new List<RelationshipName>()));
            relationshipRepository.GetRelationshipsForSiteAsync(1).Returns(Task.FromResult(new List<Relationship>()));
            customTableRepository.GetAllCustomTablesAsync().Returns(Task.FromResult(new List<CustomTable>()));
            customModuleRepository.GetCustomModulesAsync().Returns(Task.FromResult(new List<CustomModule>()));
            formRepository.GetAllFormsAsync().Returns(Task.FromResult(new List<Form>()));
            customTableRepository.GetCustomTablesForSiteAsync(1).Returns(Task.FromResult(new List<CustomTable>()));
            formRepository.GetSiteFormsAsync(1).Returns(Task.FromResult(new List<Form>()));
            contentTreeRepository.GetNodesByIdsAsync(Arg.Any<IEnumerable<int>>()).Returns(Task.FromResult(new List<ContentTreeNode>()));
        }

        private static PageType BuildArticlePageType() => new()
        {
            ClassId = 1,
            ClassName = "Demo.Article",
            ClassDisplayName = "Article",
            ClassTableName = "Demo_Article",
            Fields = new List<FieldDefinition>
            {
                new()
                {
                    FieldName = "SelectedPage",
                    FormComponentIdentifier = "Kentico.PageSelector"
                }
            }
        };

        private static Site BuildSite(int siteId, string siteName, string culture) => new()
        {
            SiteId = siteId,
            SiteName = siteName,
            SiteDefaultCultureCode = culture
        };

        private static PageBuilderConfiguration BuildPageBuilderConfig(Guid targetGuid) => new()
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
                            TypeIdentifier = "Section.Main",
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
                                                ["SelectedPages"] = new List<object?>
                                                {
                                                    new Dictionary<string, object?>
                                                    {
                                                        ["nodeGuid"] = targetGuid.ToString()
                                                    }
                                                }
                                            }
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        private static IEnumerable<ContentTreeNode> Flatten(IEnumerable<ContentTreeNode> roots)
        {
            foreach (ContentTreeNode root in roots)
            {
                yield return root;

                foreach (ContentTreeNode child in Flatten(root.Children))
                {
                    yield return child;
                }
            }
        }
    }
}