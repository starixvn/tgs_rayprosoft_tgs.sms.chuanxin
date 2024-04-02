using Microsoft.Extensions.Logging;
using Tgs.BrandCertificate.Clients;
using Tgs.Sms.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Sms.ChuanXin.MessageQueue;

internal class ChuanXinReceiveHandler(
	IBrandServiceClient brandServiceClient,
	ChuanXinApi chuanXinApi,
	ILogger<ChuanXinReceiveHandler> logger)
	: IMessageHandler<ReadOnlyMemory<byte>>
{
	private readonly IBrandServiceClient _brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
	private readonly ChuanXinApi _chuanXinApi = chuanXinApi ?? throw new ArgumentNullException(nameof(chuanXinApi));
	private readonly ILogger<ChuanXinReceiveHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public async ValueTask HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var shortMsg = ShortMessage.Parser.ParseFrom(data.Span);
		var brandId = shortMsg.BrandId.ToGuid();

		_logger.LogInformation(
			"Receive sms: [{brandIdKey}] {brandId} " +
			"[{destinationKey}] {destination} " +
			"[{contentKey}] {content}",
			nameof(shortMsg.BrandId),
			brandId,
			nameof(shortMsg.Destination),
			shortMsg.Destination,
			nameof(shortMsg.Content),
			shortMsg.Content);

		var brandInfo = await _brandServiceClient.GetBrandInfoAsync(
			brandId: brandId,
			cancellationToken).ConfigureAwait(false);

		if (brandInfo == null)
		{
			_logger.LogInformation("Can't find brand info ({brandId})", brandId);

			return;
		}

		if (!brandInfo.SmsEnabled || brandInfo == null)
		{
			_logger.LogWarning("Brand setting warn");

			return;
		}

		var smsInfo = await _brandServiceClient.GetBrandSmsSettingsAsync<SmsSettings>(
			brandId,
			cancellationToken: cancellationToken).ConfigureAwait(false);

		if (smsInfo is null)
		{
			_logger.LogWarning("Can't found sms settings({brandId})", brandId);

			return;
		}

		try
		{
			await _chuanXinApi.SendMessageAsync(
				smsInfo.SmsSettings.AppKey,
				smsInfo.SmsSettings.AppCode,
				smsInfo.SmsSettings.AppSecret,
				$"{shortMsg.AreaCode}{shortMsg.Phone.TrimStart('0')}",
				shortMsg.Content,
				cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogError("ChuanXin SendMessage Failed, Message:{0}", ex.Message);
		}
	}
}