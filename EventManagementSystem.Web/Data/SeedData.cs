using DocumentFormat.OpenXml.Bibliography;
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
        public static void Initialize(ApplicationDbContext context, string organizerId)
        {
            // Tự động Migrate database nếu có thay đổi cấu trúc
            context.Database.Migrate();

            // =========================================================
            // === TẠO DỮ LIỆU ĐỘI NGŨ PHÁT TRIỂN (TEAM MEMBERS) ===
            // =========================================================
            if (!context.TeamMembers.Any())
            {
                context.Database.ExecuteSqlRaw("TRUNCATE TABLE TeamMembers");
                context.TeamMembers.AddRange(
                    new TeamMember
                    {
                        FullName = "Nguyễn Ngọc Linh Nhi",
                        Position = "Project Leader",
                        Description = "Chịu trách nhiệm quản lý chung và thiết kế hệ thống.",
                        ImageUrl = "/img/team/member1.jpg",
                        FacebookUrl = "https://facebook.com/user1",
                        ZaloUrl = "0901234567",
                        GithubUrl = "https://github.com/NguyenNgocLinhNhi"
                    },
                    new TeamMember
                    {
                        FullName = "Hiệu Thị Sô Ny",
                        Position = "Backend Developer",
                        Description = "Chuyên gia xử lý logic nghiệp vụ và tối ưu hóa Database.",
                        ImageUrl = "/img/team/member2.jpg",
                        FacebookUrl = "https://facebook.com/user2",
                        ZaloUrl = "0907654321",
                        GithubUrl = "https://github.com/hieuthisony"
                    },
                    new TeamMember
                    {
                        FullName = "Lê Lý Kiều My",
                        Position = "Frontend Developer",
                        Description = "Thiết kế giao diện người dùng mượt mà và hiện đại.",
                        ImageUrl = "/img/team/member3.jpg",
                        FacebookUrl = "https://facebook.com/user3",
                        ZaloUrl = "0903456789",
                        GithubUrl = "https://github.com/asd12-gif"
                    },
                    new TeamMember
                    {
                        FullName = "Phùng Nhã Ái Như",
                        Position = "UI/UX Designer",
                        Description = "Xây dựng trải nghiệm người dùng và thiết kế hình ảnh sự kiện.",
                        ImageUrl = "/img/team/member4.jpg",
                        FacebookUrl = "https://facebook.com/user4",
                        ZaloUrl = "0904567890",
                        GithubUrl = "https://github.com/monligt"
                    }
                );
                context.SaveChanges();
            }

            // === TẠO THÔNG TIN TỔ CHỨC (ORG INFO) ===
            if (!context.OrganizationInfos.Any())
            {
                context.OrganizationInfos.Add(new OrganizationInfo
                {
                    OrganizationName = "UEF",
                    OrgType = "Company",
                    OrgHotline = "+84 901 234 567",
                    OrgEmail = "nhinnl22@uef.edu.vn",
                    OrgAddress = "123 Su Van Hanh Street, District 10, Ho Chi Minh City",
                    OrganizationBio = "Specializing in organizing professional seminars, workshops, and exhibitions in the beauty and health industry.",
                    AvatarUrl = "/img/logo_eventus.jpg"
                });
                context.SaveChanges();
            }

            // === 3. TẠO CẤU HÌNH HỆ THỐNG (ADMIN SYSTEM SETTINGS) ===
            if (!context.AdminSystemSettings.Any())
            {
                context.AdminSystemSettings.AddRange(
                    new AdminSystemSetting { SettingKey = "SystemName", SettingValue = "Eventus System" }, 
            
                    new AdminSystemSetting { SettingKey = "SystemDescription", SettingValue = "Comprehensive event management and ticketing platform." }, 
                    new AdminSystemSetting { SettingKey = "SystemLogoUrl", SettingValue = "/img/logo_enventus_admin.jpg" },

                    // Cấu hình Email Server (SMTP)
                    new AdminSystemSetting { SettingKey = "SmtpServer", SettingValue = "smtp.gmail.com" }, 
                    new AdminSystemSetting { SettingKey = "SmtpPort", SettingValue = "587" }, 
                    new AdminSystemSetting { SettingKey = "EnableSsl", SettingValue = "true" }, 
                    new AdminSystemSetting { SettingKey = "SmtpUser", SettingValue = "support@eventus.com" }, 
                    new AdminSystemSetting { SettingKey = "SmtpPass", SettingValue = "abcdefghijklmnop" }, 

                    // Cấu hình thời gian
                    new AdminSystemSetting { SettingKey = "DefaultTimeZone", SettingValue = "SE Asia Standard Time" }
                );
                context.SaveChanges();
            }

            // QUAN TRỌNG: Kiểm tra nếu đã có dữ liệu Booking thì KHÔNG chạy Seed để bảo vệ lịch sử vé
            if (context.Bookings.Any())
            {
                return;
            }

            // 2. CHỈ XÓA DỮ LIỆU CŨ KHI DATABASE TRỐNG (Hoặc khi bạn thực sự muốn reset mẫu)
            context.Schedules.RemoveRange(context.Schedules);
            context.Sponsors.RemoveRange(context.Sponsors);
            context.Speakers.RemoveRange(context.Speakers);
            context.BookingDetails.RemoveRange(context.BookingDetails);
            context.TicketTypes.RemoveRange(context.TicketTypes);
            context.Events.RemoveRange(context.Events);
            context.Categories.RemoveRange(context.Categories);

            context.SaveChanges();



            // === 1. TẠO DANH MỤC (CATEGORIES) ===
            if (!context.Categories.Any())
            {
                context.Categories.AddRange(
                    new Category { Name = "Medicine & Health", Description = "Medical professional workshops." },
                    new Category { Name = "Technology", Description = "Tech exhibitions, AI, Blockchain." },
                    new Category { Name = "Education", Description = "Study abroad, career orientation, and soft skills." },
                    new Category { Name = "Music & Entertainment", Description = "Concerts, EDM, Festivals." },
                    new Category { Name = "Food & Beverage", Description = "Food festivals, Wine tasting." },
                    new Category { Name = "Community & Society", Description = "Volunteering and fundraising activities." },
                    new Category { Name = "Business & Investment", Description = "Economic seminars, Real Estate." },
                    new Category { Name = "Science & Research", Description = "Specialized seminars and research presentations." }
                );
                context.SaveChanges();
            }

            // 1. TẠO DANH MỤC GIAO DIỆN (Phục vụ dropdown chọn mẫu)
            if (!context.LandingPageTemplates.Any())
            {
                context.LandingPageTemplates.AddRange(
                    new LandingPageTemplate { Id = "Charitize", Name = "Charitize - Charity", PreviewImageUrl = "/Templates/Charitize/img/carousel-1.jpg" },
                    new LandingPageTemplate { Id = "Chefer", Name = "Chefer - Culinary", PreviewImageUrl = "/Templates/Chefer/img/hero-1.jpg" },
                    new LandingPageTemplate { Id = "Medinova", Name = "Medinova - Medical", PreviewImageUrl = "/Templates/Medinova/img/hero.jpg" },
                    new LandingPageTemplate { Id = "Nova", Name = "Nova - Creative", PreviewImageUrl = "/Templates/Nova/assets/img/hero/hero-5/hero-img.svg" },
                    new LandingPageTemplate { Id = "KnightOne", Name = "KnightOne - Corporate", PreviewImageUrl = "/Templates/Knightone/assets/img/hero-bg.jpg" },
                    new LandingPageTemplate { Id = "Medilab", Name = "Medilab - Medical", PreviewImageUrl = "/Templates/Medilab/assets/img/hero-bg.jpg" },
                    new LandingPageTemplate { Id = "Yummy", Name = "Yummy - Event & Party", PreviewImageUrl = "/Templates/Yummy/assets/img/hero-img.png" }
                );
            }
           
            // =========================================================
            // SỰ KIỆN 1: MEDINOVA (Y TẾ)
            // =========================================================
            var medCat = context.Categories.FirstOrDefault(c => c.Name == "Medicine & Health");
            if (medCat != null)
            {
                var medEvent = new Event
                {
                    Title = "Medinova International Cardiology Conference 2025",
                    Description = @"<p>The conference brings together over <strong>500 leading experts</strong> to discuss breakthroughs in cardiovascular treatment.</p>",
                    ImageUrl = "/Templates/Medinova/img/hero.jpg",
                    Location = "National Convention Center, Hanoi",
                    StartDate = new DateTime(2026, 12, 20, 8, 0, 0),
                    EndDate = new DateTime(2026, 12, 20, 17, 0, 0),
                    IsActive = true,
                    CategoryId = medCat.Id,
                    OrganizerId = organizerId,
                    LandingPage = "Medinova"
                };
                context.Events.Add(medEvent);
                context.SaveChanges(); // Lưu để lấy Id cho các bảng con

                // 1. Thêm danh sách vé (TicketTypes) 
                context.TicketTypes.AddRange(
                    new TicketType { Name = "Doctor Ticket", Price = 500000, Quantity = 200, EventId = medEvent.Id },
                    new TicketType { Name = "Student Ticket", Price = 100000, Quantity = 100, EventId = medEvent.Id },
                    new TicketType { Name = "VIP (Gala Dinner)", Price = 2000000, Quantity = 50, EventId = medEvent.Id }
                );

                // 2. Thêm danh sách diễn giả (Speakers)
                context.Speakers.AddRange(
                    new Speaker
                    {
                        Name = "Dr. Sarah Smith",
                        JobTitle = "WHO Senior Expert",
                        ImageUrl = "/Templates/Medinova/img/team-2.jpg",
                        SocialUrl = "https://facebook.com/drsarah",
                        EventId = medEvent.Id
                    },
                    new Speaker
                    {
                        Name = "Assoc. Prof. Tran Van B",
                        JobTitle = "Director of Cardiology Institute",
                        ImageUrl = "/Templates/Medinova/img/team-1.jpg",
                        EventId = medEvent.Id
                    }
                );

               // 3. Thêm lịch trình chi tiết (Schedules)
                context.Schedules.AddRange(
                    new Schedule
                    {
                        Title = "Registration & Opening Ceremony",
                        StartTime = new DateTime(2026, 12, 20, 8, 0, 0),
                        EndTime = new DateTime(2026, 12, 20, 9, 0, 0),
                        Location = "Grand Hall",
                        Description = "Check-in process and distribution of seminar materials.",
                        EventId = medEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Discussion: New Cardiovascular Technology",
                        StartTime = new DateTime(2026, 12, 20, 9, 0, 0),
                        EndTime = new DateTime(2026, 12, 20, 11, 30, 0),
                        Location = "Seminar Room A1",
                        Description = "In-depth report on AI applications in cardiovascular imaging.",
                        EventId = medEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Lunch & Networking",
                        StartTime = new DateTime(2026, 12, 20, 12, 0, 0),
                        EndTime = new DateTime(2026, 12, 20, 13, 30, 0),
                        Location = "2nd Floor Restaurant Area",
                        Description = "Buffet lunch and networking session for delegates.",
                        EventId = medEvent.Id
                    }
                );

              // 4. Thêm nhà tài trợ (Sponsors) 
                context.Sponsors.AddRange(
                    new Sponsor
                    {
                        Name = "Vinmec Healthcare",
                        Rank = "Platinum",
                        LogoUrl = "/Templates/Medinova/img/vendor-1.jpg",
                        WebsiteUrl = "https://vinmec.com",
                        EventId = medEvent.Id
                    },
                    new Sponsor
                    {
                        Name = "Samsung Medical",
                        Rank = "Gold",
                        LogoUrl = "/Templates/Medinova/img/vendor-2.jpg",
                        EventId = medEvent.Id
                    }
                );

                context.SaveChanges();
            }

            // =========================================================
            // SỰ KIỆN 2: CHEFER (ẨM THỰC)
            // =========================================================
            var foodCat = context.Categories.FirstOrDefault(c => c.Name == "Food & Beverage");
            if (foodCat != null && !context.Events.Any(e => e.Title.Contains("Taste of The World")))
            {
                var chefEvent = new Event
                {
                    Title = "International Culinary Gala: Taste of The World 2026",
                    Description = "A journey to awaken all senses with 10 Michelin-starred chefs.",
                    ImageUrl = "/Templates/Chefer/img/hero-1.jpg",
                    Location = "Metropole Hotel, Hanoi",
                    StartDate = new DateTime(2026, 12, 24, 18, 0, 0),
                    EndDate = new DateTime(2026, 12, 24, 22, 30, 0),
                    IsActive = true,
                    CategoryId = foodCat.Id,
                    OrganizerId = organizerId,
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
                    new Speaker
                    {
                        Name = "Gordon Ramsay",
                        JobTitle = "Michelin Star MasterChef",
                        ImageUrl = "/Templates/Chefer/img/team-1.jpg",
                        SocialUrl = "https://facebook.com/gordonramsay",
                        EventId = chefEvent.Id
                    },
                    new Speaker
                    {
                        Name = "Alain Ducasse",
                        JobTitle = "French Culinary Legend",
                        ImageUrl = "/Templates/Chefer/img/team-2.jpg",
                        SocialUrl = "https://www.ducasse-paris.com/en",
                        EventId = chefEvent.Id
                    }
                );

                // 3. Thêm Lịch trình (Schedules) - Giúp hiện Menu "Lịch trình"
                context.Schedules.AddRange(
                    new Schedule
                    {
                        Title = "Aperitif Wine Party",
                        StartTime = new DateTime(2026, 12, 24, 18, 0, 0),
                        EndTime = new DateTime(2026, 12, 24, 19, 0, 0),
                        Location = "Main Lobby",
                        Description = "Enjoying premium wine and light appetizers.",
                        EventId = chefEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Cooking Performance",
                        StartTime = new DateTime(2026, 12, 24, 19, 0, 0),
                        EndTime = new DateTime(2026, 12, 24, 21, 0, 0),
                        Location = "Open Kitchen Area",
                        Description = "Gordon Ramsay live performing his signature Beef Wellington.",
                        EventId = chefEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Grand Gala Dinner",
                        StartTime = new DateTime(2026, 12, 24, 21, 0, 0),
                        EndTime = new DateTime(2026, 12, 24, 22, 30, 0),
                        Location = "Grand Ballroom",
                        Description = "7-course Michelin standard gourmet menu.",
                        EventId = chefEvent.Id
                    }
                );

                // 4. Thêm Nhà tài trợ (Sponsors) - Giúp hiện Menu "Đối tác"
                context.Sponsors.AddRange(
                    new Sponsor
                    {
                        Name = "Moët & Chandon",
                        Rank = "Diamond",
                        LogoUrl = "/Templates/Chefer/img/moet-logo.png",
                        WebsiteUrl = "https://www.moet.com",
                        EventId = chefEvent.Id
                    },
                    new Sponsor
                    {
                        Name = "Michelin Guide",
                        Rank = "Gold",
                        LogoUrl = "/Templates/Chefer/img/michelin-logo.png",
                        WebsiteUrl = "https://guide.michelin.com",
                        EventId = chefEvent.Id
                    }
                );

                context.SaveChanges();
            }

            // =========================================================
            // SỰ KIỆN 3: CHARITIZE (TỪ THIỆN)
            // =========================================================
            var charityCat = context.Categories.FirstOrDefault(c => c.Name == "Community & Society");
            if (charityCat != null && !context.Events.Any(e => e.Title.Contains("Run For The Future")))
            {
                var charityEvent = new Event
                {
                    Title = "Run For The Future 2026",
                    Description = "Every step counts - One hope for children's heart surgery.",
                    ImageUrl = "/Templates/Charitize/img/carousel-1.jpg",
                    Location = "Thong Nhat Park, Hanoi",
                    StartDate = new DateTime(2026, 11, 15, 6, 0, 0),
                    EndDate = new DateTime(2026, 11, 15, 11, 0, 0),
                    IsActive = true,
                    CategoryId = charityCat.Id,
                    OrganizerId = organizerId,
                    LandingPage = "Charitize"
                };
                context.Events.Add(charityEvent);
                context.SaveChanges();

                //LỊCH TRÌNH (Schedules)
                context.Schedules.AddRange(
                    new Schedule
                    {
                        Title = "Gathering & Warm-up",
                        StartTime = charityEvent.StartDate,
                        EndTime = charityEvent.StartDate.AddHours(1),
                        Location = "Main Gate",
                        Description = "Participants gather for check-in and group warm-up exercises.",
                        EventId = charityEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Race Start",
                        StartTime = charityEvent.StartDate.AddHours(1),
                        EndTime = charityEvent.EndDate ?? charityEvent.StartDate.AddHours(4),
                        Location = "5km Track",
                        Description = "The official start of the charity run for all participants.",
                        EventId = charityEvent.Id
                    }
                );

                //NHÀ TÀI TRỢ (Sponsors)
                context.Sponsors.Add(new Sponsor
                {
                    Name = "Global Philanthropist Foundation",
                    Rank = "Diamond",
                    LogoUrl = "/Templates/Charitize/img/diamond.png",
                    WebsiteUrl = "https://example.org", // Bổ sung Website mặc định
                    EventId = charityEvent.Id
                });

                //VÉ (TicketTypes)
                context.TicketTypes.Add(new TicketType
                {
                    Name = "Standard Run Kit",
                    Price = 300000,
                    Quantity = 1000,
                    EventId = charityEvent.Id
                });

                //DIỄN GIẢ/ĐẠI SỨ (Speakers)
                context.Speakers.Add(new Speaker
                {
                    Name = "H'Hen Nie",
                    JobTitle = "Ambassador",
                    ImageUrl = "/Templates/Charitize/img/team-1.jpg",
                    SocialUrl = "https://www.facebook.com/hhennie.official", // Bổ sung SocialUrl
                    EventId = charityEvent.Id
                });
                context.SaveChanges();
            }

            // =========================================================
            // EVENT 4: NOVA (TECHNOLOGY) - Updated to 2026
            // =========================================================
            var techCat = context.Categories.FirstOrDefault(c => c.Name == "Technology");
            if (techCat != null && !context.Events.Any(e => e.Title.Contains("Tech Summit")))
            {
                var novaEvent = new Event
                {
                    Title = "Vietnam Tech Summit 2026: AI & Blockchain",
                    Description = "The biggest technology summit of the year, bringing together leaders from world-leading tech corporations like OpenAI and Tesla to discuss the future of Artificial Intelligence.",
                    ImageUrl = "/Templates/Nova/assets/img/hero/hero-5/hero-img.svg",
                    Location = "GEM Center, Ho Chi Minh City",
                    StartDate = new DateTime(2026, 10, 10, 9, 0, 0),
                    EndDate = new DateTime(2026, 10, 11, 17, 0, 0),
                    IsActive = true,
                    CategoryId = techCat.Id,
                    OrganizerId = organizerId,
                    LandingPage = "Nova" // Template: Nova
                };
                context.Events.Add(novaEvent);
                context.SaveChanges();

                // 1. Ticket Types (Các loại vé)
                context.TicketTypes.AddRange(
                    new TicketType { Name = "Investor VIP", Price = 10000000, Quantity = 50, EventId = novaEvent.Id },
                    new TicketType { Name = "Standard Access", Price = 2000000, Quantity = 500, EventId = novaEvent.Id },
                    new TicketType { Name = "Student Access", Price = 500000, Quantity = 200, EventId = novaEvent.Id }
                );

                // 2. Speakers (Diễn giả)
                context.Speakers.AddRange(
                    new Speaker
                    {
                        Name = "Elon Musk",
                        JobTitle = "CEO of Tesla & SpaceX",
                        ImageUrl = "/Templates/Medinova/img/team-1.jpg",
                        SocialUrl = "https://x.com/elonmusk",
                        EventId = novaEvent.Id
                    },
                    new Speaker
                    {
                        Name = "Sam Altman",
                        JobTitle = "CEO of OpenAI",
                        ImageUrl = "/Templates/Medinova/img/team-2.jpg",
                        SocialUrl = "https://x.com/sama",
                        EventId = novaEvent.Id
                    }
                );

                // 3. Schedules (Lịch trình)
                context.Schedules.AddRange(
                    new Schedule
                    {
                        Title = "Opening Ceremony & AI Keynote",
                        StartTime = new DateTime(2026, 10, 10, 9, 0, 0),
                        EndTime = new DateTime(2026, 10, 10, 11, 0, 0),
                        Location = "Grand Ballroom",
                        Description = "Keynote speech on AI trends in the new era and its global impact.",
                        EventId = novaEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Workshop: Blockchain Applications",
                        StartTime = new DateTime(2026, 10, 10, 14, 0, 0),
                        EndTime = new DateTime(2026, 10, 10, 16, 30, 0),
                        Location = "Workshop Room 1",
                        Description = "Hands-on session for building decentralized applications.",
                        EventId = novaEvent.Id
                    }
                );

                // 4. Sponsors (Nhà tài trợ)
                context.Sponsors.AddRange(
                    new Sponsor
                    {
                        Name = "Google Cloud",
                        Rank = "Diamond", 
                        LogoUrl = "/Templates/Nova/assets/img/google-cloud.png",
                        WebsiteUrl = "https://cloud.google.com",
                        EventId = novaEvent.Id
                    },
                    new Sponsor
                    {
                        Name = "FPT Software",
                        Rank = "Gold",
                        LogoUrl = "/Templates/Nova/assets/img/fpt-software.png",
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
                    Title = "Year End Party 2026: Elite Business Gala",
                    Description = "Luxurious atmosphere, 5-star cuisine, and premium networking exclusively for the business community.",
                    ImageUrl = "/Templates/Yummy/assets/img/hero-img.png",
                    Location = "White Palace Convention Center, HCMC",
                    StartDate = new DateTime(2026, 12, 31, 19, 0, 0),
                    EndDate = new DateTime(2026, 12, 31, 23, 59, 0),
                    IsActive = true,
                    CategoryId = foodCat.Id,
                    OrganizerId = organizerId,
                    LandingPage = "Yummy"
                };
                context.Events.Add(galaEvent);
                context.SaveChanges();

                // 1. Ticket Types (Các loại vé)
                context.TicketTypes.AddRange(
                    new TicketType { Name = "Table for 10 Guests", Price = 10000000, Quantity = 50, EventId = galaEvent.Id },
                    new TicketType { Name = "Individual VIP Ticket", Price = 1500000, Quantity = 100, EventId = galaEvent.Id }
                );

                // 2. Speakers / Chefs (Diễn giả / Đầu bếp)
                context.Speakers.AddRange(
                    new Speaker
                    {
                        Name = "Gordon Ramsay",
                        JobTitle = "MasterChef Legend",
                        ImageUrl = "/Templates/Yummy/assets/img/chefs/chefs-1.jpg",
                        SocialUrl = "https://facebook.com/gordonramsay",
                        EventId = galaEvent.Id
                    },
                    new Speaker
                    {
                        Name = "Nguyen Quoc Nam",
                        JobTitle = "Executive Chef - White Palace",
                        ImageUrl = "/Templates/Yummy/assets/img/chefs/chefs-2.jpg",
                        EventId = galaEvent.Id
                    }
                );

                // 3. Schedules (Lịch trình) - Cập nhật đồng bộ 2026
                context.Schedules.AddRange(
                    new Schedule
                    {
                        Title = "Welcome Reception & Wine Party",
                        StartTime = new DateTime(2026, 12, 31, 19, 0, 0),
                        EndTime = new DateTime(2026, 12, 31, 20, 0, 0),
                        Location = "Diamond Lounge",
                        Description = "Champagne reception and networking for early arrivals.",
                        EventId = galaEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Opening Ceremony & Culinary Show",
                        StartTime = new DateTime(2026, 12, 31, 20, 0, 0),
                        EndTime = new DateTime(2026, 12, 31, 21, 30, 0),
                        Location = "Grand Ballroom",
                        Description = "Opening speech and live cooking performance by world-class masterchefs.",
                        EventId = galaEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Gala Dinner & Countdown",
                        StartTime = new DateTime(2026, 12, 31, 21, 30, 0),
                        EndTime = new DateTime(2026, 12, 31, 23, 59, 0),
                        Location = "Grand Ballroom",
                        Description = "Enjoying a premium 7-course menu and the New Year countdown.",
                        EventId = galaEvent.Id
                    }
                );

                // 4. Sponsors (Nhà tài trợ)
                context.Sponsors.AddRange(
                    new Sponsor
                    {
                        Name = "Heineken Vietnam",
                        Rank = "Gold",
                        LogoUrl = "/Templates/Yummy/assets/img/heineken.png",
                        WebsiteUrl = "https://heineken.com",
                        EventId = galaEvent.Id
                    },
                    new Sponsor
                    {
                        Name = "Vietcombank",
                        Rank = "Silver", // Chuyển Bạc -> Silver
                        LogoUrl = "/Templates/Yummy/assets/img/vietcombank.png",
                        WebsiteUrl = "https://www.vietcombank.com.vn",
                        EventId = galaEvent.Id
                    }
                );

                context.SaveChanges();
            }

            // =========================================================
            // SỰ KIỆN 6: KNIGHTONE (KINH TẾ)
            // =========================================================
            var bizCat = context.Categories.FirstOrDefault(c => c.Name == "Business & Investment");

            if (bizCat != null && !context.Events.Any(e => e.Title.Contains("Economic Forum")))
            {
                var bizEvent = new Event
                {
                    Title = "Vietnam Economic Forum 2026: Vision & Opportunities",
                    Description = "Macroeconomic analysis and Real Estate investment opportunities in the new era.",
                    ImageUrl = "/Templates/Knightone/assets/img/hero-bg.jpg",
                    Location = "JW Marriott Hotel, Hanoi",
                    StartDate = new DateTime(2026, 09, 15, 8, 0, 0),
                    EndDate = new DateTime(2026, 09, 15, 17, 0, 0),
                    IsActive = true,
                    CategoryId = bizCat.Id,
                    OrganizerId = organizerId,
                    LandingPage = "KnightOne"
                };
                context.Events.Add(bizEvent);
                context.SaveChanges();

                // 1. Ticket Types (Các loại vé)
                context.TicketTypes.AddRange(
                    new TicketType { Name = "Standard", Price = 2000000, Quantity = 500, EventId = bizEvent.Id },
                    new TicketType { Name = "VIP Member", Price = 5000000, Quantity = 50, EventId = bizEvent.Id }
                );

                // 2. Speakers (Diễn giả)
                context.Speakers.AddRange(
                    new Speaker
                    {
                        Name = "Shark Hung",
                        JobTitle = "Vice Chairman of CenGroup",
                        ImageUrl = "/Templates/Knightone/assets/img/team/team-1.jpg",
                        SocialUrl = "https://facebook.com/sharkhung",
                        EventId = bizEvent.Id
                    },
                    new Speaker
                    {
                        Name = "Prof. Dang Hung Vo",
                        JobTitle = "Former Deputy Minister of Natural Resources and Environment",
                        ImageUrl = "/Templates/Knightone/assets/img/team/team-2.jpg",
                        SocialUrl = "https://example.com", 
                        EventId = bizEvent.Id
                    }
                );

                // 3. Schedules (Lịch trình) - Cập nhật đồng bộ 2026
                context.Schedules.AddRange(
                    new Schedule
                    {
                        Title = "Guest Welcome & Check-in",
                        StartTime = new DateTime(2026, 09, 15, 8, 0, 0),
                        EndTime = new DateTime(2026, 09, 15, 9, 0, 0),
                        Location = "Grand Ballroom Lobby",
                        Description = "Material distribution and morning tea break.",
                        EventId = bizEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Macroeconomic Discussion Session",
                        StartTime = new DateTime(2026, 09, 15, 9, 0, 0),
                        EndTime = new DateTime(2026, 09, 15, 11, 30, 0),
                        Location = "Main Hall",
                        Description = "Analyzing global economic trends and their specific impact on Vietnam.",
                        EventId = bizEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Networking & Buffet Lunch",
                        StartTime = new DateTime(2026, 09, 15, 12, 0, 0),
                        EndTime = new DateTime(2026, 09, 15, 13, 30, 0),
                        Location = "JW Restaurant",
                        Description = "Direct networking opportunity with top investors and experts.",
                        EventId = bizEvent.Id
                    }
                );

                // 4. Sponsors (Nhà tài trợ)
                context.Sponsors.AddRange(
                    new Sponsor
                    {
                        Name = "CenLand",
                        Rank = "Diamond", 
                        LogoUrl = "/Templates/Knightone/assets/img/clients/client-1.png",
                        WebsiteUrl = "https://cenland.vn",
                        EventId = bizEvent.Id
                    },
                    new Sponsor
                    {
                        Name = "Techcombank",
                        Rank = "Gold", 
                        LogoUrl = "/Templates/Knightone/assets/img/clients/client-2.png",
                        WebsiteUrl = "https://www.techcombank.com",
                        EventId = bizEvent.Id
                    }
                );

                context.SaveChanges();
            }

            // =========================================================
            // SỰ KIỆN 7: MEDILAB (KHOA HỌC)
            // =========================================================
            var sciCat = context.Categories.FirstOrDefault(c => c.Name == "Science & Research");

            if (sciCat != null && !context.Events.Any(e => e.Title.Contains("Medical Breakthroughs")))
            {
                var sciEvent = new Event
                {
                    Title = "Scientific Seminar: Regenerative Medicine Breakthroughs 2026",
                    Description = "The latest progress in stem cell research and its clinical applications in modern treatment.",
                    ImageUrl = "/Templates/Medilab/assets/img/hero-bg.jpg",
                    Location = "University of Medicine and Pharmacy, HCMC",
                    StartDate = new DateTime(2026, 08, 20, 8, 0, 0),
                    EndDate = new DateTime(2026, 08, 20, 16, 0, 0),
                    IsActive = true,
                    CategoryId = sciCat.Id,
                    OrganizerId = organizerId,
                    LandingPage = "Medilab"
                };
                context.Events.Add(sciEvent);
                context.SaveChanges();

                // 1. Ticket Types (Các loại vé)
                context.TicketTypes.AddRange(
                    new TicketType { Name = "Delegate Ticket", Price = 500000, Quantity = 300, EventId = sciEvent.Id },
                    new TicketType { Name = "Student Ticket", Price = 100000, Quantity = 100, EventId = sciEvent.Id }
                );

                // 2. Speakers / Doctors (Diễn giả / Bác sĩ)
                context.Speakers.AddRange(
                    new Speaker
                    {
                        Name = "Prof. Nguyen Van A",
                        JobTitle = "Head of Biomedical Department",
                        ImageUrl = "/Templates/Medilab/assets/img/doctors/doctors-1.jpg",
                        SocialUrl = "https://facebook.com/prof.a",
                        EventId = sciEvent.Id
                    },
                    new Speaker
                    {
                        Name = "Dr. Sarah Johnson",
                        JobTitle = "Stem Cell Specialist from Harvard University",
                        ImageUrl = "/Templates/Medilab/assets/img/doctors/doctors-2.jpg",
                        SocialUrl = "https://linkedin.com/in/drsarah",
                        EventId = sciEvent.Id
                    }
                );

                // 3. Schedules (Lịch trình)
                context.Schedules.AddRange(
                    new Schedule
                    {
                        Title = "Opening Ceremony & Introduction",
                        StartTime = new DateTime(2026, 08, 20, 8, 0, 0),
                        EndTime = new DateTime(2026, 08, 20, 9, 0, 0),
                        Location = "Grand Auditorium",
                        Description = "Opening speech and program introduction by the organizing committee.",
                        EventId = sciEvent.Id
                    },
                    new Schedule
                    {
                        Title = "Session 1: Cell Technology",
                        StartTime = new DateTime(2026, 08, 20, 9, 15, 0),
                        EndTime = new DateTime(2026, 08, 20, 11, 45, 0),
                        Location = "Lab Room A",
                        Description = "In-depth report on the latest breakthroughs in cell culture and engineering.",
                        EventId = sciEvent.Id
                    }
                );

                // 4. Sponsors (Nhà tài trợ)
                context.Sponsors.AddRange(
                    new Sponsor
                    {
                        Name = "AstraZeneca",
                        Rank = "Gold",
                        LogoUrl = "/Templates/Medilab/assets/img/gallery/gallery-3.jpg",
                        WebsiteUrl = "https://www.astrazeneca.com",
                        EventId = sciEvent.Id
                    },
                    new Sponsor
                    {
                        Name = "Pfizer Vietnam",
                        Rank = "Silver", 
                        LogoUrl = "/Templates/Medilab/assets/img/gallery/gallery-4.jpg",
                        WebsiteUrl = "https://www.pfizer.com", 
                        EventId = sciEvent.Id
                    }
                );

                context.SaveChanges();
            }
            
        }
    }
}