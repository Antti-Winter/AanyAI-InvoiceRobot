using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InvoiceRobot.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<InvoiceRobotDbContext>
{
    public InvoiceRobotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InvoiceRobotDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=InvoiceRobotDev;Trusted_Connection=True;");

        return new InvoiceRobotDbContext(optionsBuilder.Options);
    }
}
