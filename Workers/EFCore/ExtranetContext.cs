using AppliedSoftware.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AppliedSoftware.Workers.EFCore;

public partial class ExtranetContext : DbContext
{
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public ExtranetContext()
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="options"></param>
    public ExtranetContext(DbContextOptions<ExtranetContext> options)
        : base(options)
    {
    }
    
    public virtual DbSet<UserDto> Users { get; set; } = null!;
    
    public virtual DbSet<PackageDto> Packages { get; set; } = null!;
    
    public virtual DbSet<PackageActionDto> PackageActions { get; set; } = null!;
    
    public virtual DbSet<EmailPackageActionDto> EmailPackageActions { get; set; } = null!;
    
    public virtual DbSet<EmailAttachmentDto> EmailAttachments { get; set; } = null!;
    
    public virtual DbSet<TeamDto> Teams { get; set; } = null!;
    
    public virtual DbSet<UserGroupDto> UserGroups { get; set; } = null!;
    
    public virtual DbSet<UserPermissionOverrideDto> UserPermissionOverrides { get; set; } = null!;
    
    public virtual DbSet<UserGroupPermissionOverrideDto> UserGroupPermissionOverrides { get; set; } = null!;
    
    
    
    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql(oa => oa.EnableRetryOnFailure())
#if DEBUG // When debugging we want to log detailed errors which may contain sensitive information.
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
#endif
    ;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserDto>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("user__pk");

            entity.ToTable("user");

            entity.HasIndex(e => e.DisplayName, "user_display_name__indx");
        });

        modelBuilder.Entity<PackageDto>(entity =>
        {
            entity.HasKey(e => e.PackageId).HasName("package__pk");
            
            entity.ToTable("package");
            
            entity.HasIndex(e => e.Name, "package_name_unq__indx").IsUnique();

            
        });

        modelBuilder.Entity<PackageActionDto>(entity =>
        {
            entity.HasKey(e => e.PackageActionId).HasName("package_action__pk");
            
            entity.ToTable("package_action");
            
            // entity.HasOne(e => e.Package)
            //         .WithMany(p => p.Actions)
            //         .HasForeignKey(e => e.PackageId)
            //       .HasConstraintName("package_action_package__fk");
        });

        modelBuilder.Entity<EmailPackageActionDto>(entity =>
        {
            entity.HasKey(e => e.PackageActionId).HasName("email_package_action__pk");
            
            entity.ToTable("email_package_action");

            entity.HasGeneratedTsVectorColumn(
                e => e.EmailTsVector,
                "english",
                e => new { e.Subject, e.Body });

            entity.HasIndex(e => e.EmailTsVector, "email_tsv__indx").HasMethod("GIN");
        });

        modelBuilder.Entity<EmailAttachmentDto>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("email_attachment__pk");
            
            entity.ToTable("email_attachment");
        });

        modelBuilder.Entity<TeamDto>(entity =>
        {
            entity.HasKey(e => e.TeamId).HasName("team__pk");
            
            entity.ToTable("team");

            entity.HasIndex(e => e.Name, "team_name__indx");
        });

        modelBuilder.Entity<UserGroupDto>(entity =>
        {
            entity.HasKey(e => e.UserGroupId).HasName("user_group__pk");
            
            entity.ToTable("user_group");
        });

        modelBuilder.Entity<UserPermissionOverrideDto>(entity =>
        {
            entity.HasKey(e => e.UserPermissionOverrideId).HasName("user_permission_override__pk");
            
            entity.ToTable("user_permission_override");
        });

        modelBuilder.Entity<UserGroupPermissionOverrideDto>(entity =>
        {
            entity.HasKey(e => e.UserGroupOverrideId).HasName("user_group_permission_override__pk");
            
            entity.ToTable("user_group_permission_override");
        });
        
        OnModelCreatingPartial(modelBuilder);
    }
    
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}