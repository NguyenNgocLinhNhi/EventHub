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
                    Description = @"<p>Hội nghị quy tụ hơn <strong>500 chuyên gia đầu ngành</strong>.</p>",
                    ImageUrl = "/Templates/Medinova/img/hero.jpg",
                    Location = "Trung tâm Hội nghị Quốc gia, Hà Nội",
                    StartDate = new DateTime(2025, 12, 20, 8, 0, 0),
                    EndDate = new DateTime(2025, 12, 20, 17, 0, 0),
                    IsActive = true,
                    CategoryId = medCat.Id,
                    LandingPage = "Medinova"
                };
                context.Events.Add(medEvent);
                context.SaveChanges();

                context.TicketTypes.Add(new TicketType { Name = "Vé Bác Sĩ", Price = 500000, Quantity = 200, EventId = medEvent.Id });
                context.Speakers.Add(new Speaker { Name = "Dr. Sarah Smith", JobTitle = "Chuyên gia WHO", ImageUrl = "/Templates/Medinova/img/team-2.jpg", EventId = medEvent.Id });
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
                context.SaveChanges();

                context.TicketTypes.Add(new TicketType { Name = "Diamond VIP", Price = 10000000, Quantity = 20, EventId = chefEvent.Id });
                context.Speakers.Add(new Speaker { Name = "Gordon Ramsay", JobTitle = "Siêu Đầu Bếp", ImageUrl = "/Templates/Chefer/img/team-1.jpg", EventId = chefEvent.Id });
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
                    Description = "Hội thảo công nghệ lớn nhất năm quy tụ lãnh đạo OpenAI và Tesla.",
                    ImageUrl = "/Templates/Nova/assets/img/hero/hero-5/hero-img.svg",
                    Location = "GEM Center, TP.HCM",
                    StartDate = new DateTime(2025, 10, 10, 9, 0, 0),
                    EndDate = new DateTime(2025, 10, 11, 17, 0, 0),
                    IsActive = true,
                    CategoryId = techCat.Id,
                    LandingPage = "Nova"
                };
                context.Events.Add(novaEvent);
                context.SaveChanges();

                context.TicketTypes.Add(new TicketType { Name = "Investor VIP", Price = 10000000, Quantity = 50, EventId = novaEvent.Id });
                context.Speakers.Add(new Speaker { Name = "Elon Musk", JobTitle = "CEO Tesla", ImageUrl = "https://example.com/musk.jpg", EventId = novaEvent.Id });
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
                    Description = "Không gian sang trọng, ẩm thực 5 sao và networking đỉnh cao.",
                    ImageUrl = "/Templates/Yummy/assets/img/hero-img.png",
                    Location = "White Palace, TP.HCM",
                    StartDate = new DateTime(2025, 12, 31, 19, 0, 0),
                    EndDate = new DateTime(2025, 12, 31, 23, 59, 0),
                    IsActive = true,
                    CategoryId = foodCat.Id,
                    LandingPage = "Yummy"
                };
                context.Events.Add(galaEvent);
                context.SaveChanges();

                context.TicketTypes.Add(new TicketType { Name = "Bàn Tiệc 10 Người", Price = 10000000, Quantity = 50, EventId = galaEvent.Id });
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
                    Location = "JW Marriott, Hà Nội",
                    StartDate = new DateTime(2025, 09, 15, 8, 0, 0),
                    EndDate = new DateTime(2025, 09, 15, 17, 0, 0),
                    IsActive = true,
                    CategoryId = bizCat.Id,
                    LandingPage = "KnightOne"
                };
                context.Events.Add(bizEvent);
                context.SaveChanges();

                context.TicketTypes.Add(new TicketType { Name = "Standard", Price = 2000000, Quantity = 500, EventId = bizEvent.Id });
                context.Speakers.Add(new Speaker { Name = "Shark Hưng", JobTitle = "CenGroup", ImageUrl = "/Templates/Knightone/assets/img/team/team-1.jpg", EventId = bizEvent.Id });
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
                    Description = "Tiến bộ mới nhất trong lĩnh vực tế bào gốc.",
                    ImageUrl = "/Templates/Medilab/assets/img/hero-bg.jpg",
                    Location = "Đại học Y Dược TP.HCM",
                    StartDate = new DateTime(2025, 08, 20, 8, 0, 0),
                    EndDate = new DateTime(2025, 08, 20, 16, 0, 0),
                    IsActive = true,
                    CategoryId = sciCat.Id,
                    LandingPage = "Medilab"
                };
                context.Events.Add(sciEvent);
                context.SaveChanges();

                context.TicketTypes.Add(new TicketType { Name = "Vé Đại Biểu", Price = 500000, Quantity = 300, EventId = sciEvent.Id });
                context.Sponsors.Add(new Sponsor { Name = "AstraZeneca", Rank = "Vàng", LogoUrl = "/Templates/Medilab/assets/img/gallery/gallery-3.jpg", EventId = sciEvent.Id });
                context.SaveChanges();
            }
            context.SaveChanges();
        }
    }
}