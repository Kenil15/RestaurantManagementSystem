using ForUpworkRestaurentManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ForUpworkRestaurentManagement.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created
            await context.Database.MigrateAsync();

            // Seed roles
            string[] roleNames = { "Admin", "Customer" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed admin user
            var adminUser = await userManager.FindByEmailAsync("admin@restaurant.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@restaurant.com",
                    Email = "admin@restaurant.com",
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(adminUser, "Admin123!");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // Seed categories
            if (!context.Categories.Any())
            {
                var categories = new[]
                {
                    new Category { Name = "Starters", Description = "Appetizers and starters" },
                    new Category { Name = "Main Course", Description = "Main dishes" },
                    new Category { Name = "Desserts", Description = "Sweet treats" },
                    new Category { Name = "Beverages", Description = "Drinks and beverages" }
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // Seed restaurants (ensure at least two) with expanded fields
            if (!context.Restaurants.Any())
            {
                var restaurants = new[]
                {
                    new Restaurant
                    {
                        Name = "Spice Garden",
                        Location = "Central Plaza",
                        Address = "123 Main St",
                        Contact = "+1-555-1234",
                        Description = "Indian & fusion cuisine",
                        ImageUrl = "/images/restaurants/spice-garden.jpg",
                        OpeningTime = new TimeSpan(10,0,0),
                        ClosingTime = new TimeSpan(22,0,0),
                        IsActive = true
                    },
                    new Restaurant
                    {
                        Name = "Pasta Palace",
                        Location = "Riverside Mall",
                        Address = "45 River Rd",
                        Contact = "+1-555-9876",
                        Description = "Authentic Italian pastas & more",
                        ImageUrl = "/images/restaurants/pasta-palace.jpg",
                        OpeningTime = new TimeSpan(11,0,0),
                        ClosingTime = new TimeSpan(23,0,0),
                        IsActive = true
                    }
                };
                await context.Restaurants.AddRangeAsync(restaurants);
                await context.SaveChangesAsync();
            }

            // Seed menu items
            if (!context.MenuItems.Any())
            {
                var r1 = await context.Restaurants.OrderBy(r => r.Id).FirstAsync();
                var r2 = await context.Restaurants.OrderBy(r => r.Id).Skip(1).FirstAsync();

                var categories = await context.Categories.ToListAsync();
                var starters = categories.First(c => c.Name == "Starters");
                var mainCourse = categories.First(c => c.Name == "Main Course");
                var desserts = categories.First(c => c.Name == "Desserts");
                var beverages = categories.First(c => c.Name == "Beverages");

                var menuItems = new[]
                {
                    new MenuItem
                    {
                        Name = "Garlic Bread",
                        Description = "Freshly baked bread with garlic butter",
                        Price = 5.99m,
                        CategoryId = starters.Id,
                        ImageUrl = "/images/garlic-bread.jpg",
                        IsAvailable = true,
                        RestaurantId = r1.Id
                    },
                    new MenuItem
                    {
                        Name = "Caesar Salad",
                        Description = "Fresh romaine lettuce with Caesar dressing",
                        Price = 8.99m,
                        CategoryId = starters.Id,
                        ImageUrl = "/images/caesar-salad.jpg",
                        IsAvailable = false,
                        RestaurantId = r1.Id
                    },
                    new MenuItem
                    {
                        Name = "Grilled Chicken",
                        Description = "Juicy grilled chicken with herbs",
                        Price = 16.99m,
                        CategoryId = mainCourse.Id,
                        ImageUrl = "/images/grilled-chicken.jpg",
                        IsAvailable = true,
                        RestaurantId = r2.Id
                    },
                    new MenuItem
                    {
                        Name = "Beef Burger",
                        Description = "Classic beef burger with fries",
                        Price = 14.99m,
                        CategoryId = mainCourse.Id,
                        ImageUrl = "/images/beef-burger.jpg",
                        IsAvailable = true,
                        RestaurantId = r2.Id
                    },
                    new MenuItem
                    {
                        Name = "Chocolate Cake",
                        Description = "Rich chocolate cake with ganache",
                        Price = 7.99m,
                        CategoryId = desserts.Id,
                        ImageUrl = "/images/chocolate-cake.jpg",
                        IsAvailable = true,
                        RestaurantId = r1.Id
                    },
                    new MenuItem
                    {
                        Name = "Soft Drink",
                        Description = "Choice of Coke, Pepsi or Sprite",
                        Price = 2.99m,
                        CategoryId = beverages.Id,
                        ImageUrl = "/images/soft-drink.jpg",
                        IsAvailable = true,
                        RestaurantId = r1.Id
                    },
                    new MenuItem
                    {
                        Name = "Margherita Pizza",
                        Description = "Classic pizza with tomatoes, mozzarella, and basil",
                        Price = 11.99m,
                        CategoryId = mainCourse.Id,
                        ImageUrl = "/images/margherita.jpg",
                        IsAvailable = true,
                        RestaurantId = r1.Id // Added RestaurantId
                    },
                    new MenuItem
                    {
                        Name = "Panna Cotta",
                        Description = "Creamy Italian dessert with berry coulis",
                        Price = 6.49m,
                        CategoryId = desserts.Id,
                        ImageUrl = "/images/panna-cotta.jpg",
                        IsAvailable = false,
                        RestaurantId = r2.Id // Added RestaurantId
                    }
                };

                await context.MenuItems.AddRangeAsync(menuItems);
                await context.SaveChangesAsync();
            }

            // Assign RestaurantId for legacy items if missing
            var firstRestaurant = await context.Restaurants.OrderBy(r => r.Id).FirstOrDefaultAsync();
            if (firstRestaurant != null)
            {
                var orphanItems = await context.MenuItems.Where(m => m.RestaurantId == null).ToListAsync();
                if (orphanItems.Any())
                {
                    foreach (var mi in orphanItems)
                    {
                        mi.RestaurantId = firstRestaurant.Id;
                    }
                    await context.SaveChangesAsync();
                }

                // Ensure SlotCapacity has a sane default
                var restaurantsToUpdate = await context.Restaurants.Where(r => r.SlotCapacity <= 0).ToListAsync();
                if (restaurantsToUpdate.Any())
                {
                    foreach (var r in restaurantsToUpdate)
                    {
                        r.SlotCapacity = 10;
                    }
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}