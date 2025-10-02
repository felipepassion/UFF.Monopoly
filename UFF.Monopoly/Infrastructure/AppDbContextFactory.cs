using Microsoft.EntityFrameworkCore;
using UFF.Monopoly.Data;

namespace UFF.Monopoly.Infrastructure;

public interface IAppDbContextFactory
{
    ApplicationDbContext CreateDbContext();
}

public class AppDbContextFactory : IAppDbContextFactory
{
    private readonly IDbContextFactory<ApplicationDbContext> _pooledFactory;

    public AppDbContextFactory(IDbContextFactory<ApplicationDbContext> pooledFactory)
    {
        _pooledFactory = pooledFactory;
    }

    public ApplicationDbContext CreateDbContext()
    {
        return _pooledFactory.CreateDbContext();
    }
}
