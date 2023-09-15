using Microsoft.Extensions.DependencyInjection;

namespace Tgs.Sms.ChuanXin.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddChuanXinSms(this IServiceCollection services, Action<IHttpClientBuilder> configureHttpClient)
	{
		configureHttpClient?.Invoke(services.AddHttpClient<ChuanXinApi>());

		return services;
	}
}
