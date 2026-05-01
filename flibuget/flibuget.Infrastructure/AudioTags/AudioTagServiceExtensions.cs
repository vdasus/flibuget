using flibuget.Core.InfraServices.AudioTags;
using Microsoft.Extensions.DependencyInjection;

namespace flibuget.Infrastructure.AudioTags;

public static class AudioTagServiceExtensions
{
    public static IServiceCollection AddAudioTagService(this IServiceCollection services)
    {
        services.AddSingleton<IAudioTagService, AudioTagService>();
        return services;
    }
}
