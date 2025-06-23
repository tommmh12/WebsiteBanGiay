using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

namespace WebsiteBanGiay.Models
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Đảm bảo database được tạo
            context.Database.EnsureCreated();

            // Tạo các role nếu chưa tồn tại
            string[] roles = { "Admin", "Staff", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Tạo tài khoản Admin mẫu
            var adminEmail = "admin@giaydep.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "System",
                    Address = "123 Admin Street",
                    DefaultRole = "Admin" // Ghi đè role mặc định
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, adminUser.DefaultRole);
                }
            }

            // Tạo tài khoản Staff mẫu
            var staffEmail = "staff@giaydep.com";
            var staffUser = await userManager.FindByEmailAsync(staffEmail);
            if (staffUser == null)
            {
                staffUser = new ApplicationUser
                {
                    UserName = staffEmail,
                    Email = staffEmail,
                    FirstName = "Staff",
                    LastName = "Member",
                    Address = "456 Staff Avenue",
                    DefaultRole = "Staff" // Ghi đè role mặc định
                };

                var result = await userManager.CreateAsync(staffUser, "Staff@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(staffUser, staffUser.DefaultRole);
                }
            }
        }
    }
}