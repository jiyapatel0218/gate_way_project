namespace SocietyGatekeeper.Application.Interfaces;

public record PaymentOrderResult(string ProviderOrderId, string? CheckoutKeyId, decimal Amount, string Currency);

public interface IPaymentGatewayService
{
    string ProviderName { get; }
    Task<PaymentOrderResult> CreateOrderAsync(decimal amount, string currency, string receipt);
    bool VerifyPayment(string providerOrderId, string providerPaymentId, string? signature);
}
