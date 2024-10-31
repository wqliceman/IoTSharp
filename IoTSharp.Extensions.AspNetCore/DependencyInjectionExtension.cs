namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtension
    {
        public static T GetRequiredService<T>(this IServiceScopeFactory scopeFactor) =>
                                  scopeFactor.CreateScope().ServiceProvider.GetRequiredService<T>();
    }
}