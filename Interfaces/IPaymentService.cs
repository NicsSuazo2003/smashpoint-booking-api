using SmashPointBookingAPI.DTOs;

namespace SmashPointBookingAPI.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponse> ProcessAsync(ProcessPaymentRequest request);
}