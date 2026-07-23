namespace KX13.ContentAuditor.DataAccess.Models
{
    public class Form
    {
        public int FormId { get; set; }

        public string? FormDisplayName { get; set; }

        public string? FormName { get; set; }

        public int FormSiteId { get; set; }

        public int FormClassId { get; set; }

        public string? FormTableName { get; set; }

        public string? SendToEmail { get; set; }

        public string? SendFromEmail { get; set; }

        public string? EmailSubject { get; set; }

        public string? EmailTemplate { get; set; }

        public bool EmailAttachUploadedDocuments { get; set; }

        public string? RedirectToUrl { get; set; }

        public string? DisplayText { get; set; }

        public bool ClearAfterSave { get; set; }

        public string? SubmitButtonText { get; set; }

        public string? ConfirmationEmailField { get; set; }

        public string? ConfirmationTemplate { get; set; }

        public string? ConfirmationSendFromEmail { get; set; }

        public string? ConfirmationEmailSubject { get; set; }

        public bool LogActivity { get; set; }

        public string? BuilderLayout { get; set; }

        public List<FormFieldDefinition> Fields { get; set; } = [];

        public List<AlternativeForm> AlternativeForms { get; set; } = [];
    }
}
