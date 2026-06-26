using KX13.ContentAuditor.DataAccess.Analysis;
using KX13.ContentAuditor.DataAccess.DbAccess;
using KX13.ContentAuditor.DataAccess.Parsers;
using KX13.ContentAuditor.DataAccess.Repositories;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KX13.ContentAuditor.DataAccess
{
    public static class ServiceCollectionExtensions
    {
        private const int DefaultSqlCommandTimeoutSeconds = 120;

        public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
        {
            const string missingConnectionStringMessage =
                "Connection string 'ConnectionString' not found or empty. Configure it in appsettings.json, appsettings.development.json, or the ConnectionStrings__ConnectionString environment variable.";

            string connectionString = configuration.GetConnectionString("ConnectionString")
                ?? throw new InvalidOperationException(missingConnectionStringMessage);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(missingConnectionStringMessage);
            }

            int commandTimeoutSeconds = GetCommandTimeoutSeconds(configuration);

            services.AddSingleton(new SqlConnectionFactory(connectionString));
            services.AddSingleton<AuditFailureCollector>();
            services.AddTransient<DbReader>(sp => new DbReader(
                sp.GetRequiredService<SqlConnectionFactory>(),
                commandTimeoutSeconds));

            // Parsers
            services.AddSingleton<ClassFormDefinitionParser>();
            services.AddSingleton<PageBuilderConfigParser>();

            // Analysis
            services.AddSingleton<PageBuilderComponentDiscovery>();
            services.AddSingleton<ContentReferenceAnalyzer>();

            // Repositories
            services.AddTransient<ISiteRepository, SiteRepository>();
            services.AddTransient<IPageTypeRepository, PageTypeRepository>();
            services.AddTransient<IContentTreeRepository, ContentTreeRepository>();
            services.AddTransient<ICustomTableRepository, CustomTableRepository>();
            services.AddTransient<IFormRepository, FormRepository>();
            services.AddTransient<ICustomModuleRepository, CustomModuleRepository>();
            services.AddTransient<IRelationshipRepository, RelationshipRepository>();

            return services;
        }

        private static int GetCommandTimeoutSeconds(IConfiguration configuration)
        {
            string? configuredValue = configuration["DataAccess:SqlCommandTimeoutSeconds"];
            if (string.IsNullOrWhiteSpace(configuredValue))
            {
                return DefaultSqlCommandTimeoutSeconds;
            }

            if (!int.TryParse(configuredValue, out int commandTimeoutSeconds) || commandTimeoutSeconds < 0)
            {
                throw new InvalidOperationException(
                    "Configuration value 'DataAccess:SqlCommandTimeoutSeconds' must be a non-negative integer.");
            }

            return commandTimeoutSeconds;
        }
    }
}
