using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.BrandCertificate.Clients;
using Tgs.Sms.ChuanXin;
using Tgs.Sms.ChuanXin.MessageQueue;
using Tgs.Sms.Protos;

namespace ChuanXin.Core.UnitTests;

public class ChuanXinReceiveHandlerTests
{
    [Fact]
    public async Task 接收到發送簡訊要求()
    {
        // Arrange
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
        var mockHttp = new MockHttpMessageHandler();
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var sut = new ChuanXinReceiveHandler(
            fakeBrandServiceClient,
            new ChuanXinApi(httpClient),
            NullLogger<ChuanXinReceiveHandler>.Instance);

        var brandId = Guid.NewGuid();
        var request = new ShortMessage
        {
            BrandId = brandId,
            Content = "test123",
            Destination = "+15100000044"
        };

        _ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
            .Returns(new BrandInfo
            {
                Id = brandId,
                SmsEnabled = true
            });

        var vendorInfo = new SmsVendorInfo<SmsSettings>(
            "Test",
            new SmsSettings("11111", "22222", "1000"));
        _ = fakeBrandServiceClient.GetBrandSmsSettingsAsync<SmsSettings>(Arg.Is(brandId))
            .Returns(vendorInfo);

        mockHttp.Expect("http://localhost/*")
            .WithQueryString(new Dictionary<string, string>
            {
                ["appkey"] = vendorInfo.SmsSettings.AppKey,
                ["appcode"] = vendorInfo.SmsSettings.AppCode,
                ["appsecret"] = vendorInfo.SmsSettings.AppSecret,
                ["phone"] = request.Destination.Trim('+'),
                ["msg"] = request.Content
            })
            .Respond(
                "application/json",
                JsonSerializer.Serialize(new ChuanXinBatchResponse(
                    "00000",
                    "提交成功",
                    "ee6ae3ebeaf24f17b27a8fb5402de3b1")));

        // Act
        await sut.HandleAsync("test", request.ToByteArray(), null);
    }
}