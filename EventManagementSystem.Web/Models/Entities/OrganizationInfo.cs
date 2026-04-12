namespace EventManagementSystem.Web.Models.Entities
{
    public class OrganizationInfo
    {
        public int Id { get; set; }
        public string? OrganizationName { get; set; }
        public string? OrgType { get; set; }
        public string? OrgHotline { get; set; }
        public string? OrgEmail { get; set; }
        public string? OrgAddress { get; set; }
        public string? OrganizationBio { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
