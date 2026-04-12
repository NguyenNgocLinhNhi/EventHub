namespace EventManagementSystem.Web.Models.Entities
{
    public class AdminSystemSetting
    {
        public int Id { get; set; }
        public string SettingKey { get; set; } // Vd: SmtpServer, SystemLogo
        public string SettingValue { get; set; }
    }
}
