﻿using IoTSharp.Data;
using IoTSharp.Data.InMemory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IoTSharpDataBuilderExtensions
    {
        public static void ConfigureInMemory(this IServiceCollection services, int poolSize, HealthChecksUIBuilder healthChecksUI)
        {
            services.AddEntityFrameworkInMemoryDatabase();
            services.AddSingleton<IDataBaseModelBuilderOptions>(c => new InMemoryModelBuilderOptions());
            services.AddDbContextPool<ApplicationDbContext>(builder =>
            {
                builder.UseInMemoryDatabase("IoTSharp");
                builder.UseInternalServiceProvider(services.BuildServiceProvider());
            }, poolSize);
            healthChecksUI.AddInMemoryStorage(opt => opt.ConfigureWarnings(w => w.Ignore(RelationalEventId.MultipleCollectionIncludeWarning)));
        }
    }
}