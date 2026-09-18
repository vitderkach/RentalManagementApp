using Microsoft.EntityFrameworkCore;
using RentalManagementApp.Domain.Entities;
using RentalManagementApp.Domain.Enums;
using RentalManagementApp.Application.Services;
using RentalManagementApp.Application.Contracts;
using RentalManagementApp.Application.Interfaces;
using RentalManagementApp.Infrastructure.Data;
using RentalManagementApp.Infrastructure.Data.Repositories;
using Xunit;

namespace RentalManagementApp.Tests;

public class ApplicantApplicationServiceTests
{
    private static ApplicantApplicationService CreateApplicantSut(ApplicationDbContext db) =>
        new(new ApplicationRepository(db), new UnitAvailabilityService());

    private static ApplicationReviewService CreateReviewSut(ApplicationDbContext db) =>
        new(new ApplicationRepository(db), new UnitAvailabilityService(), TimeProvider.System);

    private static ApplicantInfoInput ValidApplicantInfo() =>
        new("Jane", "Doe", "555-0100", "jane@example.com", "123 Elm St", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30));

    [Fact]
    public async Task StartApplicationAsync_CreatesDraft_WhenUnitIsAvailable()
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db);
        var sut = CreateApplicantSut(db);

        var result = await sut.StartApplicationAsync(unit.Id, "applicant-1");

        Assert.True(result.Succeeded);
        var application = await db.RentalApplications.SingleAsync();
        Assert.Equal(ApplicationStatus.Draft, application.Status);
        var history = await db.ApplicationStatusHistories.ToListAsync();
        Assert.Single(history);
    }

    [Fact]
    public async Task StartApplicationAsync_Fails_WhenUnitHasActiveLease()
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Leases.Add(new Lease { UnitId = unit.Id, StartDate = today.AddDays(-10), EndDate = today.AddDays(300), MonthlyRent = 1200m });
        await db.SaveChangesAsync();

        var sut = CreateApplicantSut(db);
        var result = await sut.StartApplicationAsync(unit.Id, "applicant-1");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task SaveApplicantInfoAsync_Fails_WhenApplicationIsNotEditable()
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db);
        var application = new RentalApplication { UnitId = unit.Id, ApplicantId = "applicant-1", Status = ApplicationStatus.Submitted };
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();

        var sut = CreateApplicantSut(db);
        var result = await sut.SaveApplicantInfoAsync(application.Id, "applicant-1", ValidApplicantInfo());

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task SaveApplicantInfoAsync_Fails_WhenNotOwnedByCaller()
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db);
        var application = new RentalApplication { UnitId = unit.Id, ApplicantId = "applicant-1", Status = ApplicationStatus.Draft };
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();

        var sut = CreateApplicantSut(db);
        var result = await sut.SaveApplicantInfoAsync(application.Id, "someone-else", ValidApplicantInfo());

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task SubmitAsync_Fails_WhenSectionsIncomplete()
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db);
        var application = new RentalApplication
        {
            UnitId = unit.Id, ApplicantId = "applicant-1", Status = ApplicationStatus.Draft,
            ApplicantInfoCompleted = true, ResidenceHistoryCompleted = false
        };
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();

        var sut = CreateApplicantSut(db);
        var result = await sut.SubmitAsync(application.Id, "applicant-1");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task SubmitAsync_Succeeds_AndRecordsHistory_WhenBothSectionsComplete()
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db);
        var application = new RentalApplication
        {
            UnitId = unit.Id, ApplicantId = "applicant-1", Status = ApplicationStatus.Draft,
            ApplicantInfoCompleted = true, ResidenceHistoryCompleted = true
        };
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();

        var sut = CreateApplicantSut(db);
        var result = await sut.SubmitAsync(application.Id, "applicant-1");

        Assert.True(result.Succeeded);
        var reloaded = await db.RentalApplications.Include(a => a.StatusHistory).SingleAsync(a => a.Id == application.Id);
        Assert.Equal(ApplicationStatus.Submitted, reloaded.Status);
        Assert.Contains(reloaded.StatusHistory, h => h.Status == ApplicationStatus.Submitted);
    }

    [Fact]
    public async Task SubmitAsync_Fails_WhenUnitNowHasAnActiveLease()
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db);
        var application = new RentalApplication
        {
            UnitId = unit.Id, ApplicantId = "applicant-1", Status = ApplicationStatus.Draft,
            ApplicantInfoCompleted = true, ResidenceHistoryCompleted = true
        };
        db.RentalApplications.Add(application);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Leases.Add(new Lease { UnitId = unit.Id, StartDate = today, EndDate = today.AddMonths(12), MonthlyRent = 1200m });
        await db.SaveChangesAsync();

        var sut = CreateApplicantSut(db);
        var result = await sut.SubmitAsync(application.Id, "applicant-1");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task WithdrawAsync_Fails_WhenApplicationIsTerminal()
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db);
        var application = new RentalApplication { UnitId = unit.Id, ApplicantId = "applicant-1", Status = ApplicationStatus.Approved };
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();

        var sut = CreateApplicantSut(db);
        var result = await sut.WithdrawAsync(application.Id, "applicant-1");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task WithdrawAsync_Succeeds_FromSubmitted()
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db);
        var application = new RentalApplication { UnitId = unit.Id, ApplicantId = "applicant-1", Status = ApplicationStatus.Submitted };
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();

        var sut = CreateApplicantSut(db);
        var result = await sut.WithdrawAsync(application.Id, "applicant-1");

        Assert.True(result.Succeeded);
        Assert.Equal(ApplicationStatus.Withdrawn, (await db.RentalApplications.FindAsync(application.Id))!.Status);
    }

    [Theory]
    [InlineData(ApplicationReviewOutcome.Return)]
    [InlineData(ApplicationReviewOutcome.Deny)]
    public async Task ReviewAsync_RequiresComment_ForReturnOrDeny(ApplicationReviewOutcome outcome)
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db);
        var application = new RentalApplication { UnitId = unit.Id, ApplicantId = "applicant-1", Status = ApplicationStatus.Submitted };
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();

        var sut = CreateReviewSut(db);
        var result = await sut.ReviewAsync(application.Id, "manager-1", outcome, comment: null);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ReviewAsync_Approve_CreatesTwelveMonthLease_AndTransitionsToApproved()
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db, rent: 1500m);
        var application = new RentalApplication { UnitId = unit.Id, ApplicantId = "applicant-1", Status = ApplicationStatus.Submitted };
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();

        var sut = CreateReviewSut(db);
        var result = await sut.ReviewAsync(application.Id, "manager-1", ApplicationReviewOutcome.Approve, null);

        Assert.True(result.Succeeded);
        var reloaded = await db.RentalApplications.SingleAsync(a => a.Id == application.Id);
        Assert.Equal(ApplicationStatus.Approved, reloaded.Status);

        var lease = await db.Leases.SingleAsync(l => l.RentalApplicationId == application.Id);
        Assert.Equal(1500m, lease.MonthlyRent);
        Assert.Equal(lease.StartDate.AddMonths(12).AddDays(-1), lease.EndDate);
    }

    [Fact]
    public async Task ReviewAsync_Fails_WhenReviewerDoesNotManageTheProperty()
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db);
        var application = new RentalApplication
        {
            UnitId = unit.Id,
            ApplicantId = "applicant-1",
            Status = ApplicationStatus.Submitted
        };
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();

        var sut = CreateReviewSut(db);
        var result = await sut.ReviewAsync(application.Id, "manager-2", ApplicationReviewOutcome.Approve, null);

        Assert.False(result.Succeeded);
        Assert.Equal(ApplicationStatus.Submitted, (await db.RentalApplications.FindAsync(application.Id))!.Status);
    }

    [Fact]
    public async Task ReviewAsync_Approve_UsesApplicantsDesiredStartDate_ForTheTwelveMonthTerm()
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db, rent: 1500m);
        var desiredStartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(14);
        var application = new RentalApplication
        {
            UnitId = unit.Id, ApplicantId = "applicant-1", Status = ApplicationStatus.Submitted,
            DesiredLeaseStartDate = desiredStartDate
        };
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();

        var sut = CreateReviewSut(db);
        var result = await sut.ReviewAsync(application.Id, "manager-1", ApplicationReviewOutcome.Approve, null);

        Assert.True(result.Succeeded);
        var lease = await db.Leases.SingleAsync(l => l.RentalApplicationId == application.Id);
        Assert.Equal(desiredStartDate, lease.StartDate);
        Assert.Equal(desiredStartDate.AddMonths(12).AddDays(-1), lease.EndDate);
    }

    [Fact]
    public async Task ReviewAsync_Approve_ClampsToToday_WhenDesiredStartDateHasAlreadyPassed()
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db);
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10);
        var application = new RentalApplication
        {
            UnitId = unit.Id, ApplicantId = "applicant-1", Status = ApplicationStatus.Submitted,
            DesiredLeaseStartDate = pastDate
        };
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();

        var sut = CreateReviewSut(db);
        var result = await sut.ReviewAsync(application.Id, "manager-1", ApplicationReviewOutcome.Approve, null);

        Assert.True(result.Succeeded);
        var lease = await db.Leases.SingleAsync(l => l.RentalApplicationId == application.Id);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), lease.StartDate);
    }

    [Fact]
    public async Task ReviewAsync_Approve_Fails_WhenUnitAlreadyHasAnActiveLease()
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Leases.Add(new Lease { UnitId = unit.Id, StartDate = today.AddDays(-5), EndDate = today.AddMonths(12), MonthlyRent = 1200m });

        var application = new RentalApplication { UnitId = unit.Id, ApplicantId = "applicant-1", Status = ApplicationStatus.Submitted };
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();

        var sut = CreateReviewSut(db);
        var result = await sut.ReviewAsync(application.Id, "manager-1", ApplicationReviewOutcome.Approve, null);

        Assert.False(result.Succeeded);
        Assert.Equal(ApplicationStatus.Submitted, (await db.RentalApplications.FindAsync(application.Id))!.Status);
    }

    [Fact]
    public async Task ReviewAsync_Fails_WhenApplicationIsNotSubmitted()
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db);
        var application = new RentalApplication { UnitId = unit.Id, ApplicantId = "applicant-1", Status = ApplicationStatus.Draft };
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();

        var sut = CreateReviewSut(db);
        var result = await sut.ReviewAsync(application.Id, "manager-1", ApplicationReviewOutcome.Approve, null);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AddOrUpdateResidenceAsync_ResetsResidenceHistoryCompleted()
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db);
        var application = new RentalApplication
        {
            UnitId = unit.Id, ApplicantId = "applicant-1", Status = ApplicationStatus.Draft,
            ResidenceHistoryCompleted = true
        };
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();

        var sut = CreateApplicantSut(db);
        var input = new ResidenceInput(null, "1 Elm St", "Landlord Larry", "555-0101", new DateOnly(2020, 1, 1), new DateOnly(2022, 1, 1));
        var result = await sut.AddOrUpdateResidenceAsync(application.Id, "applicant-1", input);

        Assert.True(result.Succeeded);
        var reloaded = await db.RentalApplications.FindAsync(application.Id);
        Assert.False(reloaded!.ResidenceHistoryCompleted);
    }

    [Fact]
    public async Task AddOrUpdateResidenceAsync_Fails_WhenMoveOutDateIsBeforeMoveInDate()
    {
        using var db = TestDbFactory.Create();
        var (unit, _) = TestDbFactory.SeedPropertyAndUnit(db);
        var application = new RentalApplication { UnitId = unit.Id, ApplicantId = "applicant-1", Status = ApplicationStatus.Draft };
        db.RentalApplications.Add(application);
        await db.SaveChangesAsync();

        var sut = CreateApplicantSut(db);
        var input = new ResidenceInput(null, "1 Elm St", "Landlord Larry", "555-0101", new DateOnly(2022, 1, 1), new DateOnly(2020, 1, 1));
        var result = await sut.AddOrUpdateResidenceAsync(application.Id, "applicant-1", input);

        Assert.False(result.Succeeded);
        Assert.Empty(await db.Residences.Where(r => r.RentalApplicationId == application.Id).ToListAsync());
    }
}
