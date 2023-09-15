using System.Text.Json.Serialization;

namespace Tgs.Sms.ChuanXin;
public record SmsSettings(
	[property: JsonPropertyName("appkey")] string AppKey,
	[property: JsonPropertyName("appsecret")] string AppSecret,
	[property: JsonPropertyName("appcode")] string AppCode);
