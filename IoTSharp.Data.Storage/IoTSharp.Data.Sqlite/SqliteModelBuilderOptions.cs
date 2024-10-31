using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace IoTSharp.Data.Sqlite
{
    public class SqliteModelBuilderOptions : IDataBaseModelBuilderOptions
    {
        public SqliteModelBuilderOptions()
        {
        }

        public IInfrastructure<IServiceProvider> Infrastructure { get; set; }

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.SetCaseInsensitiveSearchesForSQLite();
        }
    }
}