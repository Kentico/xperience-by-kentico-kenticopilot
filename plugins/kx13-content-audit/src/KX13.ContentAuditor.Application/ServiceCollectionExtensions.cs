using KX13.ContentAuditor.Application.Services;

using Microsoft.Extensions.DependencyInjection;

namespace KX13.ContentAuditor.Application
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddTransient<ContentModelService>();
            services.AddTransient<MarkdownReportService>();
            services.AddTransient<JsonExportService>();

            return services;
        }
    }
}
