using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Shared.Infrastructure.Persistence;

public class ModulithDbContext : DbContext
{
    public ModulithDbContext(DbContextOptions<ModulithDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Modules will apply their configurations
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.EndsWith("Infrastructure") == true)
            .ToArray();

        foreach (var assembly in assemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }
    }
}
