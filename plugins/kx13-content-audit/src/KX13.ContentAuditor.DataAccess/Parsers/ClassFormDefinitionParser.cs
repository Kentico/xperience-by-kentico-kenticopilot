using System.Xml.Linq;

using KX13.ContentAuditor.DataAccess.Models;

namespace KX13.ContentAuditor.DataAccess.Parsers
{
    public class ClassFormDefinitionParser
    {
        private readonly AuditFailureCollector failureCollector;

        public ClassFormDefinitionParser(AuditFailureCollector failureCollector)
        {
            this.failureCollector = failureCollector;
        }

        public List<FieldDefinition> TryParseFieldDefinitions(
            string? xml,
            string entityType,
            string entityIdentifier,
            string? context)
        {
            try
            {
                return ParseFieldDefinitions(xml);
            }
            catch (Exception ex)
            {
                failureCollector.Record(
                    "Field definition parsing",
                    entityType,
                    entityIdentifier,
                    context,
                    ex);

                return [];
            }
        }

        public List<FormFieldDefinition> TryParseFormFieldDefinitions(
            string? xml,
            string entityIdentifier,
            string? context)
        {
            try
            {
                return ParseFormFieldDefinitions(xml);
            }
            catch (Exception ex)
            {
                failureCollector.Record(
                    "Form field parsing",
                    "Form",
                    entityIdentifier,
                    context,
                    ex);

                return [];
            }
        }

        private List<FieldDefinition> ParseFieldDefinitions(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return [];
            }

            var doc = XDocument.Parse(xml);
            var fields = new List<FieldDefinition>();
            string? currentCategory = null;
            int order = 0;

            foreach (var element in doc.Root!.Elements())
            {
                if (element.Name.LocalName == "category")
                {
                    currentCategory = (string?)element.Attribute("name");
                    continue;
                }

                if (element.Name.LocalName == "field")
                {
                    var field = ParseField(element, currentCategory, order++);
                    fields.Add(field);
                }
            }

            return fields;
        }

        private List<FormFieldDefinition> ParseFormFieldDefinitions(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return [];
            }

            var doc = XDocument.Parse(xml);
            var fields = new List<FormFieldDefinition>();
            string? currentCategory = null;
            int order = 0;

            foreach (var element in doc.Root!.Elements())
            {
                if (element.Name.LocalName == "category")
                {
                    currentCategory = (string?)element.Attribute("name");
                    continue;
                }

                if (element.Name.LocalName == "field")
                {
                    var field = ParseFormField(element, currentCategory, order++);
                    fields.Add(field);
                }
            }

            return fields;
        }


        private static FieldDefinition ParseField(XElement element, string? category, int order)
        {
            var properties = element.Element("properties");
            var settings = element.Element("settings");

            var field = new FieldDefinition
            {
                FieldGuid = Guid.TryParse(element.Attribute("guid")?.Value, out Guid guid) ? guid : null,
                FieldName = (string?)element.Attribute("column"),
                FieldCaption = (string?)properties?.Element("fieldcaption"),
                DataType = (string?)element.Attribute("columntype"),
                Size = ParseNullableInt(element.Attribute("columnsize")),
                Precision = ParseNullableInt(element.Attribute("columnprecision")),
                IsRequired = ParseBool(properties?.Element("required")) || ParseBool(element.Attribute("required")),
                DefaultValue = (string?)properties?.Element("defaultvalue"),
                IsVisible = ParseBool(element.Attribute("visible"), defaultValue: true),
                IsSystemPageField = ParseBool(element.Attribute("system")),
                FormControlName = (string?)settings?.Element("controlname"),
                FormComponentIdentifier = (string?)settings?.Element("componentidentifier"),
                ReferenceToObjectType = (string?)settings?.Element("ObjectType"),
                ReferenceType = (string?)settings?.Element("ReferenceType"),
                Category = category,
                Order = order
            };

            if (settings is not null)
            {
                var knownSettings = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "controlname", "componentidentifier", "ObjectType", "ReferenceType"
                };

                foreach (var setting in settings.Elements()
                    .Where(s => !knownSettings.Contains(s.Name.LocalName) && !string.IsNullOrEmpty(s.Value)))
                {
                    field.FormControlSettings[setting.Name.LocalName] = setting.Value;
                }
            }

            return field;
        }

        private static FormFieldDefinition ParseFormField(XElement element, string? category, int order)
        {
            var baseField = ParseField(element, category, order);
            var properties = element.Element("properties");
            var settings = element.Element("settings");

            return new FormFieldDefinition
            {
                FieldGuid = baseField.FieldGuid,
                FieldName = baseField.FieldName,
                FieldCaption = baseField.FieldCaption,
                DataType = baseField.DataType,
                Size = baseField.Size,
                Precision = baseField.Precision,
                IsRequired = baseField.IsRequired,
                DefaultValue = baseField.DefaultValue,
                IsVisible = baseField.IsVisible,
                IsSystemPageField = baseField.IsSystemPageField,
                FormControlName = baseField.FormControlName,
                FormComponentIdentifier = baseField.FormComponentIdentifier,
                FormControlSettings = baseField.FormControlSettings,
                ReferenceToObjectType = baseField.ReferenceToObjectType,
                ReferenceType = baseField.ReferenceType,
                Category = baseField.Category,
                Order = baseField.Order,
                LiveSiteFormComponentIdentifier = (string?)settings?.Element("livesitecomponentidentifier"),
                ValidationRule = (string?)element.Element("validationrule") ?? (string?)settings?.Element("validationrule"),
                ValidationErrorMessage = (string?)element.Element("validationerrormessage"),
                VisibilityCondition = (string?)element.Element("visibilitycondition"),
                ExplanationText = (string?)properties?.Element("explanationtext"),
                Tooltip = (string?)properties?.Element("tooltip")
            };
        }

        private static bool ParseBool(XAttribute? attr, bool defaultValue = false)
        {
            if (attr is null)
            {
                return defaultValue;
            }

            return string.Equals(attr.Value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ParseBool(XElement? element, bool defaultValue = false)
        {
            if (element is null || string.IsNullOrWhiteSpace(element.Value))
            {
                return defaultValue;
            }

            return string.Equals(element.Value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static int? ParseNullableInt(XAttribute? attr)
        {
            if (attr is null || string.IsNullOrWhiteSpace(attr.Value))
            {
                return null;
            }

            return int.TryParse(attr.Value, out int value) ? value : null;
        }
    }
}
