using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentalManagementApp.Data.Entities;

namespace RentalManagementApp.Data.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.AddressLine1).IsRequired().HasMaxLength(200);
        builder.Property(p => p.AddressLine2).HasMaxLength(200);
        builder.Property(p => p.City).IsRequired().HasMaxLength(100);
        builder.Property(p => p.State).IsRequired().HasMaxLength(50);
        builder.Property(p => p.ZipCode).IsRequired().HasMaxLength(20);

        builder.HasOne(p => p.PropertyManager)
            .WithMany(u => u.ManagedProperties)
            .HasForeignKey(p => p.PropertyManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Units)
            .WithOne(u => u.Property)
            .HasForeignKey(u => u.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
