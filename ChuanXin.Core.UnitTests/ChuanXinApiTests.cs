using System.Net;
using System.Text.Json;
using RichardSzalay.MockHttp;
using Tgs.Sms.ChuanXin;

namespace ChuanXin.Core.UnitTests;

public class ChuanXinApiTests
{
    [Fact]
    public async Task 傳送簡訊()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var sut = new ChuanXinApi(httpClient);

        var appKey = "11111";
        var appCode = "1000";
        var appSecret = "22222";
        var phone = "15100000044";
        var message = "test123";

        mockHttp.Expect("http://localhost/*")
            .WithQueryString(new Dictionary<string, string>
            {
                ["appkey"] = appKey,
                ["appcode"] = appCode,
                ["appsecret"] = appSecret,
                ["phone"] = phone,
                ["msg"] = message
            })
            .Respond(
                "application/json",
                JsonSerializer.Serialize(new ChuanXinBatchResponse(
                    "00000",
                    "提交成功",
                    "ee6ae3ebeaf24f17b27a8fb5402de3b1")));

        // Act
        await sut.SendMessageAsync(appKey, appCode, appSecret, phone, message);
    }

    [Fact]
    public async Task 傳送簡訊_傳信回應StatusCode不是200()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var sut = new ChuanXinApi(httpClient);

        var appKey = "11111";
        var appCode = "1000";
        var appSecret = "22222";
        var phone = "15100000044";
        var message = "test123";

        mockHttp.Expect("http://localhost/*")
            .Respond(HttpStatusCode.InternalServerError);

        // Act
        _ = await Assert.ThrowsAsync<HttpRequestException>(async () => await sut.SendMessageAsync(
            appKey,
            appCode,
            appSecret,
            phone,
            message));
    }

    [Fact]
    public async Task 傳送簡訊_傳信回應內容_Code不是00000_視為錯誤()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var sut = new ChuanXinApi(httpClient);

        var appKey = "11111";
        var appCode = "1000";
        var appSecret = "22222";
        var phone = "15100000044";
        var message = "test123";

        mockHttp.Expect("http://localhost/*")
            .Respond(
                "application/json",
                JsonSerializer.Serialize(new ChuanXinBatchResponse(
                    "10000",
                    "提交成功",
                    "ee6ae3ebeaf24f17b27a8fb5402de3b1")));

        // Act
        _ = await Assert.ThrowsAsync<Exception>(async () => await sut.SendMessageAsync(
            appKey,
            appCode,
            appSecret,
            phone,
            message));
    }

    [Fact]
    public async Task 傳送簡訊時_會把電話號碼前面的加號去掉()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost");

        var sut = new ChuanXinApi(httpClient);

        var appKey = "11111";
        var appCode = "1000";
        var appSecret = "22222";
        var phone = "15100000044";
        var message = "test123";

        mockHttp.Expect("http://localhost/*")
            .WithQueryString(new Dictionary<string, string>
            {
                ["appkey"] = appKey,
                ["appcode"] = appCode,
                ["appsecret"] = appSecret,
                ["phone"] = phone,
                ["msg"] = message
            })
            .Respond(
                "application/json",
                JsonSerializer.Serialize(new ChuanXinBatchResponse(
                    "00000",
                    "提交成功",
                    "ee6ae3ebeaf24f17b27a8fb5402de3b1")));

        // Act
        await sut.SendMessageAsync(appKey, appCode, appSecret, "+" + phone, message);
    }
}
