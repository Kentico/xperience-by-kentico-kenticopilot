using System.Text.Json;

using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.DataAccess.Analysis
{
    public class ContentReferenceAnalyzer
    {
        private static readonly HashSet<string> PageSelectorNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Kentico.PageSelector", "pageselector", "PageSelector"
        };

        private static readonly HashSet<string> PathSelectorNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Kentico.PathSelector", "pathselector", "PathSelector"
        };

        private static readonly HashSet<string> MediaSelectorNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Kentico.MediaFilesSelector", "Kentico.MediaSelector",
            "mediafilesselector", "MediaFilesSelector",
            // Legacy form controls (KX12/KX11 upgrades)
            "MediaSelectionControl", "AttachmentSelector"
        };

        private static readonly HashSet<string> ObjectSelectorNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Kentico.ObjectSelector", "objectselector", "ObjectSelector"
        };

        public List<ContentReference> AnalyzeFieldReferences(
            List<FieldDefinition> fields,
            Dictionary<string, object?> customFieldValues)
        {
            var references = new List<ContentReference>();

            foreach (var field in fields)
            {
                var refType = DetectReferenceType(field);

                if (refType is null || string.IsNullOrWhiteSpace(field.FieldName))
                {
                    continue;
                }

                customFieldValues.TryGetValue(field.FieldName, out object? fieldValue);

                var fieldRefs = ExtractReferencesFromValue(field.FieldName, refType.Value, fieldValue);
                references.AddRange(fieldRefs);
            }

            return references;
        }

        public List<ContentReference> AnalyzeWidgetReferences(WidgetConfig widget)
        {
            var references = new List<ContentReference>();

            foreach (var kvp in widget.Properties)
            {
                var extracted = ExtractReferencesFromWidgetProperty(kvp.Key, kvp.Value);
                references.AddRange(extracted);
            }

            return references;
        }

        public List<PageContentReferenceEntry> BuildReferenceGraph(List<ContentTreeNode> allNodes)
        {
            // Build lookups for target resolution
            var nodeGuidToClassName = new Dictionary<Guid, string?>();
            var pathToNode = new Dictionary<string, (Guid NodeGuid, string? ClassName)>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in allNodes)
            {
                nodeGuidToClassName.TryAdd(node.NodeGuid, node.PageTypeClassName);

                if (node.NodeAliasPath is not null)
                {
                    pathToNode.TryAdd(node.NodeAliasPath, (node.NodeGuid, node.PageTypeClassName));
                }
            }

            var entries = new List<PageContentReferenceEntry>();

            foreach (var node in allNodes)
            {
                // References from page type fields
                foreach (var reference in node.PageTypeFieldReferences)
                {
                    ResolveTarget(reference, nodeGuidToClassName, pathToNode);

                    entries.Add(new PageContentReferenceEntry
                    {
                        SourceNodeGuid = node.NodeGuid,
                        SourceNodeAliasPath = node.NodeAliasPath,
                        SourcePageTypeClassName = node.PageTypeClassName,
                        PropertyName = reference.SourcePropertyName,
                        ReferenceType = reference.ReferenceType,
                        TargetNodeGuid = reference.ReferencedNodeGuid,
                        TargetNodeAliasPath = reference.ReferencedNodeAliasPath,
                        TargetMediaFileGuid = reference.ReferencedMediaFileGuid,
                        TargetPageTypeClassName = reference.ReferencedPageTypeClassName
                    });
                }

                // References from page builder widgets
                if (node.PageBuilderConfig is null)
                {
                    continue;
                }

                foreach (var area in node.PageBuilderConfig.EditableAreas)
                {
                    foreach (var section in area.Sections)
                    {
                        foreach (var zone in section.Zones)
                        {
                            foreach (var widget in zone.Widgets)
                            {
                                var widgetRefs = AnalyzeWidgetReferences(widget);
                                widget.ContentReferences = widgetRefs;

                                foreach (var reference in widgetRefs)
                                {
                                    ResolveTarget(reference, nodeGuidToClassName, pathToNode);

                                    entries.Add(new PageContentReferenceEntry
                                    {
                                        SourceNodeGuid = node.NodeGuid,
                                        SourceNodeAliasPath = node.NodeAliasPath,
                                        SourcePageTypeClassName = node.PageTypeClassName,
                                        WidgetTypeIdentifier = widget.TypeIdentifier,
                                        PropertyName = reference.SourcePropertyName,
                                        ReferenceType = reference.ReferenceType,
                                        TargetNodeGuid = reference.ReferencedNodeGuid,
                                        TargetNodeAliasPath = reference.ReferencedNodeAliasPath,
                                        TargetMediaFileGuid = reference.ReferencedMediaFileGuid,
                                        TargetPageTypeClassName = reference.ReferencedPageTypeClassName
                                    });
                                }
                            }
                        }
                    }
                }
            }

            return entries;
        }

        /// <summary>
        /// Resolves target page type class name from the reference's GUID or path
        /// using the content tree node lookups.
        /// </summary>
        private static void ResolveTarget(
            ContentReference reference,
            Dictionary<Guid, string?> nodeGuidToClassName,
            Dictionary<string, (Guid NodeGuid, string? ClassName)> pathToNode)
        {
            // Resolve by GUID (PageSelector)
            if (reference.ReferencedNodeGuid.HasValue &&
                nodeGuidToClassName.TryGetValue(reference.ReferencedNodeGuid.Value, out string? classNameByGuid))
            {
                reference.ReferencedPageTypeClassName = classNameByGuid;
            }

            // Resolve by path (PathSelector) — also fill in the GUID and class name
            if (reference.ReferencedNodeAliasPath is not null &&
                pathToNode.TryGetValue(reference.ReferencedNodeAliasPath, out var targetByPath))
            {
                reference.ReferencedNodeGuid ??= targetByPath.NodeGuid;
                reference.ReferencedPageTypeClassName ??= targetByPath.ClassName;
            }
        }

        private static ContentReferenceType? DetectReferenceType(FieldDefinition field)
        {
            string? identifier = field.FormComponentIdentifier ?? field.FormControlName;

            if (string.IsNullOrEmpty(identifier))
            {
                return field.ReferenceToObjectType is not null ? ContentReferenceType.PageTypeField : null;
            }

            if (PageSelectorNames.Contains(identifier))
            {
                return ContentReferenceType.PageSelector;
            }

            if (PathSelectorNames.Contains(identifier))
            {
                return ContentReferenceType.PathSelector;
            }

            if (MediaSelectorNames.Contains(identifier))
            {
                return ContentReferenceType.MediaFilesSelector;
            }

            if (ObjectSelectorNames.Contains(identifier))
            {
                return ContentReferenceType.ObjectSelector;
            }

            if (field.ReferenceToObjectType is not null)
            {
                return ContentReferenceType.PageTypeField;
            }

            return null;
        }

        private static List<ContentReference> ExtractReferencesFromValue(
            string propertyName,
            ContentReferenceType refType,
            object? value)
        {
            var references = new List<ContentReference>();

            if (value is null)
            {
                return references;
            }

            string valueStr = value.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(valueStr))
            {
                return references;
            }

            // Try structured JSON parsing first (MVC form component values store JSON arrays)
            var jsonItems = TryParseJsonSelectorArray(valueStr);

            switch (refType)
            {
                case ContentReferenceType.PageSelector:
                    if (jsonItems is not null)
                    {
                        // MVC format: [{"nodeGuid":"..."}]
                        foreach (var item in jsonItems)
                        {
                            if (item.TryGetValue("nodeGuid", out string? nodeGuidStr) &&
                                Guid.TryParse(nodeGuidStr, out var nodeGuid))
                            {
                                references.Add(new ContentReference
                                {
                                    SourcePropertyName = propertyName,
                                    ReferenceType = ContentReferenceType.PageSelector,
                                    ReferencedNodeGuid = nodeGuid
                                });
                            }
                        }
                    }
                    else
                    {
                        // Fallback: plain GUID string or GUIDs embedded in text
                        foreach (var guid in ExtractGuids(valueStr))
                        {
                            references.Add(new ContentReference
                            {
                                SourcePropertyName = propertyName,
                                ReferenceType = ContentReferenceType.PageSelector,
                                ReferencedNodeGuid = guid
                            });
                        }
                    }

                    break;

                case ContentReferenceType.PathSelector:
                    if (jsonItems is not null)
                    {
                        // MVC format: [{"nodeAliasPath":"/path/to/page"}]
                        foreach (var item in jsonItems)
                        {
                            if (item.TryGetValue("nodeAliasPath", out string? path) &&
                                !string.IsNullOrWhiteSpace(path))
                            {
                                references.Add(new ContentReference
                                {
                                    SourcePropertyName = propertyName,
                                    ReferenceType = ContentReferenceType.PathSelector,
                                    ReferencedNodeAliasPath = path
                                });
                            }
                        }
                    }
                    else
                    {
                        // Fallback: plain path string
                        references.Add(new ContentReference
                        {
                            SourcePropertyName = propertyName,
                            ReferenceType = ContentReferenceType.PathSelector,
                            ReferencedNodeAliasPath = valueStr
                        });
                    }

                    break;

                case ContentReferenceType.MediaFilesSelector:
                    if (jsonItems is not null)
                    {
                        // MVC format: [{"fileGuid":"..."}]
                        foreach (var item in jsonItems)
                        {
                            if (item.TryGetValue("fileGuid", out string? fileGuidStr) &&
                                Guid.TryParse(fileGuidStr, out var fileGuid))
                            {
                                references.Add(new ContentReference
                                {
                                    SourcePropertyName = propertyName,
                                    ReferenceType = ContentReferenceType.MediaFilesSelector,
                                    ReferencedMediaFileGuid = fileGuid
                                });
                            }
                        }
                    }
                    else
                    {
                        // Fallback: GUID in plain text or ~/getmedia/GUID/... URL
                        foreach (var guid in ExtractGuids(valueStr))
                        {
                            references.Add(new ContentReference
                            {
                                SourcePropertyName = propertyName,
                                ReferenceType = ContentReferenceType.MediaFilesSelector,
                                ReferencedMediaFileGuid = guid
                            });
                        }
                    }

                    break;

                case ContentReferenceType.ObjectSelector:
                case ContentReferenceType.PageTypeField:
                case ContentReferenceType.GeneralSelector:
                    references.Add(new ContentReference
                    {
                        SourcePropertyName = propertyName,
                        ReferenceType = refType,
                        ReferencedObjectCodeName = valueStr
                    });
                    break;
            }

            return references;
        }

        /// <summary>
        /// Attempts to parse a JSON string as an array of objects, extracting string
        /// property values from each item. Returns null if the string is not valid JSON
        /// or is not an array of objects.
        /// </summary>
        private static List<Dictionary<string, string?>>? TryParseJsonSelectorArray(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value[0] != '[')
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(value);

                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                var items = new List<Dictionary<string, string?>>();

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

                    foreach (var prop in element.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.String)
                        {
                            dict[prop.Name] = prop.Value.GetString();
                        }
                    }

                    if (dict.Count > 0)
                    {
                        items.Add(dict);
                    }
                }

                return items.Count > 0 ? items : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static List<ContentReference> ExtractReferencesFromWidgetProperty(string propertyName, object? value)
        {
            var references = new List<ContentReference>();

            if (value is null)
            {
                return references;
            }

            // Widget properties that are lists of objects (e.g., page selectors store [{nodeGuid: "..."}])
            if (value is List<object?> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object?> dict)
                    {
                        // PageSelector — KX13 PageSelectorItem uses "nodeGuid"
                        if (dict.TryGetValue("nodeGuid", out object? nodeGuidObj) &&
                            nodeGuidObj is string nodeGuidStr &&
                            Guid.TryParse(nodeGuidStr, out var nodeGuid))
                        {
                            references.Add(new ContentReference
                            {
                                SourcePropertyName = propertyName,
                                ReferenceType = ContentReferenceType.PageSelector,
                                ReferencedNodeGuid = nodeGuid
                            });
                        }

                        // MediaFilesSelector — KX13 MediaFilesSelectorItem uses "fileGuid"
                        if (dict.TryGetValue("fileGuid", out object? mediaGuidObj) &&
                            mediaGuidObj is string mediaGuidStr &&
                            Guid.TryParse(mediaGuidStr, out var mediaGuid))
                        {
                            references.Add(new ContentReference
                            {
                                SourcePropertyName = propertyName,
                                ReferenceType = ContentReferenceType.MediaFilesSelector,
                                ReferencedMediaFileGuid = mediaGuid
                            });
                        }

                        // PathSelector — KX13 PathSelectorItem uses "nodeAliasPath"
                        if (dict.TryGetValue("nodeAliasPath", out object? pathObj) &&
                            pathObj is string path &&
                            !string.IsNullOrWhiteSpace(path))
                        {
                            references.Add(new ContentReference
                            {
                                SourcePropertyName = propertyName,
                                ReferenceType = ContentReferenceType.PathSelector,
                                ReferencedNodeAliasPath = path
                            });
                        }

                        // ObjectSelector — stores code name identifiers
                        if (dict.TryGetValue("objectCodeName", out object? codeNameObj) &&
                            codeNameObj is string codeName &&
                            !string.IsNullOrWhiteSpace(codeName))
                        {
                            references.Add(new ContentReference
                            {
                                SourcePropertyName = propertyName,
                                ReferenceType = ContentReferenceType.ObjectSelector,
                                ReferencedObjectCodeName = codeName
                            });
                        }
                    }
                }
            }

            return references;
        }

        private static List<Guid> ExtractGuids(string value)
        {
            var guids = new List<Guid>();

            // Try parsing as a single GUID
            if (Guid.TryParse(value, out var singleGuid))
            {
                guids.Add(singleGuid);
                return guids;
            }

            // Try parsing as JSON array of GUIDs or objects with nodeGuid/fileGuid
            // Simple approach: find all GUID-shaped substrings
            int index = 0;

            while (index < value.Length)
            {
                // Look for GUID pattern (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)
                int dashPos = value.IndexOf('-', index);

                if (dashPos < 8 || dashPos == -1)
                {
                    break;
                }

                int start = dashPos - 8;

                if (start >= 0 && start + 36 <= value.Length)
                {
                    string candidate = value.Substring(start, 36);

                    if (Guid.TryParse(candidate, out var guid))
                    {
                        guids.Add(guid);
                        index = start + 36;
                        continue;
                    }
                }

                index = dashPos + 1;
            }

            return guids;
        }
    }
}
