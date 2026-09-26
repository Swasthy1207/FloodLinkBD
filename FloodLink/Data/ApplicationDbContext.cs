using FloodLink.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FloodLink.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Section 4.4 – Citizen Help Request
        public DbSet<HelpRequest> HelpRequests { get; set; }

        // Section 4.3 – Multi-Organization Coordination
        public DbSet<DisasterOperation> DisasterOperations { get; set; }
        public DbSet<OrganizationOperation> OrganizationOperations { get; set; }

        // Section 4.2 – Organization Management
        public DbSet<Organization> Organizations { get; set; }

        // Section 4.6 - Volunteer Management
        public DbSet<VolunteerProfile> VolunteerProfiles { get; set; }

        // Section 4.7 - Help Request Tracking
        public DbSet<HelpRequestStatusLog> HelpRequestStatusLogs { get; set; }

        // Section 4.8 - Shelter Management
        public DbSet<Shelter> Shelters { get; set; }

        // Section 4.9 - Resource and Equipment Management
        public DbSet<Resource> Resources { get; set; }
        public DbSet<ResourceDistribution> ResourceDistributions { get; set; }

        // Section 4.10 - Emergency Announcements
        public DbSet<Announcement> Announcements { get; set; }

        // Section 4.11 - Preparedness Activities
        public DbSet<PreparednessActivity> PreparednessActivities { get; set; }

        // Section 4.12 - High-Risk Area Management
        public DbSet<HighRiskArea> HighRiskAreas { get; set; }

        // Section 4.15 - User Notifications
        public DbSet<UserNotification> UserNotifications { get; set; }

        public DbSet<OrganizationHelpNeed> OrganizationHelpNeeds { get; set; }

        public DbSet<OrganizationHelpOffer> OrganizationHelpOffers { get; set; }

        public DbSet<OrganizationHelpOfferAssignment> OrganizationHelpOfferAssignments { get; set; }

        // Upgrades: Attachments, Reactions, Ratings, Comments
        public DbSet<HelpRequestAttachment> HelpRequestAttachments { get; set; }
        public DbSet<HelpRequestReaction> HelpRequestReactions { get; set; }
        public DbSet<HelpRequestRating> HelpRequestRatings { get; set; }
        public DbSet<HelpRequestComment> HelpRequestComments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure custom ApplicationUser properties
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
                entity.Property(u => u.Status).HasMaxLength(30).HasDefaultValue("Active");
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Configure Organization entity
            builder.Entity<Organization>(entity =>
            {
                entity.HasKey(o => o.Id);

                entity.Property(o => o.OrganizationName).HasMaxLength(150).IsRequired();
                entity.Property(o => o.OrganizationType).HasMaxLength(80).IsRequired();
                entity.Property(o => o.Description).HasMaxLength(1000).IsRequired();
                entity.Property(o => o.ContactEmail).HasMaxLength(256).IsRequired();
                entity.Property(o => o.ContactPhone).HasMaxLength(20).IsRequired();
                entity.Property(o => o.Address).HasMaxLength(300).IsRequired();
                entity.Property(o => o.ServiceArea).HasMaxLength(300).IsRequired();
                entity.Property(o => o.Status).HasMaxLength(20).HasDefaultValue("Pending");
                entity.Property(o => o.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(o => o.ManagedByUserId).HasMaxLength(450);

                // Unique index on organization name
                entity.HasIndex(o => o.OrganizationName).IsUnique();

                // Relationship: Organization -> ApplicationUser (optional manager)
                entity.HasOne(o => o.ManagedByUser)
                      .WithMany()
                      .HasForeignKey(o => o.ManagedByUserId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);
            });

            // Configure HelpRequest entity (Section 4.4)
            builder.Entity<HelpRequest>(entity =>
            {
                entity.HasKey(h => h.Id);
                entity.Property(h => h.Title).HasMaxLength(150).IsRequired();
                entity.Property(h => h.Description).HasMaxLength(2000).IsRequired();
                entity.Property(h => h.Category).HasMaxLength(80).IsRequired();
                entity.Property(h => h.Location).HasMaxLength(300).IsRequired();
                entity.Property(h => h.ContactInformation).HasMaxLength(200);
                entity.Property(h => h.ImagePath).HasMaxLength(500);
                entity.Property(h => h.Status).HasMaxLength(30).HasDefaultValue("Submitted");
                entity.Property(h => h.Priority).HasMaxLength(20).HasDefaultValue("Medium");
                entity.Property(h => h.UserEmail).HasMaxLength(256);
                entity.Property(h => h.VolunteerEmail).HasMaxLength(256);
                entity.Property(h => h.UserId).HasMaxLength(450);
                entity.Property(h => h.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Relationship: HelpRequest -> ApplicationUser (submitter)
                entity.HasOne(h => h.Submitter)
                      .WithMany()
                      .HasForeignKey(h => h.UserId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);

                // Relationship: HelpRequest -> DisasterOperation (optional, extension point)
                entity.HasOne(h => h.DisasterOperation)
                      .WithMany()
                      .HasForeignKey(h => h.DisasterOperationId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);

                // Relationship: HelpRequest -> ApplicationUser (verifier) (Section 4.5)
                entity.HasOne(h => h.VerifiedBy)
                      .WithMany()
                      .HasForeignKey(h => h.VerifiedById)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);

                // Relationship: HelpRequest -> ApplicationUser (assigned volunteer) (Section 4.6)
                entity.HasOne(h => h.AssignedVolunteer)
                      .WithMany()
                      .HasForeignKey(h => h.AssignedVolunteerId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);

                // Relationship: HelpRequest -> ApplicationUser (volunteer who completed)
                entity.HasOne(h => h.CompletedByVolunteer)
                      .WithMany()
                      .HasForeignKey(h => h.CompletedByVolunteerId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);

                entity.Property(h => h.VolunteerProgressNote).HasMaxLength(500);
                entity.Property(h => h.CompletedByVolunteerId).HasMaxLength(450);
            });

            // Configure DisasterOperation entity
            builder.Entity<DisasterOperation>(entity =>
            {
                entity.HasKey(d => d.DisasterOperationId);
                entity.Property(d => d.OperationName).HasMaxLength(150).IsRequired();
                entity.Property(d => d.Description).HasMaxLength(1000);
                entity.Property(d => d.DisasterType).HasMaxLength(100);
                entity.Property(d => d.Location).HasMaxLength(200);
                entity.Property(d => d.Status).HasMaxLength(20).HasDefaultValue("Pending");
                entity.Property(d => d.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                // Relationship to Lead Organization
                entity.HasOne(d => d.LeadOrganization)
                      .WithMany()
                      .HasForeignKey(d => d.CreatedByOrganizationId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure OrganizationOperation (junction) entity
            builder.Entity<OrganizationOperation>(entity =>
            {
                entity.HasKey(o => o.OrganizationOperationId);
                entity.Property(o => o.ParticipationStatus).HasMaxLength(20).HasDefaultValue("Pending");
                entity.Property(o => o.AssignedArea).HasMaxLength(300);
                entity.HasOne(o => o.DisasterOperation)
                      .WithMany()
                      .HasForeignKey(o => o.DisasterOperationId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(o => o.Organization)
                      .WithMany()
                      .HasForeignKey(o => o.OrganizationId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure VolunteerProfile entity (Section 4.6)
            builder.Entity<VolunteerProfile>(entity =>
            {
                entity.HasKey(v => v.Id);
                entity.Property(v => v.UserId).IsRequired();
                entity.Property(v => v.Skills).HasMaxLength(255);
                entity.Property(v => v.AvailabilityStatus).HasMaxLength(50).HasDefaultValue("Available");
                entity.Property(v => v.CurrentLocation).HasMaxLength(200);
                entity.Property(v => v.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(v => v.User)
                      .WithOne()
                      .HasForeignKey<VolunteerProfile>(v => v.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(v => v.Organization)
                      .WithMany()
                      .HasForeignKey(v => v.OrganizationId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);
            });

            // Configure HelpRequestStatusLog (Section 4.7)
            builder.Entity<HelpRequestStatusLog>(entity =>
            {
                entity.HasKey(l => l.Id);
                entity.Property(l => l.ChangeType).HasMaxLength(20).HasDefaultValue("Status");
                entity.Property(l => l.OldValue).HasMaxLength(50);
                entity.Property(l => l.NewValue).HasMaxLength(50);
                entity.Property(l => l.Note).HasMaxLength(500);
                entity.Property(l => l.ChangedByUserId).HasMaxLength(450);
                entity.Property(l => l.ChangedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(l => l.HelpRequest)
      .WithMany(h => h.StatusLogs)
      .HasForeignKey(l => l.HelpRequestId)
      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(l => l.ChangedBy)
                      .WithMany()
                      .HasForeignKey(l => l.ChangedByUserId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);
            });

            // Configure Shelter entity (Section 4.8)
            builder.Entity<Shelter>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.ShelterName).HasMaxLength(150).IsRequired();
                entity.Property(s => s.Location).HasMaxLength(300).IsRequired();
                entity.Property(s => s.Status).HasMaxLength(30).HasDefaultValue("Open");
                entity.Property(s => s.ContactPerson).HasMaxLength(200);
                entity.Property(s => s.ContactPhone).HasMaxLength(20);
                entity.Property(s => s.Facilities).HasMaxLength(500);
                entity.Property(s => s.CreatedByUserId).HasMaxLength(450);
                entity.Property(s => s.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(s => s.Organization)
                      .WithMany()
                      .HasForeignKey(s => s.OrganizationId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);

                entity.HasOne(s => s.CreatedBy)
                      .WithMany()
                      .HasForeignKey(s => s.CreatedByUserId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);
            });

            // Configure Resource & Equipment (Section 4.9)
            builder.Entity<Resource>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.ResourceName).HasMaxLength(150).IsRequired();
                entity.Property(r => r.ItemClassification).HasMaxLength(30).HasDefaultValue("Resource");
                entity.Property(r => r.Category).HasMaxLength(80).IsRequired();
                entity.Property(r => r.Unit).HasMaxLength(30).HasDefaultValue("Packets");
                entity.Property(r => r.Status).HasMaxLength(40).HasDefaultValue("Available");
                entity.Property(r => r.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(r => r.Organization)
                      .WithMany()
                      .HasForeignKey(r => r.OrganizationId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);

                entity.HasOne(r => r.CreatedBy)
                      .WithMany()
                      .HasForeignKey(r => r.CreatedByUserId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);
            });

            // Configure ResourceDistribution (Section 4.9)
            builder.Entity<ResourceDistribution>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.DistributedTo).HasMaxLength(200).IsRequired();
                entity.Property(d => d.DistributionDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(d => d.Resource)
                      .WithMany(r => r.Distributions)
                      .HasForeignKey(d => d.ResourceId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Organization)
                      .WithMany()
                      .HasForeignKey(d => d.OrganizationId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);

                entity.HasOne(d => d.DisasterOperation)
                      .WithMany()
                      .HasForeignKey(d => d.DisasterOperationId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);

                entity.HasOne(d => d.DistributedBy)
                      .WithMany()
                      .HasForeignKey(d => d.DistributedByUserId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);
            });

            // Configure Announcement (Section 4.10)
            builder.Entity<Announcement>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Title).HasMaxLength(200).IsRequired();
                entity.Property(a => a.Category).HasMaxLength(80).IsRequired();
                entity.Property(a => a.UrgencyLevel).HasMaxLength(30).HasDefaultValue("Normal");
                entity.Property(a => a.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(a => a.Organization)
                      .WithMany()
                      .HasForeignKey(a => a.OrganizationId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);

                entity.HasOne(a => a.CreatedBy)
                      .WithMany()
                      .HasForeignKey(a => a.CreatedByUserId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);
            });

            // Configure PreparednessActivity (Section 4.11)
            builder.Entity<PreparednessActivity>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Title).HasMaxLength(200).IsRequired();
                entity.Property(p => p.ActivityType).HasMaxLength(80).IsRequired();
                entity.Property(p => p.Status).HasMaxLength(40).HasDefaultValue("Planned");
                entity.Property(p => p.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(p => p.Organization)
                      .WithMany()
                      .HasForeignKey(p => p.OrganizationId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);

                entity.HasOne(p => p.CreatedBy)
                      .WithMany()
                      .HasForeignKey(p => p.CreatedByUserId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);
            });

            // Configure HighRiskArea (Section 4.12)
            builder.Entity<HighRiskArea>(entity =>
            {
                entity.HasKey(h => h.Id);
                entity.Property(h => h.AreaName).HasMaxLength(150).IsRequired();
                entity.Property(h => h.RiskLevel).HasMaxLength(30).HasDefaultValue("High");
                entity.Property(h => h.Accessibility).HasMaxLength(60).HasDefaultValue("Boat Required");
                entity.Property(h => h.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(h => h.Organization)
                      .WithMany()
                      .HasForeignKey(h => h.OrganizationId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);

                entity.HasOne(h => h.CreatedBy)
                      .WithMany()
                      .HasForeignKey(h => h.CreatedByUserId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);
            });

            // Configure UserNotification (Section 4.15)
            builder.Entity<UserNotification>(entity =>
            {
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Title).HasMaxLength(200).IsRequired();
                entity.Property(n => n.Message).HasMaxLength(1500).IsRequired();
                entity.Property(n => n.NotificationType).HasMaxLength(60).HasDefaultValue("General");
                entity.Property(n => n.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(n => n.User)
                      .WithMany()
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure HelpRequestAttachment
            builder.Entity<HelpRequestAttachment>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.FilePath).HasMaxLength(500).IsRequired();
                entity.Property(a => a.FileName).HasMaxLength(255).IsRequired();
                entity.Property(a => a.ContentType).HasMaxLength(100);
                entity.Property(a => a.UploadedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(a => a.HelpRequest)
                      .WithMany(h => h.Attachments)
                      .HasForeignKey(a => a.HelpRequestId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure HelpRequestReaction
            builder.Entity<HelpRequestReaction>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.UserId).HasMaxLength(450).IsRequired();
                entity.Property(r => r.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Unique constraint: one reaction per user per help request
                entity.HasIndex(r => new { r.HelpRequestId, r.UserId }).IsUnique();

                entity.HasOne(r => r.HelpRequest)
                      .WithMany(h => h.Reactions)
                      .HasForeignKey(r => r.HelpRequestId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.User)
                      .WithMany()
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure HelpRequestRating
            builder.Entity<HelpRequestRating>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.UserId).HasMaxLength(450).IsRequired();
                entity.Property(r => r.Review).HasMaxLength(1000);
                entity.Property(r => r.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Unique constraint: one rating per user per help request
                entity.HasIndex(r => new { r.HelpRequestId, r.UserId }).IsUnique();

                entity.HasOne(r => r.HelpRequest)
                      .WithMany(h => h.Ratings)
                      .HasForeignKey(r => r.HelpRequestId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.User)
                      .WithMany()
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure HelpRequestComment
            builder.Entity<HelpRequestComment>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.UserId).HasMaxLength(450).IsRequired();
                entity.Property(c => c.Content).HasMaxLength(2000).IsRequired();
                entity.Property(c => c.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(c => c.HelpRequest)
                      .WithMany(h => h.Comments)
                      .HasForeignKey(c => c.HelpRequestId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.User)
                      .WithMany()
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Organization Help Need
            builder.Entity<OrganizationHelpNeed>(entity =>
            {
                entity.HasKey(h => h.Id);

                entity.Property(h => h.Title)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(h => h.Description)
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.Property(h => h.HelpType)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(h => h.Priority)
                    .HasMaxLength(20)
                    .HasDefaultValue("Medium");

                entity.Property(h => h.Location)
                    .HasMaxLength(300)
                    .IsRequired();

                entity.Property(h => h.Status)
                    .HasMaxLength(30)
                    .HasDefaultValue("Pending");

                entity.Property(h => h.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(h => h.Organization)
                    .WithMany()
                    .HasForeignKey(h => h.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(h => h.RequestedByUser)
                    .WithMany()
                    .HasForeignKey(h => h.RequestedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(h => h.ReviewedByUser)
                    .WithMany()
                    .HasForeignKey(h => h.ReviewedByUserId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
            });

            // ================================================================
            // Organization Help Offer
            // ================================================================
            builder.Entity<OrganizationHelpOffer>(entity =>
            {
                entity.HasKey(o => o.HelpOfferId);

                entity.Property(o => o.OfferedHelpType)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(o => o.OfferedQuantity)
                      .HasMaxLength(200);

                entity.Property(o => o.Message)
                      .HasMaxLength(1000);

                entity.Property(o => o.Status)
                      .HasMaxLength(30)
                      .HasDefaultValue("Pending");

                entity.Property(o => o.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Offer -> Help Need
                entity.HasOne(o => o.HelpNeed)
                      .WithMany()
                      .HasForeignKey(o => o.HelpNeedId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Offer -> Offering Organization
                entity.HasOne(o => o.OfferingOrganization)
                      .WithMany()
                      .HasForeignKey(o => o.OfferingOrganizationId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Organization Help Offer Volunteer Assignment
            builder.Entity<OrganizationHelpOfferAssignment>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.AssignedByUserId)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.Property(a => a.Status)
                    .HasMaxLength(30)
                    .HasDefaultValue("Assigned");

                entity.Property(a => a.Notes)
                    .HasMaxLength(500);

                entity.Property(a => a.AssignedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(a => a.HelpOffer)
                    .WithMany()
                    .HasForeignKey(a => a.HelpOfferId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.VolunteerProfile)
                    .WithMany()
                    .HasForeignKey(a => a.VolunteerProfileId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.AssignedByUser)
                    .WithMany()
                    .HasForeignKey(a => a.AssignedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(a => new
                {
                    a.HelpOfferId,
                    a.VolunteerProfileId
                }).IsUnique();
            });

        }
        private void NormalizeDateTimesToUtc()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                foreach (var property in entry.Properties)
                {
                    if (property.CurrentValue is DateTime dt)
                    {
                        property.CurrentValue = dt.Kind switch
                        {
                            DateTimeKind.Utc =>
                                dt,

                            DateTimeKind.Local =>
                                dt.ToUniversalTime(),

                            DateTimeKind.Unspecified =>
                                DateTime.SpecifyKind(
                                    dt,
                                    DateTimeKind.Utc),

                            _ => dt
                        };
                    }
                }
            }
        }

        public override int SaveChanges()
        {
            NormalizeDateTimesToUtc();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            NormalizeDateTimesToUtc();
            return base.SaveChangesAsync(cancellationToken);
        }

    }
   
}