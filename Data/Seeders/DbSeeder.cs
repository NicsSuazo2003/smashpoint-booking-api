// Data/Seeders/DbSeeder.cs
using SmashPointBookingAPI.Entities;

namespace SmashPointBookingAPI.Data;

public static class DbSeeder
{
    public static void Initialize(AppDbContext db)
    {
        if (!db.Clients.Any())
        {
            var client = new Client
            {
                Id = Guid.NewGuid(),
                Name = "Smash Point Pickle Sports Center",
                Subdomain = "smashpoint",
                LogoUrl = null,
                PrimaryColor = "#2563EB",
                AccentColor = "#A3E635",
                GcashNumber = "09XX XXX XXXX", // TODO: fill in real GCash
                GcashAccountName = "Smash Point Pickle Sports Center",
                CreatedAt = DateTime.UtcNow,
                Status = "active",
                AvailableAmenitiesJson = null,
            };

            db.Clients.Add(client);
            db.SaveChanges();

            var clientId = client.Id;

            if (!db.Courts.Any())
            {
                var courts = new List<Court>
                {
                    new Court
                    {
                        Id = Guid.NewGuid(),
                        ClientId = clientId,
                        Name = "Court 1",
                        Type = "indoor",
                        Indoor = true,
                        PricePerHour = 400,
                        PeakPricePerHour = 500,
                        Description = "Premium indoor court with professional-grade flooring",
                        ImageUrl = "https://images.unsplash.com/photo-1626224583764-f87db24ac4ea",
                        ImagesRaw = "https://images.unsplash.com/photo-1626224583764-f87db24ac4ea",
                        AmenitiesRaw = "WiFi,Air Conditioning,Lighting,Showers",
                        Rating = 4.8,
                        Status = "active",
                        OpenTime = new TimeOnly(7, 0),
                        CloseTime = new TimeOnly(0, 0),
                        Dimensions = "44ft x 20ft",
                        Surface = "Cushion",
                    },
                    new Court
                    {
                        Id = Guid.NewGuid(),
                        ClientId = clientId,
                        Name = "Court 2",
                        Type = "indoor",
                        Indoor = true,
                        PricePerHour = 400,
                        PeakPricePerHour = 500,
                        Description = "Tournament-ready indoor court",
                        ImageUrl = "https://images.unsplash.com/photo-1622163642998-1ea32b0bbc67",
                        ImagesRaw = "https://images.unsplash.com/photo-1622163642998-1ea32b0bbc67",
                        AmenitiesRaw = "WiFi,Air Conditioning,Lighting,Showers",
                        Rating = 4.7,
                        Status = "active",
                        OpenTime = new TimeOnly(7, 0),
                        CloseTime = new TimeOnly(0, 0),
                        Dimensions = "44ft x 20ft",
                        Surface = "Cushion",
                    },
                    new Court
                    {
                        Id = Guid.NewGuid(),
                        ClientId = clientId,
                        Name = "Court 3",
                        Type = "indoor",
                        Indoor = true,
                        PricePerHour = 500,
                        PeakPricePerHour = 600,
                        Description = "Flagship court with premium lighting",
                        ImageUrl = "https://images.unsplash.com/photo-1554068865-24cecd4e34b8",
                        ImagesRaw = "https://images.unsplash.com/photo-1554068865-24cecd4e34b8",
                        AmenitiesRaw = "WiFi,Air Conditioning,Lighting,Showers,Pro Shop",
                        Rating = 4.9,
                        Status = "active",
                        OpenTime = new TimeOnly(7, 0),
                        CloseTime = new TimeOnly(0, 0),
                        Dimensions = "44ft x 20ft",
                        Surface = "Premium Cushion",
                    },
                };

                db.Courts.AddRange(courts);
                db.SaveChanges();
            }

            if (!db.Users.Any())
            {
                var admin = new User
                {
                    Id = Guid.NewGuid(),
                    Email = "admin@smashpoint.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Name = "Admin User",
                    Role = "admin",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    ClientId = clientId,
                };

                db.Users.Add(admin);
                db.SaveChanges();
            }
        }
    }
}