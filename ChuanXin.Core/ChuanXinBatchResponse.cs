using System.Text.Json.Serialization;

namespace Tgs.Sms.ChuanXin;
public record ChuanXinBatchResponse(
	[property: JsonPropertyName("code")] string Code,
	[property: JsonPropertyName("desc")] string Description,
	[property: JsonPropertyName("uid")] string Uid);
