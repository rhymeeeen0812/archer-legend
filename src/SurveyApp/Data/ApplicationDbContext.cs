using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SurveyApp.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Choice> Choices => Set<Choice>();
    public DbSet<Response> Responses => Set<Response>();
    public DbSet<Answer> Answers => Set<Answer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Survey>(e =>
        {
            e.Property(s => s.Title).HasMaxLength(200);
            e.Property(s => s.Description).HasMaxLength(2000);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(s => s.Owner).WithMany()
                .HasForeignKey(s => s.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(s => s.Questions).WithOne(q => q.Survey)
                .HasForeignKey(q => q.SurveyId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(s => s.Responses).WithOne(r => r.Survey)
                .HasForeignKey(r => r.SurveyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Question>(e =>
        {
            e.Property(q => q.Text).HasMaxLength(500);
            e.Property(q => q.Type).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(q => new { q.SurveyId, q.Order }).IsUnique();

            e.HasMany(q => q.Choices).WithOne(c => c.Question)
                .HasForeignKey(c => c.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Choice>(e =>
        {
            e.Property(c => c.Text).HasMaxLength(200);
            e.HasIndex(c => new { c.QuestionId, c.Order }).IsUnique();
        });

        builder.Entity<Response>(e =>
        {
            // 退会したユーザーの回答は集計に残す
            e.HasOne(r => r.Respondent).WithMany()
                .HasForeignKey(r => r.RespondentId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasMany(r => r.Answers).WithOne(a => a.Response)
                .HasForeignKey(a => a.ResponseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Answer>(e =>
        {
            e.Property(a => a.Text).HasMaxLength(4000);

            e.HasOne(a => a.Question).WithMany()
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.Choice).WithMany()
                .HasForeignKey(a => a.ChoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // 選択肢ごとの集計クエリ用
            e.HasIndex(a => new { a.QuestionId, a.ChoiceId });

            // 選択肢と自由記述のどちらか一方だけを持つ
            e.ToTable(t => t.HasCheckConstraint(
                "CK_Answers_ChoiceOrText",
                "(\"ChoiceId\" IS NULL) <> (\"Text\" IS NULL)"));
        });
    }
}
