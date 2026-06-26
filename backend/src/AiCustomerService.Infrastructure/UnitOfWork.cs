using AiCustomerService.Core.Interfaces;
using AiCustomerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace AiCustomerService.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public UnitOfWork(AppDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default) =>
        await _db.Database.BeginTransactionAsync(ct);
}
