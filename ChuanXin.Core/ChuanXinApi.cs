using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

namespace Tgs.Sms.ChuanXin;

internal class ChuanXinApi
{
	private readonly HttpClient _httpClient;

	public ChuanXinApi(HttpClient httpClient)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
	}

	public async ValueTask SendMessageAsync(
		string appKey,
		string appCode,
		string appSecret,
		string phone,
		string message,
		CancellationToken cancellationToken = default)
	{
		var query = new QueryString()
			.Add("appkey", appKey)
			.Add("appcode", appCode)
			.Add("appsecret", appSecret)
			.Add("phone", phone.TrimStart('+'))
			.Add("msg", message);

		var response = await _httpClient.PostAsync($"/sms/batch/v2{query.ToUriComponent()}", null, cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<ChuanXinBatchResponse>(cancellationToken: cancellationToken)
				.ToAsyncEnumerable()
				.Select(res => res!))
			.FirstAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (response.Code != "00000")
			throw new Exception("Send message fail.");
	}
}
