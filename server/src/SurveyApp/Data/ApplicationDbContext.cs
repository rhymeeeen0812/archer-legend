using Microsoft.EntityFrameworkCore;

namespace SurveyApp.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Run> Runs => Set<Run>();
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Choice> Choices => Set<Choice>();
    public DbSet<Response> Responses => Set<Response>();
    public DbSet<Answer> Answers => Set<Answer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Player>(e =>
        {
            // Id はゲーム側で生成するので DB では採番しない
            e.Property(p => p.Id).ValueGeneratedNever();
            e.Property(p => p.Name).HasMaxLength(Player.NameMaxLength);
            e.Property(p => p.Version).IsRowVersion();

            // ランキングの並び順（スコアの高い順、同点なら先に達成した順）
            e.HasIndex(p => new { p.BestScore, p.BestScoreAchievedAt })
                .IsDescending(true, false);
        });

        builder.Entity<Run>(e =>
        {
            e.Property(r => r.Id).ValueGeneratedNever();

            e.HasOne(r => r.Player).WithMany()
                .HasForeignKey(r => r.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(r => new { r.PlayerId, r.PlayedAt });
        });

        builder.Entity<Survey>(e =>
        {
            e.Property(s => s.Title).HasMaxLength(200);
            e.Property(s => s.Description).HasMaxLength(2000);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

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
            e.HasOne(r => r.Player).WithMany()
                .HasForeignKey(r => r.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            // 同じプレイヤーは 1 つのアンケートに 1 回だけ回答できる
            e.HasIndex(r => new { r.SurveyId, r.PlayerId }).IsUnique();

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
