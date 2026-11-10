using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Fleet.Shared.Messaging.Outbox;

namespace Fleet.VehicleService.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id");

        builder.Property(o => o.EventType)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("event_type");

        builder.Property(o => o.Payload)
            .IsRequired()
            .HasColumnType("text")
            .HasColumnName("payload");

        builder.Property(o => o.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(o => o.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(o => o.Error)
            .HasMaxLength(2000)
            .HasColumnName("error");

        // Partial index to allow fast polling of unprocessed messages: WHERE processed_at IS NULL
        builder.HasIndex(o => o.ProcessedAt)
            .HasFilter("processed_at IS NULL")
            .HasDatabaseName("ix_outbox_unprocessed");
    }
}