namespace EventManagementSystem.Web.Models.Entities
{
    public class Review
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public virtual Event? Event { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int Rating { get; set; } // 1-5 sao
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;        
    }
}
