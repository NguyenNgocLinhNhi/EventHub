using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagementSystem.Web.Models.Entities
{
    public class ContactInquiry
    {
        [Key]
        public int Id { get; set; }
        public string? Category { get; set; } // Ví dụ: "Attendee" hoặc "Organizer"

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [StringLength(200)]
        public string? Subject { get; set; }

        [Required]
        [StringLength(2000)]
        public string Message { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsReplied { get; set; } = false;

        [StringLength(2000)]
        public string? ReplyMessage { get; set; }

        public DateTime? RepliedAt { get; set; }

        // Gắn thắc mắc này với một Sự kiện cụ thể
        public int? EventId { get; set; }

        [ForeignKey("EventId")]
        public virtual Event? Event { get; set; }
        public string? UserId { get; set; }
        public bool IsReadByAttendee { get; set; } = false;
        public bool IsReadByAdmin { get; set; } = false;
        public bool IsReadByOrganizer { get; set; } = false;
    }
}