using System.ComponentModel.DataAnnotations;
using SocietyGatekeeper.Application.Validation;

namespace SocietyGatekeeper.Application.DTOs;

public record LoginRequest(
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required] string Password
);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    Guid? SocietyId
);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, StrongPassword] string NewPassword
);

public record ForgotPasswordRequest([Required, EmailAddress, StringLength(256)] string Email);

public record ResetPasswordRequest(
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required] string Token,
    [Required, StrongPassword] string NewPassword
);

public record ForgotPasswordOtpRequest(
    [Required, StringLength(256, MinimumLength = 3)] string Identifier,
    [Required, RegularExpression("^(Email|Sms)$", ErrorMessage = "DeliveryMethod must be 'Email' or 'Sms'.")] string DeliveryMethod
);

public record VerifyResetOtpRequest(
    [Required, StringLength(256, MinimumLength = 3)] string Identifier,
    [Required, StringLength(6, MinimumLength = 6)] string Otp
);

public record VerifyResetOtpResponse(string Email, string ResetToken);

public record RefreshTokenRequest(
    [Required] string AccessToken,
    [Required] string RefreshToken
);

public record RegisterResidentRequest(
    [Required, StringLength(150, MinimumLength = 2)] string FullName,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, Phone, StringLength(20)] string PhoneNumber,
    [Required, StrongPassword] string Password,
    [Required] Guid FlatId,
    bool IsOwner,
    [StringLength(500)] string? ProfileImageUrl
);
