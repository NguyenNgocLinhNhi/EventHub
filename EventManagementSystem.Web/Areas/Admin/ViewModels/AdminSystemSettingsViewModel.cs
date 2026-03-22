namespace EventManagementSystem.Web.Areas.Admin.ViewModels
{
    public class AdminSystemSettingsViewModel
    {
        // Cấu hình chung
        public string SystemName { get; set; }
        public string SystemDescription { get; set; }
        public string SystemLogoUrl { get; set; }
        public string DefaultTimeZone { get; set; }

        // Cấu hình Email
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpPass { get; set; }
        public bool EnableSsl { get; set; }
    }
}
