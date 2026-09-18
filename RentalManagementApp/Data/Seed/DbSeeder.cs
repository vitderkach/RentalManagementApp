using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalManagementApp.Data.Entities;
using RentalManagementApp.Data.Enums;

namespace RentalManagementApp.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        var unitTypes = await SeedUnitTypesAsync(db);
        var managers = await SeedUsersAsync(userManager, Roles.PropertyManager, "manager", 3);
        var applicants = await SeedUsersAsync(userManager, Roles.Applicant, "applicant", 10);

        var properties = await SeedPropertiesAndUnitsAsync(db, managers, unitTypes);
        await SeedApplicationsAsync(db, applicants, properties);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { Roles.Applicant, Roles.PropertyManager })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task<List<UnitType>> SeedUnitTypesAsync(ApplicationDbContext db)
    {
        var seedTypes = new (string Name, bool IsActive)[]
        {
            ("Studio", true),
            ("One Bedroom", true),
            ("Two Bedroom", true),
            ("Three Bedroom", true),
            ("Penthouse", true),
            ("Loft (Discontinued)", false)
        };

        foreach (var (name, isActive) in seedTypes)
        {
            if (!await db.UnitTypes.AnyAsync(t => t.Name == name))
            {
                db.UnitTypes.Add(new UnitType { Name = name, IsActive = isActive });
            }
        }

        await db.SaveChangesAsync();
        return await db.UnitTypes.ToListAsync();
    }

    private static async Task<List<ApplicationUser>> SeedUsersAsync(
        UserManager<ApplicationUser> userManager, string role, string emailPrefix, int count)
    {
        var faker = new Faker("en");
        var result = new List<ApplicationUser>();

        for (var i = 1; i <= count; i++)
        {
            var email = $"{emailPrefix}{i}@rentalapp.test";
            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                if (!await userManager.IsInRoleAsync(existing, role))
                {
                    await userManager.AddToRoleAsync(existing, role);
                }

                result.Add(existing);
                continue;
            }

            var firstName = faker.Name.FirstName();
            var lastName = faker.Name.LastName();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = faker.Phone.PhoneNumber("##########")
            };

            var createResult = await userManager.CreateAsync(user, "Passw0rd!2024");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
                result.Add(user);
            }
        }

        return result;
    }

    private static async Task<List<Property>> SeedPropertiesAndUnitsAsync(
        ApplicationDbContext db, List<ApplicationUser> managers, List<UnitType> unitTypes)
    {
        var faker = new Faker("en");
        var activeTypes = unitTypes.Where(t => t.IsActive).ToList();
        var properties = await db.Properties.Include(p => p.Units).ToListAsync();

        var propertyNames = new[]
        {
            "Maple Grove Apartments", "Riverside Commons", "Sunset Terrace",
            "Oakwood Residences", "Harbor View Flats", "Willow Creek Village"
        };

        for (var i = 0; i < propertyNames.Length; i++)
        {
            var property = properties.FirstOrDefault(p => p.Name == propertyNames[i]);
            if (property is null)
            {
                var manager = managers[i % managers.Count];
                property = new Property
                {
                    Name = propertyNames[i],
                    AddressLine1 = faker.Address.StreetAddress(),
                    City = faker.Address.City(),
                    State = faker.Address.StateAbbr(),
                    ZipCode = faker.Address.ZipCode(),
                    PropertyManagerId = manager.Id
                };
                properties.Add(property);
                db.Properties.Add(property);
            }

            if (property.Units.Any())
            {
                continue;
            }

            for (var u = 1; u <= 6; u++)
            {
                var type = activeTypes[(i + u - 1) % activeTypes.Count];
                var bedrooms = type.Name switch
                {
                    "Studio" => 0,
                    "One Bedroom" => 1,
                    "Two Bedroom" => 2,
                    "Three Bedroom" => 3,
                    _ => 2
                };

                property.Units.Add(new Unit
                {
                    UnitNumber = $"{i + 1}{u:00}",
                    Bedrooms = bedrooms,
                    MonthlyRent = 950 + ((i * 6 + u) * 125),
                    UnitTypeId = type.Id
                });
            }
        }

        await db.SaveChangesAsync();
        return properties;
    }

    private static async Task SeedApplicationsAsync(
        ApplicationDbContext db, List<ApplicationUser> applicants, List<Property> properties)
    {
        var faker = new Faker("en");
        var units = properties.SelectMany(p => p.Units).ToList();
        var statuses = new[]
        {
            ApplicationStatus.Draft, ApplicationStatus.Submitted, ApplicationStatus.Returned,
            ApplicationStatus.Approved, ApplicationStatus.Denied, ApplicationStatus.Withdrawn
        };

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var activeLeaseUnitIds = await db.Leases
            .Where(lease => lease.StartDate <= today && lease.EndDate >= today)
            .Select(lease => lease.UnitId)
            .ToListAsync();
        var availableUnits = units.Where(unit => !activeLeaseUnitIds.Contains(unit.Id)).ToList();
        var unitIndex = 0;

        for (var i = 0; i < statuses.Length; i++)
        {
            var status = statuses[i];
            if (await db.RentalApplications.AnyAsync(application => application.Status == status))
            {
                continue;
            }

            var applicant = applicants[i % applicants.Count];
            var unit = availableUnits[unitIndex++ % availableUnits.Count];

            var application = new RentalApplication
            {
                UnitId = unit.Id,
                ApplicantId = applicant.Id,
                Status = status,
                CreatedAt = now.AddDays(-30 + i),
                UpdatedAt = now.AddDays(-25 + i),
                ApplicantFirstName = applicant.FirstName,
                ApplicantLastName = applicant.LastName,
                ApplicantPhone = faker.Phone.PhoneNumber("##########"),
                ApplicantEmail = applicant.Email!,
                CurrentAddress = faker.Address.FullAddress(),
                DesiredLeaseStartDate = DateOnly.FromDateTime(now.AddDays(faker.Random.Int(7, 45))),
                ApplicantInfoCompleted = status != ApplicationStatus.Draft || faker.Random.Bool(),
                ResidenceHistoryCompleted = status != ApplicationStatus.Draft
            };

            application.Residences.Add(new Residence
            {
                Address = faker.Address.FullAddress(),
                LandlordName = faker.Name.FullName(),
                LandlordPhone = faker.Phone.PhoneNumber("##########"),
                MoveInDate = DateOnly.FromDateTime(now.AddYears(-3)),
                MoveOutDate = DateOnly.FromDateTime(now.AddYears(-1))
            });

            application.StatusHistory.Add(new ApplicationStatusHistory
            {
                Status = ApplicationStatus.Draft,
                ChangedByUserId = applicant.Id,
                ChangedAt = application.CreatedAt,
                Comment = "Application created."
            });

            if (status != ApplicationStatus.Draft)
            {
                application.StatusHistory.Add(new ApplicationStatusHistory
                {
                    Status = ApplicationStatus.Submitted,
                    ChangedByUserId = applicant.Id,
                    ChangedAt = application.CreatedAt.AddDays(1),
                    Comment = "Application submitted."
                });
            }

            var manager = properties.First(p => p.Units.Any(u => u.Id == unit.Id)).PropertyManagerId;

            switch (status)
            {
                case ApplicationStatus.Returned:
                    application.StatusHistory.Add(new ApplicationStatusHistory
                    {
                        Status = ApplicationStatus.Returned,
                        ChangedByUserId = manager,
                        ChangedAt = application.CreatedAt.AddDays(2),
                        Comment = "Please clarify your residence history dates."
                    });
                    break;
                case ApplicationStatus.Denied:
                    application.StatusHistory.Add(new ApplicationStatusHistory
                    {
                        Status = ApplicationStatus.Denied,
                        ChangedByUserId = manager,
                        ChangedAt = application.CreatedAt.AddDays(2),
                        Comment = "Insufficient income verification."
                    });
                    break;
                case ApplicationStatus.Withdrawn:
                    application.StatusHistory.Add(new ApplicationStatusHistory
                    {
                        Status = ApplicationStatus.Withdrawn,
                        ChangedByUserId = applicant.Id,
                        ChangedAt = application.CreatedAt.AddDays(2),
                        Comment = "Applicant withdrew the application."
                    });
                    break;
                case ApplicationStatus.Approved:
                    application.StatusHistory.Add(new ApplicationStatusHistory
                    {
                        Status = ApplicationStatus.Approved,
                        ChangedByUserId = manager,
                        ChangedAt = application.CreatedAt.AddDays(2),
                        Comment = "Approved. Lease issued."
                    });
                    break;
            }

            db.RentalApplications.Add(application);
            await db.SaveChangesAsync();

            if (status == ApplicationStatus.Approved)
            {
                var startDate = DateOnly.FromDateTime(now.AddDays(-10));
                db.Leases.Add(new Lease
                {
                    UnitId = unit.Id,
                    RentalApplicationId = application.Id,
                    StartDate = startDate,
                    EndDate = startDate.AddMonths(12).AddDays(-1),
                    MonthlyRent = unit.MonthlyRent
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
