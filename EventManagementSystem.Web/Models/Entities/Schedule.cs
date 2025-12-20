using System;
using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Web.Models.Entities
{
    public class Schedule
    {
        public int Id { get; set; }

        // ===== ACTIVITY INFO =====
        [Required]
        [Display(Name = "Tên hoạt động")]
        [StringLength(100)]
        public string Title { get; set; } = null!;   // FIX

        // ===== TIME =====
        [Required]
        [Display(Name = "Giờ bắt đầu")]
        public DateTime StartTime { get; set; }

        [Required]
        [Display(Name = "Giờ kết thúc")]
        public DateTime EndTime { get; set; }

        // ===== LOCATION =====
        [Display(Name = "Địa điểm nhỏ")]
        [StringLength(100)]
        public string? Location { get; set; }        // OPTIONAL → nullable

        // ===== DESCRIPTION =====
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }     // OPTIONAL → nullable

        // ===== RELATION: EVENT =====
        [Required]
        public int EventId { get; set; }

        public Event Event { get; set; } = null!;    // FIX
    }
}
