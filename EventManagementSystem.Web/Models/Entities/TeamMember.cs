namespace EventManagementSystem.Web.Models.Entities
{
    public class TeamMember
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Position { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? FacebookUrl { get; set; }
        public string? ZaloUrl { get; set; }
        public string? GithubUrl { get; set; }
        public bool IsVisible { get; set; } = true; // Mặc định là hiện
    }
}
