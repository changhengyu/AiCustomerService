using AiCustomerService.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiCustomerService.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ChannelConfig> ChannelConfigs => Set<ChannelConfig>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();
    public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<IntentionRule> IntentionRules => Set<IntentionRule>();
    public DbSet<AiUsageLog> AiUsageLogs => Set<AiUsageLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<IndustryFaq> IndustryFaqs => Set<IndustryFaq>();
    public DbSet<EvalReport> EvalReports => Set<EvalReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
