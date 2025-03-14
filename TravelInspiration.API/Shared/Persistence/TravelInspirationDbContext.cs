﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TravelInspiration.API.Shared.Domain.Entities;
namespace TravelInspiration.API.Shared.Persistence;

public sealed class TravelInspirationDbContext(
    DbContextOptions<TravelInspirationDbContext> options) : DbContext(options)
{
    public DbSet<Itinerary> Itineraries => Set<Itinerary>();
    public DbSet<Stop> Stops => Set<Stop>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Stop>()
            .Property(s => s.ImageUri)
            .HasConversion(
                v => v != null ? v.ToString() : null,
                v => v != null ? new Uri(v) : null);
        
        modelBuilder.Entity<Itinerary>().HasData(
            new Itinerary("A Trip to Paris", "KevinsUserId")
            {
                Id = 1,
                Description = "Five great days in Paris",
                CreatedBy = "DATASEED",
                CreatedOn = DateTime.UtcNow
            },
             new Itinerary("Antwerp Extravaganza", "KevinsUserId")
             {
                 Id = 2,
                 Description = "A week in beautiful Antwerp",
                 CreatedBy = "DATASEED",
                 CreatedOn = DateTime.UtcNow
             });

        modelBuilder.Entity<Stop>().HasData(
                 new("The Eiffel Tower")
                 {
                     Id = 1,
                     ItineraryId = 1,
                     ImageUri = new Uri("https://localhost:7120/images/eiffeltower.jpg"),
                     CreatedBy = "DATASEED",
                     CreatedOn = DateTime.UtcNow
                 },
                 new("The Louvre")
                 {
                     Id = 2,
                     ItineraryId = 1,
                     ImageUri = new Uri("https://localhost:7120/images/louvre.jpg"),
                     CreatedBy = "DATASEED",
                     CreatedOn = DateTime.UtcNow
                 },
                 new("Père Lachaise Cemetery")
                 {
                     Id = 3,
                     ItineraryId = 1,
                     ImageUri = new Uri("https://localhost:7120/images/perelachaise.jpg"),
                     CreatedBy = "DATASEED",
                     CreatedOn = DateTime.UtcNow
                 },
                 new("The Royal Museum of Beautiful Arts")
                 {
                     Id = 4,
                     ItineraryId = 2,
                     ImageUri = new Uri("https://localhost:7120/images/royalmuseum.jpg"),
                     CreatedBy = "DATASEED",
                     CreatedOn = DateTime.UtcNow
                 },
                 new("Saint Paul's Church")
                 {
                     Id = 5,
                     ItineraryId = 2,
                     ImageUri = new Uri("https://localhost:7120/images/stpauls.jpg"),
                     CreatedBy = "DATASEED",
                     CreatedOn = DateTime.UtcNow
                 },
                 new("Michelin Restaurant Visit")
                 {
                     Id = 6,
                     ItineraryId = 2,
                     ImageUri = new Uri("https://localhost:7120/images/michelin.jpg"),
                     CreatedBy = "DATASEED",
                     CreatedOn = DateTime.UtcNow
                 });

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TravelInspirationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    // ...existing code...


    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedOn = DateTime.UtcNow;
                    entry.Entity.CreatedBy = "SYSTEM";
                    entry.Entity.LastModifiedOn = DateTime.UtcNow;
                    entry.Entity.LastModifiedBy = "SYSTEM";
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedOn = DateTime.UtcNow;
                    entry.Entity.LastModifiedBy = "SYSTEM";
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken); 
    }
}
