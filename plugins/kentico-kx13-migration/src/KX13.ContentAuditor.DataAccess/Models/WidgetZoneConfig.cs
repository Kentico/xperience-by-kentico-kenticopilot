namespace KX13.ContentAuditor.DataAccess.Models
{
    public class WidgetZoneConfig
    {
        public string? Identifier { get; set; }

        public List<WidgetConfig> Widgets { get; set; } = [];
    }
}