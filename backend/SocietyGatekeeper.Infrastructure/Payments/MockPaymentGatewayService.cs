using SocietyGatekeeper.Application.Interfaces;

namespace SocietyGatekeeper.Infrastructure.Payments;

/// <summary>
/// Simulated gateway for local development. Always approves payments so the
/// full online-payment flow (order -> checkout -> verify -> mark paid) can be
/// exercised without real gateway credentials. Swap the "PaymentGateway:Provider"
/// config to "Razorpay" (with real keys) to switch to the live gateway.
/// </summary>
public class MockPaymentGatewayService : IPaymentGatewayService
{
    public string ProviderName => "Mock";

    public Task<PaymentOrderResult> CreateOrderAsync(decimal amount, string currency, string receipt)
    {
        var orderId = $"order_mock_{Guid.NewGuid():N}";
        return Task.FromResult(new PaymentOrderResult(orderId, null, amount, currency));
    }

    public bool VerifyPayment(string providerOrderId, string providerPaymentId, string? signature)
    {
        return !string.IsNullOrWhiteSpace(providerOrderId) && !string.IsNullOrWhiteSpace(providerPaymentId);
    }
}
