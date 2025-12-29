using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EventManagementSystem.Web.Data
{
    public static class SeedData
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Tự động Migrate database nếu có thay đổi cấu trúc
            context.Database.Migrate();

            context.Schedules.RemoveRange(context.Schedules);
            context.Sponsors.RemoveRange(context.Sponsors);
            context.Speakers.RemoveRange(context.Speakers);
            context.TicketTypes.RemoveRange(context.TicketTypes);

            context.Events.RemoveRange(context.Events);
            context.Categories.RemoveRange(context.Categories);

            context.SaveChanges();

            // === 1. TẠO DANH MỤC (CATEGORIES) ===
            if (!context.Categories.Any())
            {
                context.Categories.AddRange(
                    new Category { Name = "Y học & Sức khỏe", Description = "Hội thảo chuyên ngành y khoa." },
                    new Category { Name = "Công nghệ", Description = "Triển lãm công nghệ, AI, Blockchain." },
                    new Category { Name = "Giáo dục", Description = "Du học, hướng nghiệp và kỹ năng mềm." },
                    new Category { Name = "Âm nhạc & Giải trí", Description = "Concert, EDM, Festival." },
                    new Category { Name = "Ẩm thực & Đồ uống", Description = "Lễ hội ẩm thực, Wine tasting." },
                    new Category { Name = "Cộng đồng & Xã hội", Description = "Các hoạt động tình nguyện, gây quỹ." },
                    new Category { Name = "Kinh doanh & Đầu tư", Description = "Hội thảo kinh tế, Bất động sản." },
                    new Category { Name = "Khoa học & Giáo dục", Description = "Hội thảo chuyên đề, nghiên cứu." }
                );
                context.SaveChanges();
            }

            // =========================================================
            // SỰ KIỆN 1: MEDINOVA (Y TẾ)
            // =========================================================
            var medCat = context.Categories.FirstOrDefault(c => c.Name == "Y học & Sức khỏe");
            if (medCat != null && !context.Events.Any(e => e.Title.Contains("Medinova")))
            {
                var medEvent = new Event
                {
                    Title = "Hội nghị Tim mạch Quốc tế Medinova 2025",
                    Description = @"<p>Hội nghị quy tụ hơn <strong>500 chuyên gia đầu ngành</strong> để thảo luận về các đột phá trong điều trị tim mạch.</p>",
                    ImageUrl = "/Templates/Medinova/img/hero.jpg",
                    Location = "Trung tâm Hội nghị Quốc gia, Hà Nội",
                    StartDate = new DateTime(2025, 12, 20, 8, 0, 0),
                    EndDate = new DateTime(2025, 12, 20, 17, 0, 0),
                    IsActive = true,
                    CategoryId = medCat.Id,
                    LandingPage = "Medinova"
                };
                context.Events.Add(medEvent);
                context.SaveChanges(); // Lưu để lấy Id cho các bảng con

                // 1. Thêm danh sách vé (TicketTypes) 
                context.TicketTypes.AddRange(
                    new TicketType { Name = "Vé Bác Sĩ", Price = 500000, Quantity = 200, EventId = medEvent.Id },
                    new TicketType { Name = "Vé Sinh Viên", Price = 100000, Quantity = 100, EventId = medEvent.Id },
                    new TicketType { Name = "Vé VIP (Gala Dinner)", Price = 2000000, Quantity = 50, EventId = medEvent.Id }
                );

                // 2. Thêm danh sách diễn giả (Speakers)
                context.Speakers.AddRange(
                    new Speaker
                    {
                        Name = "Dr. Sarah Smith",
                        JobTitle = "Chuyên gia cao cấp WHO",
                        ImageUrl = "/Templates/Medinova/img/team-2.jpg",
                        SocialUrl = "https://facebook.com/drsarah",
                        EventId = medEvent.Id
                    },
                    new Speaker
                    {
                        Name = "PGS.TS. Trần Văn B",
                        JobTitle = "Viện trưởng Viện Tim Mạch",
                        ImageUrl = "/Templates/Medinova/img/team-1.jpg",
                        EventId = medEvent.Id
                    }
                );

               // 3. Thêm lịch trình chi tiết (Schedules)
                context.Schedules.AddRange(
                    new Schedule
                    {
                        Title = "Đón tiếp và Khai mạc",
                        StartTime = new DateTime(2025, 12, 20, 8, 0, 0),
                        EndTime = new DateTime(2025, 12, 20, 9, 0, 0),
                        Location = "Sảnh chính (Grand Hall)",
                        Description = "Thủ tục check-in và nhận tài liệu hội thảo.",
                        EventId = medEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Phiên thảo luận: Công nghệ Tim mạch mới",
                        StartTime = new DateTime(2025, 12, 20, 9, 0, 0),
                        EndTime = new DateTime(2025, 12, 20, 11, 30, 0),
                        Location = "Phòng hội thảo A1",
                        Description = "Báo cáo chuyên sâu về ứng dụng AI trong chẩn đoán hình ảnh tim mạch.",
                        EventId = medEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Nghỉ trưa & Networking",
                        StartTime = new DateTime(2025, 12, 20, 12, 0, 0),
                        EndTime = new DateTime(2025, 12, 20, 13, 30, 0),
                        Location = "Khu vực Nhà hàng tầng 2",
                        Description = "Tiệc buffet trưa và giao lưu giữa các đại biểu.",
                        EventId = medEvent.Id
                    }
                );

              // 4. Thêm nhà tài trợ (Sponsors) 
                context.Sponsors.AddRange(
                    new Sponsor
                    {
                        Name = "Vinmec Healthcare",
                        Rank = "Bạch kim",
                        LogoUrl = "/Templates/Medinova/img/vendor-1.jpg",
                        WebsiteUrl = "https://vinmec.com",
                        EventId = medEvent.Id
                    },
                    new Sponsor
                    {
                        Name = "Samsung Medical",
                        Rank = "Vàng",
                        LogoUrl = "/Templates/Medinova/img/vendor-2.jpg",
                        EventId = medEvent.Id
                    }
                );

                context.SaveChanges();
            }

            // =========================================================
            // SỰ KIỆN 2: CHEFER (ẨM THỰC)
            // =========================================================
            var foodCat = context.Categories.FirstOrDefault(c => c.Name == "Ẩm thực & Đồ uống");
            if (foodCat != null && !context.Events.Any(e => e.Title.Contains("Taste of The World")))
            {
                var chefEvent = new Event
                {
                    Title = "Đại Tiệc Ẩm Thực Quốc Tế: Taste of The World 2025",
                    Description = "Hành trình đánh thức mọi giác quan với 10 đầu bếp Michelin.",
                    ImageUrl = "/Templates/Chefer/img/hero-1.jpg",
                    Location = "Khách sạn Metropole, Hà Nội",
                    StartDate = new DateTime(2025, 12, 24, 18, 0, 0),
                    EndDate = new DateTime(2025, 12, 24, 22, 30, 0),
                    IsActive = true,
                    CategoryId = foodCat.Id,
                    LandingPage = "Chefer"
                };
                context.Events.Add(chefEvent);
                context.SaveChanges(); // Lưu để lấy EventId

                // 1. Thêm các loại vé (TicketTypes)
                context.TicketTypes.AddRange(
                    new TicketType { Name = "Diamond VIP", Price = 10000000, Quantity = 20, EventId = chefEvent.Id },
                    new TicketType { Name = "Standard", Price = 2000000, Quantity = 100, EventId = chefEvent.Id }
                );

                // 2. Thêm Diễn giả / Đầu bếp (Speakers)
                context.Speakers.AddRange(
                    new Speaker { Name = "Gordon Ramsay", JobTitle = "Siêu Đầu Bếp Michelin", ImageUrl = "/Templates/Chefer/img/team-1.jpg", SocialUrl = "https://facebook.com/gordonramsay", EventId = chefEvent.Id },
                    new Speaker { Name = "Alain Ducasse", JobTitle = "Huyền thoại ẩm thực Pháp", ImageUrl = "/Templates/Chefer/img/team-2.jpg", EventId = chefEvent.Id }
                );

                // 3. Thêm Lịch trình (Schedules) - Giúp hiện Menu "Lịch trình"
                context.Schedules.AddRange(
                    new Schedule { Title = "Tiệc Rượu Khai Vị", StartTime = new DateTime(2025, 12, 24, 18, 0, 0), EndTime = new DateTime(2025, 12, 24, 19, 0, 0), Location = "Sảnh chính", Description = "Thưởng thức rượu vang và món khai vị nhẹ.", EventId = chefEvent.Id },
                    new Schedule { Title = "Trình diễn Chế biến", StartTime = new DateTime(2025, 12, 24, 19, 0, 0), EndTime = new DateTime(2025, 12, 24, 21, 0, 0), Location = "Khu vực bếp mở", Description = "Gordon Ramsay trực tiếp trình diễn món bò Wellington.", EventId = chefEvent.Id },
                    new Schedule { Title = "Dạ tiệc chính", StartTime = new DateTime(2025, 12, 24, 21, 0, 0), EndTime = new DateTime(2025, 12, 24, 22, 30, 0), Location = "Phòng tiệc lớn", Description = "Thực đơn 7 món chuẩn Michelin.", EventId = chefEvent.Id }
                );

                // 4. Thêm Nhà tài trợ (Sponsors) - Giúp hiện Menu "Đối tác"
                context.Sponsors.AddRange(
                    new Sponsor { Name = "Moët & Chandon", Rank = "Kim cương", LogoUrl = "https://example.com/moet-logo.png", WebsiteUrl = "https://www.moet.com", EventId = chefEvent.Id },
                    new Sponsor { Name = "Michelin Guide", Rank = "Vàng", LogoUrl = "https://example.com/michelin-logo.png", EventId = chefEvent.Id }
                );

                context.SaveChanges();
            }

            // =========================================================
            // SỰ KIỆN 3: CHARITIZE (TỪ THIỆN)
            // =========================================================
            var charityCat = context.Categories.FirstOrDefault(c => c.Name == "Cộng đồng & Xã hội");
            if (charityCat != null && !context.Events.Any(e => e.Title.Contains("Run For The Future")))
            {
                var charityEvent = new Event
                {
                    Title = "Chạy Vì Trái Tim: Run For The Future 2025",
                    Description = "Mỗi bước chạy - Một hy vọng phẫu thuật tim cho trẻ em.",
                    ImageUrl = "/Templates/Charitize/img/carousel-1.jpg",
                    Location = "Công viên Thống Nhất, Hà Nội",
                    StartDate = new DateTime(2025, 11, 15, 6, 0, 0),
                    EndDate = new DateTime(2025, 11, 15, 11, 0, 0),
                    IsActive = true,
                    CategoryId = charityCat.Id,
                    LandingPage = "Charitize"
                };
                context.Events.Add(charityEvent);
                context.SaveChanges();

                // BỔ SUNG LỊCH TRÌNH (Schedules)
                context.Schedules.AddRange(
                    new Schedule { StartTime = charityEvent.StartDate, EndTime = charityEvent.StartDate.AddHours(1), Title = "Tập trung & Khởi động", Location = "Cổng chính", EventId = charityEvent.Id },
                    new Schedule { StartTime = charityEvent.StartDate.AddHours(1), EndTime = charityEvent.EndDate ?? charityEvent.StartDate.AddHours(4), Title = "Bắt đầu chạy bộ", Location = "Đường chạy 5km", EventId = charityEvent.Id }
                );

                // BỔ SUNG NHÀ TÀI TRỢ (Sponsors)
                context.Sponsors.Add(new Sponsor { Name = "Nhà hảo tâm A", Rank = "Kim cương", LogoUrl = "/img/sponsors/diamond.png", EventId = charityEvent.Id });

                context.TicketTypes.Add(new TicketType { Name = "Run Kit", Price = 300000, Quantity = 1000, EventId = charityEvent.Id });
                context.Speakers.Add(new Speaker { Name = "H'Hen Niê", JobTitle = "Đại sứ", ImageUrl = "/Templates/Charitize/img/team-1.jpg", EventId = charityEvent.Id });

                context.SaveChanges();
            }

            // =========================================================
            // SỰ KIỆN 4: NOVA (CÔNG NGHỆ)
            // =========================================================
            var techCat = context.Categories.FirstOrDefault(c => c.Name == "Công nghệ");
            if (techCat != null && !context.Events.Any(e => e.Title.Contains("Tech Summit")))
            {
                var novaEvent = new Event
                {
                    Title = "Vietnam Tech Summit 2025: AI & Blockchain",
                    Description = "Hội thảo công nghệ lớn nhất năm quy tụ lãnh đạo các tập đoàn công nghệ hàng đầu thế giới như OpenAI và Tesla để thảo luận về tương lai của AI.",
                    ImageUrl = "/Templates/Nova/assets/img/hero/hero-5/hero-img.svg",
                    Location = "GEM Center, TP.HCM",
                    StartDate = new DateTime(2025, 10, 10, 9, 0, 0),
                    EndDate = new DateTime(2025, 10, 11, 17, 0, 0),
                    IsActive = true,
                    CategoryId = techCat.Id,
                    LandingPage = "Nova" // Chỉ định template Nova
                };
                context.Events.Add(novaEvent);
                context.SaveChanges(); // Lưu để lấy Id cho các bảng liên quan

                // 1. Thêm danh sách vé (TicketTypes)
                context.TicketTypes.AddRange(
                    new TicketType { Name = "Investor VIP", Price = 10000000, Quantity = 50, EventId = novaEvent.Id },
                    new TicketType { Name = "Standard Access", Price = 2000000, Quantity = 500, EventId = novaEvent.Id },
                    new TicketType { Name = "Student", Price = 500000, Quantity = 200, EventId = novaEvent.Id }
                );

                // 2. Thêm diễn giả (Speakers) - Giúp hiện Menu "Diễn giả"
                context.Speakers.AddRange(
                    new Speaker
                    {
                        Name = "Elon Musk",
                        JobTitle = "CEO Tesla & SpaceX",
                        ImageUrl = "/Templates/Medinova/img/team-1.jpg", // Sử dụng ảnh mẫu có sẵn hoặc link online
                        SocialUrl = "https://x.com/elonmusk",
                        EventId = novaEvent.Id
                    },
                    new Speaker
                    {
                        Name = "Sam Altman",
                        JobTitle = "CEO OpenAI",
                        ImageUrl = "/Templates/Medinova/img/team-2.jpg",
                        SocialUrl = "https://x.com/sama",
                        EventId = novaEvent.Id
                    }
                );

                // 3. Thêm lịch trình (Schedules) - Giúp hiện Menu "Lịch trình"
                context.Schedules.AddRange(
                    new Schedule
                    {
                        Title = "Khai mạc & Keynote AI",
                        StartTime = new DateTime(2025, 10, 10, 9, 0, 0),
                        EndTime = new DateTime(2025, 10, 10, 11, 0, 0),
                        Location = "Hội trường Grand Ballroom",
                        Description = "Bài phát biểu chính về xu hướng AI trong kỷ nguyên mới.",
                        EventId = novaEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Workshop: Blockchain App",
                        StartTime = new DateTime(2025, 10, 10, 14, 0, 0),
                        EndTime = new DateTime(2025, 10, 10, 16, 30, 0),
                        Location = "Phòng Workshop 1",
                        Description = "Thực hành xây dựng ứng dụng phi tập trung.",
                        EventId = novaEvent.Id
                    }
                );

                // 4. Thêm nhà tài trợ (Sponsors) - Giúp hiện Menu "Đối tác"
                context.Sponsors.AddRange(
                    new Sponsor
                    {
                        Name = "Google Cloud",
                        Rank = "Kim cương",
                        LogoUrl = "/Templates/Medinova/img/vendor-1.jpg",
                        WebsiteUrl = "https://cloud.google.com",
                        EventId = novaEvent.Id
                    },
                    new Sponsor
                    {
                        Name = "FPT Software",
                        Rank = "Vàng",
                        LogoUrl = "/Templates/Medinova/img/vendor-2.jpg",
                        WebsiteUrl = "https://fptsoftware.com",
                        EventId = novaEvent.Id
                    }
                );

                context.SaveChanges();
            }
            // =========================================================
            // SỰ KIỆN 5: YUMMY (GALA DINNER)
            // =========================================================
            if (foodCat != null && !context.Events.Any(e => e.Title.Contains("Year End Party")))
            {
                var galaEvent = new Event
                {
                    Title = "Year End Party 2025: Dạ Tiệc Doanh Nhân",
                    Description = "Không gian sang trọng, ẩm thực 5 sao và networking đỉnh cao dành riêng cho cộng đồng doanh nhân Việt Nam.",
                    ImageUrl = "/Templates/Yummy/assets/img/hero-img.png",
                    Location = "Trung tâm Hội nghị White Palace, TP.HCM",
                    StartDate = new DateTime(2025, 12, 31, 19, 0, 0),
                    EndDate = new DateTime(2025, 12, 31, 23, 59, 0),
                    IsActive = true,
                    CategoryId = foodCat.Id,
                    LandingPage = "Yummy"
                };
                context.Events.Add(galaEvent);
                context.SaveChanges(); // Lưu để lấy Id cho các bảng con

                // 1. Thêm danh sách vé (TicketTypes)
                context.TicketTypes.AddRange(
                    new TicketType { Name = "Bàn Tiệc 10 Người", Price = 10000000, Quantity = 50, EventId = galaEvent.Id },
                    new TicketType { Name = "Vé Cá Nhân VIP", Price = 1500000, Quantity = 100, EventId = galaEvent.Id }
                );

                // 2. Thêm Diễn giả / Đầu bếp (Speakers) - Để hiện Menu "Khách mời"
                context.Speakers.AddRange(
                    new Speaker
                    {
                        Name = "Gordon Ramsay",
                        JobTitle = "Siêu đầu bếp MasterChef",
                        ImageUrl = "/Templates/Yummy/assets/img/chefs/chefs-1.jpg",
                        SocialUrl = "https://facebook.com/gordonramsay",
                        EventId = galaEvent.Id
                    },
                    new Speaker
                    {
                        Name = "Nguyễn Quốc Nam",
                        JobTitle = "Bếp trưởng điều hành White Palace",
                        ImageUrl = "/Templates/Yummy/assets/img/chefs/chefs-2.jpg",
                        EventId = galaEvent.Id
                    }
                );

                // 3. Thêm lịch trình chi tiết (Schedules) - Để hiện Menu "Lịch trình"
                context.Schedules.AddRange(
                    new Schedule
                    {
                        Title = "Đón khách & Tiệc rượu nhẹ",
                        StartTime = new DateTime(2025, 12, 31, 19, 0, 0),
                        EndTime = new DateTime(2025, 12, 31, 20, 0, 0),
                        Location = "Sảnh chờ Kim Cương",
                        Description = "Thưởng thức Champagne và giao lưu kết nối đầu giờ.",
                        EventId = galaEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Khai mạc & Trình diễn Ẩm thực",
                        StartTime = new DateTime(2025, 12, 31, 20, 0, 0),
                        EndTime = new DateTime(2025, 12, 31, 21, 30, 0),
                        Location = "Hội trường chính",
                        Description = "Phát biểu khai mạc và màn trình diễn nấu ăn trực tiếp từ các siêu đầu bếp.",
                        EventId = galaEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Dạ tiệc & Countdown",
                        StartTime = new DateTime(2025, 12, 31, 21, 30, 0),
                        EndTime = new DateTime(2025, 12, 31, 23, 59, 0),
                        Location = "Hội trường chính",
                        Description = "Thưởng thức thực đơn 7 món cao cấp và đếm ngược chào năm mới.",
                        EventId = galaEvent.Id
                    }
                );

                // 4. Thêm Nhà tài trợ (Sponsors) - Để hiện Menu "Đối tác"
                context.Sponsors.AddRange(
                    new Sponsor
                    {
                        Name = "Heineken Vietnam",
                        Rank = "Vàng",
                        LogoUrl = "/Templates/Medinova/img/vendor-1.jpg",
                        WebsiteUrl = "https://heineken.com",
                        EventId = galaEvent.Id
                    },
                    new Sponsor
                    {
                        Name = "Vietcombank",
                        Rank = "Bạc",
                        LogoUrl = "/Templates/Medinova/img/vendor-2.jpg",
                        EventId = galaEvent.Id
                    }
                );

                context.SaveChanges();
            }

            // =========================================================
            // SỰ KIỆN 6: KNIGHTONE (KINH TẾ)
            // =========================================================
            var bizCat = context.Categories.FirstOrDefault(c => c.Name == "Kinh doanh & Đầu tư");
            if (bizCat != null && !context.Events.Any(e => e.Title.Contains("Diễn đàn Kinh tế")))
            {
                var bizEvent = new Event
                {
                    Title = "Diễn đàn Kinh tế Việt Nam 2025: Tầm nhìn & Cơ hội",
                    Description = "Phân tích vĩ mô và cơ hội đầu tư Bất động sản.",
                    ImageUrl = "/Templates/Knightone/assets/img/hero-bg.jpg",
                    Location = "Khách sạn JW Marriott, Hà Nội",
                    StartDate = new DateTime(2025, 09, 15, 8, 0, 0),
                    EndDate = new DateTime(2025, 09, 15, 17, 0, 0),
                    IsActive = true,
                    CategoryId = bizCat.Id,
                    LandingPage = "KnightOne"
                };
                context.Events.Add(bizEvent);
                context.SaveChanges(); // Lưu để lấy Id cho các bảng con

                // 1. Thêm danh sách vé (TicketTypes)
                context.TicketTypes.AddRange(
                    new TicketType { Name = "Standard", Price = 2000000, Quantity = 500, EventId = bizEvent.Id },
                    new TicketType { Name = "VIP Member", Price = 5000000, Quantity = 50, EventId = bizEvent.Id }
                );

                // 2. Thêm diễn giả (Speakers)
                context.Speakers.AddRange(
                    new Speaker
                    {
                        Name = "Shark Hưng",
                        JobTitle = "Phó Chủ tịch HĐQT CenGroup",
                        ImageUrl = "/Templates/Knightone/assets/img/team/team-1.jpg",
                        SocialUrl = "https://facebook.com/sharkhung",
                        EventId = bizEvent.Id
                    },
                    new Speaker
                    {
                        Name = "GS. Đặng Hùng Võ",
                        JobTitle = "Nguyên Thứ trưởng Bộ TN&MT",
                        ImageUrl = "/Templates/Knightone/assets/img/team/team-2.jpg",
                        EventId = bizEvent.Id
                    }
                );

                // 3. Thêm lịch trình chi tiết (Schedules) - Để hiện Menu "Lịch trình"
                context.Schedules.AddRange(
                    new Schedule
                    {
                        Title = "Đón khách & Check-in",
                        StartTime = new DateTime(2025, 09, 15, 8, 0, 0),
                        EndTime = new DateTime(2025, 09, 15, 9, 0, 0),
                        Location = "Sảnh Grand Ballroom",
                        Description = "Phát tài liệu và teabreak khai vị.",
                        EventId = bizEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Phiên thảo luận vĩ mô",
                        StartTime = new DateTime(2025, 09, 15, 9, 0, 0),
                        EndTime = new DateTime(2025, 09, 15, 11, 30, 0),
                        Location = "Hội trường chính",
                        Description = "Phân tích xu hướng kinh tế toàn cầu và tác động đến Việt Nam.",
                        EventId = bizEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Networking & Buffet Trưa",
                        StartTime = new DateTime(2025, 09, 15, 12, 0, 0),
                        EndTime = new DateTime(2025, 09, 15, 13, 30, 0),
                        Location = "Nhà hàng JW",
                        Description = "Giao lưu trực tiếp cùng các nhà đầu tư.",
                        EventId = bizEvent.Id
                    }
                );

                // 4. Thêm nhà tài trợ (Sponsors) - Để hiện Menu "Đối tác"
                context.Sponsors.AddRange(
                    new Sponsor
                    {
                        Name = "CenLand",
                        Rank = "Kim cương",
                        LogoUrl = "/Templates/Knightone/assets/img/clients/client-1.png",
                        WebsiteUrl = "https://cenland.vn",
                        EventId = bizEvent.Id
                    },
                    new Sponsor
                    {
                        Name = "Techcombank",
                        Rank = "Vàng",
                        LogoUrl = "/Templates/Knightone/assets/img/clients/client-2.png",
                        EventId = bizEvent.Id
                    }
                );

                context.SaveChanges();
            }
            // =========================================================
            // SỰ KIỆN 7: MEDILAB (KHOA HỌC)
            // =========================================================
            var sciCat = context.Categories.FirstOrDefault(c => c.Name == "Khoa học & Giáo dục");
            if (sciCat != null && !context.Events.Any(e => e.Title.Contains("Đột phá Y học")))
            {
                var sciEvent = new Event
                {
                    Title = "Hội thảo Khoa học: Đột phá Y học Tái tạo 2025",
                    Description = "Tiến bộ mới nhất trong lĩnh vực tế bào gốc và ứng dụng trong điều trị lâm sàng.",
                    ImageUrl = "/Templates/Medilab/assets/img/hero-bg.jpg",
                    Location = "Đại học Y Dược TP.HCM",
                    StartDate = new DateTime(2025, 08, 20, 8, 0, 0),
                    EndDate = new DateTime(2025, 08, 20, 16, 0, 0),
                    IsActive = true,
                    CategoryId = sciCat.Id,
                    LandingPage = "Medilab"
                };
                context.Events.Add(sciEvent);
                context.SaveChanges(); // Lưu để lấy Id cho các bảng con

                // 1. Thêm loại vé (TicketTypes)
                context.TicketTypes.AddRange(
                    new TicketType { Name = "Vé Đại Biểu", Price = 500000, Quantity = 300, EventId = sciEvent.Id },
                    new TicketType { Name = "Vé Sinh Viên", Price = 100000, Quantity = 100, EventId = sciEvent.Id }
                );

                // 2. Thêm Diễn giả / Bác sĩ (Speakers)
                context.Speakers.AddRange(
                    new Speaker
                    {
                        Name = "GS.TS. Nguyễn Văn A",
                        JobTitle = "Trưởng khoa Y sinh học",
                        ImageUrl = "/Templates/Medilab/assets/img/doctors/doctors-1.jpg",
                        SocialUrl = "https://facebook.com/prof.a",
                        EventId = sciEvent.Id
                    },
                    new Speaker
                    {
                        Name = "Dr. Sarah Johnson",
                        JobTitle = "Chuyên gia Tế bào gốc từ Đại học Harvard",
                        ImageUrl = "/Templates/Medilab/assets/img/doctors/doctors-2.jpg",
                        EventId = sciEvent.Id
                    }
                );

                // 3. Thêm Lịch trình (Schedules) - Để hiện Menu "Lịch trình"
                context.Schedules.AddRange(
                    new Schedule
                    {
                        Title = "Khai mạc & Giới thiệu",
                        StartTime = new DateTime(2025, 08, 20, 8, 0, 0),
                        EndTime = new DateTime(2025, 08, 20, 9, 0, 0),
                        Location = "Hội trường chính",
                        Description = "Phát biểu khai mạc và giới thiệu chương trình.",
                        EventId = sciEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Phiên thảo luận 1: Công nghệ Tế bào",
                        StartTime = new DateTime(2025, 08, 20, 9, 15, 0),
                        EndTime = new DateTime(2025, 08, 20, 11, 45, 0),
                        Location = "Phòng Lab A",
                        Description = "Báo cáo về các đột phá mới nhất trong nuôi cấy tế bào.",
                        EventId = sciEvent.Id
                    }
                );

                // 4. Thêm Nhà tài trợ (Sponsors) - Để hiện Menu "Đối tác"
                context.Sponsors.AddRange(
                    new Sponsor
                    {
                        Name = "AstraZeneca",
                        Rank = "Vàng",
                        LogoUrl = "/Templates/Medilab/assets/img/gallery/gallery-3.jpg",
                        WebsiteUrl = "https://www.astrazeneca.com",
                        EventId = sciEvent.Id
                    },
                    new Sponsor
                    {
                        Name = "Pfizer Vietnam",
                        Rank = "Bạc",
                        LogoUrl = "/Templates/Medilab/assets/img/gallery/gallery-4.jpg",
                        EventId = sciEvent.Id
                    }
                );

                context.SaveChanges();
            }
        }
        }
}