namespace KX13.ContentAuditor.CLI
{
    internal static class AuditCliOptionsParser
    {
        private static readonly HashSet<string> KnownBooleanFlags = new(StringComparer.OrdinalIgnoreCase)
        {
            "--help", "-h",
            "--sites", "--page-types", "--page-builder-components",
            "--custom-modules", "--custom-tables", "--forms", "--relationships",
            "--report"
        };

        private static readonly HashSet<string> KnownValueFlags = new(StringComparer.OrdinalIgnoreCase)
        {
            "--output", "--site-name", "--class-name", "--page-path"
        };

        public static AuditCliOptions Parse(string[] args)
        {
            var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var errors = new List<string>();

            int index = 0;
            while (index < args.Length)
            {
                string arg = args[index];

                if (KnownBooleanFlags.Contains(arg))
                {
                    flags.Add(arg);
                    index++;
                    continue;
                }

                if (KnownValueFlags.Contains(arg))
                {
                    if (index + 1 >= args.Length)
                    {
                        errors.Add($"Flag '{arg}' requires a value.");
                        break;
                    }
                    else if (!KnownBooleanFlags.Contains(args[index + 1]) && !KnownValueFlags.Contains(args[index+1]))
                    {
                        values[arg] = args[index + 1];
                        index += 2;
                    }
                    else
                    {
                        errors.Add($"Flag '{arg}' requires a value.");
                        break;
                    }

                    continue;
                }

                errors.Add($"Unexpected argument: '{arg}'.");
                break;
            }

            return new AuditCliOptions
            {
                Errors = errors,
                ShowHelp = flags.Contains("--help") || flags.Contains("-h"),
                ExportSites = flags.Contains("--sites"),
                ExportPageTypes = flags.Contains("--page-types"),
                ExportPageBuilderComponents = flags.Contains("--page-builder-components"),
                ExportCustomModules = flags.Contains("--custom-modules"),
                ExportCustomTables = flags.Contains("--custom-tables"),
                ExportForms = flags.Contains("--forms"),
                ExportRelationships = flags.Contains("--relationships"),
                GenerateReport = flags.Contains("--report"),
                OutputPath = values.GetValueOrDefault("--output"),
                SiteName = values.GetValueOrDefault("--site-name"),
                ClassNamePattern = values.GetValueOrDefault("--class-name"),
                PagePathPrefix = values.GetValueOrDefault("--page-path")
            };
        }
    }
}