using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Google.Protobuf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.BrandCertificate.Clients;
using Tgs.Sms.ChuanXin.MessageQueue;
using Tgs.Sms.Protos;
using Valhalla.MessageQueue;
using Xunit.Abstractions;

namespace Tgs.Sms.ChuanXin.IntegrationTests;

public class HandleMessageTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public HandleMessageTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
    }

    [Fact]
    public async Task 處理傳送簡訊要求()
    {
        // Arrange
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
        var mockHttp = new MockHttpMessageHandler();

        await using var web = ChuanXinTestHost.Instance
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakeBrandServiceClient)
                    .AddMessageQueue()
                    .AddDirectMessageQueue(config => config
                        .AddHandler(ProcessorTypes.ChuanXinReceiveHandlerType, "tgs.sms.chuan-xin"))
                    .ClearAllExchanges()
                    .AddDirectGlobPatternExchange("*")
                    .Services
                    .AddHttpClient<ChuanXinApi>()
                    .ConfigurePrimaryHttpMessageHandler(_ => mockHttp)));

        var brandId = Guid.NewGuid();
        var request = new ShortMessage
        {
            BrandId = brandId,
            Content = "test123",
            Destination = "+15100000044"
        };

        var messageSender = web.Services.GetRequiredService<IMessageSender>();

        _ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId), Arg.Any<CancellationToken>())
            .Returns(new BrandInfo
            {
                Id = brandId,
                SmsEnabled = true
            });

        var vendorInfo = new SmsVendorInfo<SmsSettings>(
            "Test",
            new SmsSettings("11111", "22222", "1000"));
        _ = fakeBrandServiceClient.GetBrandSmsSettingsAsync<SmsSettings>(
            Arg.Is(brandId),
            Arg.Any<JsonSerializerOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(vendorInfo);

        var tcs = new TaskCompletionSource();
        mockHttp.Expect("http://localhost/sms/batch/v2")
            .WithQueryString(new Dictionary<string, string>
            {
                ["appkey"] = vendorInfo.SmsSettings.AppKey,
                ["appcode"] = vendorInfo.SmsSettings.AppCode,
                ["appsecret"] = vendorInfo.SmsSettings.AppSecret,
                ["phone"] = request.Destination.Trim('+'),
                ["msg"] = request.Content
            })
            .Respond(
                HttpStatusCode.OK,
                _ =>
                {
                    tcs.SetResult();

                    return JsonContent.Create(new ChuanXinBatchResponse(
                        "00000",
                        "提交成功",
                        "ee6ae3ebeaf24f17b27a8fb5402de3b1"));
                });

        // Act
        await messageSender.PublishAsync("tgs.sms.chuan-xin", request.ToByteArray());
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1D));
    }
}
