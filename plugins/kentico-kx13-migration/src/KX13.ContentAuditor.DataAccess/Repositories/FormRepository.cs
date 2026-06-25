using KX13.ContentAuditor.DataAccess.DbAccess;
using KX13.ContentAuditor.DataAccess.Models;
using KX13.ContentAuditor.DataAccess.Parsers;

using Microsoft.Data.SqlClient;

namespace KX13.ContentAuditor.DataAccess.Repositories
{
    public class FormRepository : IFormRepository
    {
        private readonly DbReader dbReader;
        private readonly ClassFormDefinitionParser formDefinitionParser;

        public FormRepository(DbReader dbReader, ClassFormDefinitionParser formDefinitionParser)
        {
            this.dbReader = dbReader;
            this.formDefinitionParser = formDefinitionParser;
        }

        private const string GetSiteFormsSql = """
            SELECT f.FormID, f.FormDisplayName, f.FormName, f.FormSiteID, f.FormClassID,
                   c.ClassTableName, c.ClassFormDefinition,
                   f.FormSendToEmail, f.FormSendFromEmail,
                   f.FormEmailSubject, f.FormEmailTemplate, f.FormEmailAttachUploadedDocs,
                   f.FormRedirectToUrl, f.FormDisplayText,
                   f.FormClearAfterSave, f.FormSubmitButtonText,
                   f.FormConfirmationEmailField, f.FormConfirmationTemplate,
                   f.FormConfirmationSendFromEmail, f.FormConfirmationEmailSubject,
                   f.FormLogActivity, f.FormBuilderLayout
            FROM CMS_Form f
            INNER JOIN CMS_Class c ON f.FormClassID = c.ClassID
            WHERE f.FormSiteID = @SiteID
            """;

        private const string GetAllFormsSql = """
            SELECT f.FormID, f.FormDisplayName, f.FormName, f.FormSiteID, f.FormClassID,
                   c.ClassTableName, c.ClassFormDefinition,
                   f.FormSendToEmail, f.FormSendFromEmail,
                   f.FormEmailSubject, f.FormEmailTemplate, f.FormEmailAttachUploadedDocs,
                   f.FormRedirectToUrl, f.FormDisplayText,
                   f.FormClearAfterSave, f.FormSubmitButtonText,
                   f.FormConfirmationEmailField, f.FormConfirmationTemplate,
                   f.FormConfirmationSendFromEmail, f.FormConfirmationEmailSubject,
                   f.FormLogActivity, f.FormBuilderLayout
            FROM CMS_Form f
            INNER JOIN CMS_Class c ON f.FormClassID = c.ClassID
            """;

        private const string GetAlternativeFormsSql = """
            SELECT FormID, FormDisplayName, FormName, FormClassID, FormDefinition, FormLayoutType
            FROM CMS_AlternativeForm
            WHERE FormClassID = @ClassID
            """;

        public async Task<List<Form>> GetSiteFormsAsync(int siteId)
        {
            var results = await dbReader.QueryAsync(GetSiteFormsSql,
                new SqlParameter("@SiteID", siteId));

            var forms = new List<Form>(results.Count);

            foreach (var row in results)
            {
                forms.Add(await MapFormAsync(row));
            }

            return forms;
        }

        public async Task<List<Form>> GetAllFormsAsync()
        {
            var results = await dbReader.QueryAsync(GetAllFormsSql);
            var forms = new List<Form>(results.Count);

            foreach (var row in results)
            {
                forms.Add(await MapFormAsync(row));
            }

            return forms;
        }

        public async Task<List<AlternativeForm>> GetAlternativeFormsAsync(int classId)
        {
            var results = await dbReader.QueryAsync(GetAlternativeFormsSql,
                new SqlParameter("@ClassID", classId));

            return results.Select(row => new AlternativeForm
            {
                FormId = Convert.ToInt32(row["FormID"]),
                FormDisplayName = row["FormDisplayName"] as string,
                FormName = row["FormName"] as string,
                FormClassId = Convert.ToInt32(row["FormClassID"]),
                FormDefinitionDelta = row["FormDefinition"] as string,
                FormLayoutType = row["FormLayoutType"] as string
            }).ToList();
        }

        private async Task<Form> MapFormAsync(Dictionary<string, object?> row)
        {
            int classId = Convert.ToInt32(row["FormClassID"]);
            string? classFormDefinition = row["ClassFormDefinition"] as string;
            string formName = row["FormName"] as string ?? $"FormID {row["FormID"]}";
            string? displayName = row["FormDisplayName"] as string;

            return new Form
            {
                FormId = Convert.ToInt32(row["FormID"]),
                FormDisplayName = row["FormDisplayName"] as string,
                FormName = row["FormName"] as string,
                FormSiteId = Convert.ToInt32(row["FormSiteID"]),
                FormClassId = classId,
                FormTableName = row["ClassTableName"] as string,
                SendToEmail = row["FormSendToEmail"] as string,
                SendFromEmail = row["FormSendFromEmail"] as string,
                EmailSubject = row["FormEmailSubject"] as string,
                EmailTemplate = row["FormEmailTemplate"] as string,
                EmailAttachUploadedDocuments = Convert.ToBoolean(row["FormEmailAttachUploadedDocs"] ?? false),
                RedirectToUrl = row["FormRedirectToUrl"] as string,
                DisplayText = row["FormDisplayText"] as string,
                ClearAfterSave = Convert.ToBoolean(row["FormClearAfterSave"] ?? false),
                SubmitButtonText = row["FormSubmitButtonText"] as string,
                ConfirmationEmailField = row["FormConfirmationEmailField"] as string,
                ConfirmationTemplate = row["FormConfirmationTemplate"] as string,
                ConfirmationSendFromEmail = row["FormConfirmationSendFromEmail"] as string,
                ConfirmationEmailSubject = row["FormConfirmationEmailSubject"] as string,
                LogActivity = Convert.ToBoolean(row["FormLogActivity"] ?? false),
                BuilderLayout = row["FormBuilderLayout"] as string,
                Fields = formDefinitionParser.TryParseFormFieldDefinitions(classFormDefinition, formName, displayName),
                AlternativeForms = await GetAlternativeFormsAsync(classId)
            };
        }
    }
}
