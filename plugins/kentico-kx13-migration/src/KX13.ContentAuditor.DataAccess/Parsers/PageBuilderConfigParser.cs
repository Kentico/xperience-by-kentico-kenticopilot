using System.Text.Json;

using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.DataAccess.Parsers
{
    public class PageBuilderConfigParser
    {
        private readonly AuditFailureCollector failureCollector;
        private static readonly JsonDocumentOptions JsonOptions = new()
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        };

        public PageBuilderConfigParser(AuditFailureCollector failureCollector)
        {
            this.failureCollector = failureCollector;
        }

        public PageBuilderConfiguration? TryParsePageBuilderConfiguration(
            string? widgetsJson,
            string? templateJson,
            string entityIdentifier,
            string? context)
        {
            try
            {
                return ParsePageBuilderConfiguration(widgetsJson, templateJson);
            }
            catch (Exception ex)
            {
                failureCollector.Record(
                    "Page Builder parsing",
                    "Content tree node",
                    entityIdentifier,
                    context,
                    ex);

                return null;
            }
        }

        private PageBuilderConfiguration? ParsePageBuilderConfiguration(string? widgetsJson, string? templateJson)
        {
            bool hasWidgets = !string.IsNullOrWhiteSpace(widgetsJson);
            bool hasTemplate = !string.IsNullOrWhiteSpace(templateJson);

            if (!hasWidgets && !hasTemplate)
            {
                return null;
            }

            var config = new PageBuilderConfiguration();

            if (hasWidgets)
            {
                using var doc = JsonDocument.Parse(widgetsJson!, JsonOptions);
                var root = doc.RootElement;

                if (root.TryGetProperty("editableAreas", out var areas))
                {
                    config.EditableAreas = ParseEditableAreas(areas);
                }
            }

            if (hasTemplate)
            {
                using var doc = JsonDocument.Parse(templateJson!, JsonOptions);
                config.Template = ParsePageTemplate(doc.RootElement);
            }

            return config;
        }


        private static List<EditableAreaConfig> ParseEditableAreas(JsonElement areasElement)
        {
            var areas = new List<EditableAreaConfig>();

            foreach (var areaEl in areasElement.EnumerateArray())
            {
                var area = new EditableAreaConfig
                {
                    Identifier = GetString(areaEl, "identifier")
                };

                if (areaEl.TryGetProperty("sections", out var sections))
                {
                    area.Sections = ParseSections(sections);
                }

                areas.Add(area);
            }

            return areas;
        }

        private static List<SectionConfig> ParseSections(JsonElement sectionsElement)
        {
            var sections = new List<SectionConfig>();

            foreach (var secEl in sectionsElement.EnumerateArray())
            {
                var section = new SectionConfig
                {
                    TypeIdentifier = GetString(secEl, "type"),
                    PropertiesTypeName = GetString(secEl, "propertiesType"),
                    Properties = GetDictionary(secEl, "properties")
                };

                if (secEl.TryGetProperty("zones", out var zones))
                {
                    section.Zones = ParseWidgetZones(zones);
                }

                sections.Add(section);
            }

            return sections;
        }

        private static List<WidgetZoneConfig> ParseWidgetZones(JsonElement zonesElement)
        {
            var zones = new List<WidgetZoneConfig>();

            foreach (var zoneEl in zonesElement.EnumerateArray())
            {
                var zone = new WidgetZoneConfig
                {
                    Identifier = GetString(zoneEl, "identifier")
                };

                if (zoneEl.TryGetProperty("widgets", out var widgets))
                {
                    zone.Widgets = ParseWidgets(widgets);
                }

                zones.Add(zone);
            }

            return zones;
        }

        private static List<WidgetConfig> ParseWidgets(JsonElement widgetsElement)
        {
            var widgets = new List<WidgetConfig>();

            foreach (var widgetEl in widgetsElement.EnumerateArray())
            {
                var widget = new WidgetConfig
                {
                    TypeIdentifier = GetString(widgetEl, "type"),
                    PropertiesTypeName = GetString(widgetEl, "propertiesType"),
                    PersonalizationConditionTypeIdentifier = GetString(widgetEl, "conditionType")
                };

                if (widgetEl.TryGetProperty("variants", out var variants))
                {
                    widget.Variants = ParseVariants(variants);
                }

                if (widget.Variants.Count > 0)
                {
                    widget.Properties = widget.Variants[0].Properties;
                }

                widgets.Add(widget);
            }

            return widgets;
        }

        private static List<WidgetVariant> ParseVariants(JsonElement variantsElement)
        {
            var variants = new List<WidgetVariant>();

            foreach (var varEl in variantsElement.EnumerateArray())
            {
                var variant = new WidgetVariant
                {
                    Name = GetString(varEl, "name"),
                    Properties = GetDictionary(varEl, "properties"),
                    ConditionTypeParameters = GetDictionary(varEl, "conditionTypeParameters")
                };

                string? identifierStr = GetString(varEl, "identifier");
                if (Guid.TryParse(identifierStr, out var guid))
                {
                    variant.Identifier = guid;
                }

                variants.Add(variant);
            }

            return variants;
        }

        private static PageTemplateConfig ParsePageTemplate(JsonElement templateElement)
        {
            return new PageTemplateConfig
            {
                Identifier = GetString(templateElement, "identifier"),
                PropertiesTypeName = GetString(templateElement, "propertiesType"),
                Properties = GetDictionary(templateElement, "properties")
            };
        }

        private static string? GetString(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString();
            }

            return null;
        }

        private static Dictionary<string, object?> GetDictionary(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Object)
            {
                return JsonElementToDictionary(prop);
            }

            return new Dictionary<string, object?>();
        }

        private static Dictionary<string, object?> JsonElementToDictionary(JsonElement element)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in element.EnumerateObject())
            {
                dict[property.Name] = ConvertJsonElement(property.Value);
            }

            return dict;
        }

        private static object? ConvertJsonElement(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out long l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => JsonElementToDictionary(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            _ => element.GetRawText()
        };
    }
}
