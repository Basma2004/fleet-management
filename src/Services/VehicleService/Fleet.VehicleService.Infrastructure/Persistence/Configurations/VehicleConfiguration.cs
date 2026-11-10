using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fleet.VehicleService.Domain.Entities;
using Fleet.VehicleService.Domain.Enums;

namespace Fleet.VehicleService.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id");

        builder.Property(v => v.TenantId)
            .IsRequired()
            .HasColumnName("tenant_id");

        builder.Property(v => v.Vin)
            .IsRequired()
            .HasMaxLength(17)
            .HasColumnName("vin");

        builder.Property(v => v.PlateNumber)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("plate_number");

        builder.Property(v => v.Make)
            .HasMaxLength(50)
            .HasColumnName("make");

        builder.Property(v => v.Model)
            .HasMaxLength(50)
            .HasColumnName("model");

        builder.Property(v => v.Year)
            .HasColumnName("year");

        builder.Property(v => v.Type)
            .HasMaxLength(30)
            .HasColumnName("type");

        // Convert Enum to String for human-readable, migration-safe storage
        builder.Property(v => v.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("status");

        builder.Property(v => v.OdometerKm)
            .HasPrecision(12, 2)
            .HasColumnName("odometer_km");

        builder.Property(v => v.EngineHours)
            .HasPrecision(10, 2)
            .HasColumnName("engine_hours");

        builder.Property(v => v.DeviceId)
            .HasMaxLength(50)
            .HasColumnName("device_id");

        builder.Property(v => v.CreatedBy)
            .HasMaxLength(100)
            .HasColumnName("created_by");

        builder.Property(v => v.UpdatedBy)
            .HasMaxLength(100)
            .HasColumnName("updated_by");

        builder.Property(v => v.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(v => v.UpdatedAt)
            .IsRequired()
            .HasColumnName("updated_at");

        // Multi-tenant Unique Indexes
        builder.HasIndex(v => new { v.TenantId, v.Vin })
            .IsUnique()
            .HasDatabaseName("ix_vehicles_tenant_vin");

        builder.HasIndex(v => new { v.TenantId, v.PlateNumber })
            .IsUnique()
            .HasDatabaseName("ix_vehicles_tenant_plate");

        // Nullable unique index for IoT Device ID (a device can only be assigned to one vehicle)
        builder.HasIndex(v => v.DeviceId)
            .IsUnique()
            .HasFilter("device_id IS NOT NULL")
            .HasDatabaseName("ix_vehicles_device_id");

        // Configure 1-to-Many relationship with MaintenanceRecord
        builder.HasMany(v => v.MaintenanceHistory)
            .WithOne()
            .HasForeignKey(m => m.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tell EF Core to access the private backing field `_maintenanceHistory`
        builder.Navigation(v => v.MaintenanceHistory)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}