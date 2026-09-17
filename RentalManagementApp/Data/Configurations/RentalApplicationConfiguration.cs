using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentalManagementApp.Data.Entities;
using RentalManagementApp.Data.Enums;

namespace RentalManagementApp.Data.Configurations;

public class RentalApplicationConfiguration : IEntityTypeConfiguration<RentalApplication>
{
    public void Configure(EntityTypeBuilder<RentalApplication> builder)
    {
        builder.Property(a => a.ApplicantFirstName).HasMaxLength(100);
        builder.Property(a => a.ApplicantLastName).HasMaxLength(100);
        builder.Property(a => a.ApplicantPhone).HasMaxLength(30);
        builder.Property(a => a.ApplicantEmail).HasMaxLength(200);
        builder.Property(a => a.CurrentAddress).HasMaxLength(300);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.RowVersion).IsRowVersion();

        builder.HasOne(a => a.Unit)
            .WithMany(u => u.RentalApplications)
            .HasForeignKey(a => a.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Applicant)
            .WithMany(u => u.RentalApplications)
            .HasForeignKey(a => a.ApplicantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Residences)
            .WithOne(r => r.RentalApplication)
            .HasForeignKey(r => r.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.StatusHistory)
            .WithOne(h => h.RentalApplication)
            .HasForeignKey(h => h.RentalApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.Status);
    }
}

public class ResidenceConfiguration : IEntityTypeConfiguration<Residence>
{
    public void Configure(EntityTypeBuilder<Residence> builder)
    {
        builder.Property(r => r.Address).IsRequired().HasMaxLength(300);
        builder.Property(r => r.LandlordName).IsRequired().HasMaxLength(150);
        builder.Property(r => r.LandlordPhone).IsRequired().HasMaxLength(30);
    }
}

public class ApplicationStatusHistoryConfiguration : IEntityTypeConfiguration<ApplicationStatusHistory>
{
    public void Configure(EntityTypeBuilder<ApplicationStatusHistory> builder)
    {
        builder.Property(h => h.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.Comment).HasMaxLength(1000);

        builder.HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
