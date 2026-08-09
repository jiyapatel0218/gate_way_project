using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SocietyGatekeeper.Application.Interfaces;

namespace SocietyGatekeeper.Infrastructure.Payments;

/// <summary>
/// Real Razorpay connector: creates an Order via the Orders API and verifies the
/// payment signature Razorpay returns after checkout (HMAC-SHA256 of "{order_id}|{payment_id}"
/// keyed with the account's Key Secret). Requires PaymentGateway:Razorpay:KeyId/KeySecret.
/// </summary>
public class RazorpayPaymentGatewayService : IPaymentGatewayService
{
    private readonly HttpClient _httpClient;
    private readonly string _keyId;
    private readonly string _keySecret;

    public RazorpayPaymentGatewayService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress ??= new Uri("https://api.razorpay.com/v1/");

        _keyId = configuration["PaymentGateway:Razorpay:KeyId"] ?? string.Empty;
        _keySecret = configuration["PaymentGateway:Razorpay:KeySecret"] ?? string.Empty;

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyId}:{_keySecret}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    public string ProviderName => "Razorpay";

    public async Task<PaymentOrderResult> CreateOrderAsync(decimal amount, string currency, string receipt)
    {
        var payload = new
        {
            amount = (int)Math.Round(amount * 100, MidpointRounding.AwayFromZero), // Razorpay expects the smallest currency unit (paise)
            currency,
            receipt
        };

        var response = await _httpClient.PostAsJsonAsync("orders", payload);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(stream);
        var orderId = json.GetProperty("id").GetString()!;

        return new PaymentOrderResult(orderId, _keyId, amount, currency);
    }

    public bool VerifyPayment(string providerOrderId, string providerPaymentId, string? signature)
    {
        if (string.IsNullOrEmpty(signature)) return false;

        var payload = Encoding.UTF8.GetBytes($"{providerOrderId}|{providerPaymentId}");
        var key = Encoding.UTF8.GetBytes(_keySecret);
        var computedHash = Convert.ToHexString(HMACSHA256.HashData(key, payload)).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }
}
