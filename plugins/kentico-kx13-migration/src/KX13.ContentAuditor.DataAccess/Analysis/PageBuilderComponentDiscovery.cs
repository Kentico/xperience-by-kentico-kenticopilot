using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.DataAccess.Analysis
{
    public class PageBuilderComponentDiscovery
    {
        public List<PageBuilderComponentDefinition> DiscoverComponents(List<ContentTreeNode> allNodes)
        {
            var templates = new ComponentTracker();
            var sections = new ComponentTracker();
            var widgets = new ComponentTracker();

            foreach (var node in allNodes)
            {
                if (node.PageBuilderConfig is null)
                {
                    continue;
                }

                string pageType = node.PageTypeClassName ?? string.Empty;

                if (node.PageBuilderConfig.Template is { Identifier: { } templateId })
                {
                    templates.Track(templateId, pageType);
                    templates.TrackProperties(templateId, node.PageBuilderConfig.Template.Properties);
                }

                foreach (var area in node.PageBuilderConfig.EditableAreas)
                {
                    foreach (var section in area.Sections)
                    {
                        if (section.TypeIdentifier is not null)
                        {
                            sections.Track(section.TypeIdentifier, pageType);
                            sections.TrackProperties(section.TypeIdentifier, section.Properties);
                        }

                        foreach (var widget in section.Zones
                            .SelectMany(z => z.Widgets)
                            .Where(w => w.TypeIdentifier is not null))
                        {
                            widgets.Track(widget.TypeIdentifier!, pageType);
                            widgets.TrackProperties(widget.TypeIdentifier!, widget.Properties);
                        }
                    }
                }
            }

            var result = new List<PageBuilderComponentDefinition>();

            result.AddRange(templates.BuildDefinitions(PageBuilderComponentKind.PageTemplate));
            result.AddRange(sections.BuildDefinitions(PageBuilderComponentKind.Section));
            result.AddRange(widgets.BuildDefinitions(PageBuilderComponentKind.Widget));

            return result;
        }

        /// <summary>
        /// Tracks component usage (page types) and discovered properties for a
        /// single component kind (templates, sections, or widgets).
        /// </summary>
        private class ComponentTracker
        {
            // identifier → page types that use this component
            private readonly Dictionary<string, HashSet<string>> pageTypes = new(StringComparer.OrdinalIgnoreCase);

            // identifier → (property name → inferred type name)
            private readonly Dictionary<string, Dictionary<string, string>> properties = new(StringComparer.OrdinalIgnoreCase);

            public void Track(string identifier, string pageType)
            {
                if (!pageTypes.TryGetValue(identifier, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    pageTypes[identifier] = set;
                }

                if (!string.IsNullOrEmpty(pageType))
                {
                    set.Add(pageType);
                }
            }

            public void TrackProperties(string identifier, Dictionary<string, object?> instanceProperties)
            {
                if (!properties.TryGetValue(identifier, out var propTypes))
                {
                    propTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    properties[identifier] = propTypes;
                }

                foreach (var kvp in instanceProperties)
                {
                    string inferredType = InferTypeName(kvp.Value);

                    // Keep the most specific type on collision (non-null wins over null)
                    if (!propTypes.TryGetValue(kvp.Key, out string? existingType) ||
                        existingType == "null")
                    {
                        propTypes[kvp.Key] = inferredType;
                    }
                }
            }

            public List<PageBuilderComponentDefinition> BuildDefinitions(PageBuilderComponentKind kind)
            {
                return pageTypes.Select(kvp =>
                {
                    var def = new PageBuilderComponentDefinition
                    {
                        Identifier = kvp.Key,
                        DisplayName = kvp.Key,
                        Kind = kind,
                        AllowedForPageTypes = kvp.Value.Order().ToList()
                    };

                    if (properties.TryGetValue(kvp.Key, out var propTypes))
                    {
                        int order = 0;

                        def.PropertyDefinitions = propTypes
                            .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
                            .Select(p => new ComponentPropertyDefinition
                            {
                                PropertyName = p.Key,
                                PropertyTypeName = p.Value,
                                Order = order++
                            })
                            .ToList();
                    }

                    return def;
                }).ToList();
            }

            private static string InferTypeName(object? value) => value switch
            {
                null => "null",
                string => "string",
                bool => "boolean",
                long or int => "number",
                double or float or decimal => "number",
                List<object?> => "array",
                Dictionary<string, object?> => "object",
                _ => value.GetType().Name.ToLowerInvariant()
            };
        }
    }
}
