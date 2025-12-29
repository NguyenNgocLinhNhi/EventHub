using EventManagementSystem.Web.Models.Entities;

namespace EventManagementSystem.Web.ViewModels
{
    public class HomeViewModel
    {
        // Sự kiện nổi bật / tâm điểm (CÓ THỂ NULL)
        public Event? SpotlightEvent { get; set; }

        // Danh sách sự kiện sắp diễn ra (KHÔNG NULL)
        public List<Event> UpcomingEvents { get; set; }
            = new();
    }
}
