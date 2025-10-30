using Microsoft.Extensions.DependencyInjection;

namespace flibuget.Core.InfraServices.AudioTags;

public static class AudioTagServiceExtensions
{
 public static IServiceCollection AddAudioTagService(this IServiceCollection services)
 {
 services.AddSingleton<IAudioTagService, AudioTagService>();
 return services;
 }
}
