using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Web.Areas.Organizer.ViewModels
{
    public class InquiryReplyViewModel
    {
        public int InquiryId { get; set; }

        public string CustomerName { get; set; } // Chỉ hiển thị để Admin biết đang trả lời ai

        public string Question { get; set; } // Hiển thị nội dung câu hỏi cũ

        [Required(ErrorMessage = "Vui lòng nhập nội dung phản hồi.")]
        [MinLength(5, ErrorMessage = "Nội dung phản hồi quá ngắn.")]
        public string ReplyContent { get; set; }
    }
}
