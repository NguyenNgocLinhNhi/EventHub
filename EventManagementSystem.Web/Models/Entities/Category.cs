using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Web.Models.Entities
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        public virtual ICollection<Event> Events { get; set; }
            = new List<Event>();
    }
}