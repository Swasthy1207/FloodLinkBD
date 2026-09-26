using FloodLink.Models;
using Microsoft.AspNetCore.Identity;

namespace FloodLink.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles =
            {
                "Super Admin",
                "Organization Admin",
                "Coordinator",
                "Volunteer",
                "Community Reporter",
                "Citizen",
                "Admin",
                "Organization"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
            string defaultAdminEmail = "superadmin@floodlink.com";
            var existingAdmin = await userManager.FindByEmailAsync(defaultAdminEmail);

            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = defaultAdminEmail,
                    Email = defaultAdminEmail,
                    PhoneNumber = "01700000000",
                    FullName = "System Super Admin",
                    Status = "Active",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(adminUser, "Admin@123456");

                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Super Admin");
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    await userManager.AddClaimAsync(adminUser, new System.Security.Claims.Claim("FullName", adminUser.FullName));
                }
            }

            // Seed default Community Reporter user
            string defaultReporterEmail = "reporter@floodlink.org";
            var existingReporter = await userManager.FindByEmailAsync(defaultReporterEmail);
            if (existingReporter == null)
            {
                var reporterUser = new ApplicationUser
                {
                    UserName = defaultReporterEmail,
                    Email = defaultReporterEmail,
                    PhoneNumber = "01700000001",
                    FullName = "Local Reporter",
                    Status = "Active",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createRes = await userManager.CreateAsync(reporterUser, "Reporter@123");
                if (createRes.Succeeded)
                {
                    await userManager.AddToRoleAsync(reporterUser, "Community Reporter");
                    await userManager.AddClaimAsync(reporterUser, new System.Security.Claims.Claim("FullName", reporterUser.FullName));
                }
            }
        }
    }
}