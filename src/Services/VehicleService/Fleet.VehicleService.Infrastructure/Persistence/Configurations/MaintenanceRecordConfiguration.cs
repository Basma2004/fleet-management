using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fleet.VehicleService.Domain.Entities;

namespace Fleet.VehicleService.Infrastructure.Persistence.Configurations;

public class MaintenanceRecordConfiguration : IEntityTypeConfiguration<MaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<MaintenanceRecord> builder)
    {
        builder.ToTable("vehicle_maintenance_history");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");

        builder.Property(m => m.VehicleId)
            .IsRequired()
            .HasColumnName("vehicle_id");

        builder.Property(m => m.ServiceDate)
            .IsRequired()
            .HasColumnName("service_date");

        builder.Property(m => m.ServiceType)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("service_type");

        builder.Property(m => m.Cost)
            .HasPrecision(10, 2)
            .HasColumnName("cost");

        builder.Property(m => m.Notes)
            .HasMaxLength(1000)
            .HasColumnName("notes");

        builder.Property(m => m.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(m => m.UpdatedAt)
            .IsRequired()
            .HasColumnName("updated_at");

        // Composite index for fast vehicle history lookups ordered by date
        builder.HasIndex(m => new { m.VehicleId, m.ServiceDate })
            .HasDatabaseName("ix_maintenance_vehicle_date");
    }
}