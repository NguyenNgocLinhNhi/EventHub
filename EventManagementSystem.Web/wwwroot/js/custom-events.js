$(document).ready(function () {
    $(".my-event-slider").owlCarousel({
        // --- CẤU HÌNH TỰ ĐỘNG CHẠY ---
        autoplay: true,           // Bật tự động chạy
        autoplayTimeout: 3000,    // Thời gian chờ: 3000ms = 3 giây
        autoplayHoverPause: true, // Di chuột vào thì dừng lại (để người dùng kịp xem)
        smartSpeed: 1000,         // Tốc độ trượt mượt mà

        // --- CẤU HÌNH CƠ BẢN ---
        margin: 25,               // Khoảng cách giữa các ảnh
        dots: false,              // Ẩn dấu chấm tròn bên dưới
        loop: true,               // Chạy vòng lặp vô tận
        slideBy: 1,               // Mỗi lần nhảy 1 ảnh

        // --- TẮT MŨI TÊN ---
        nav: false,               // <--- Đặt thành false để ẩn mũi tên
        navText: [],              // Xóa icon mũi tên cho chắc chắn

        // --- CẤU HÌNH MÀN HÌNH ---
        responsive: {
            0: { items: 1 },
            768: { items: 2 },
            992: { items: 3 },
            1200: { items: 4 }    // Hiện 4 ảnh trên màn hình lớn
        }
    });
});