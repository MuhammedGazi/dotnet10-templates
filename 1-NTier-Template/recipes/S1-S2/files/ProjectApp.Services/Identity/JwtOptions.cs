using System.ComponentModel.DataAnnotations;

namespace ProjectApp.Services.Identity;

public class JwtOptions : IValidatableObject
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 7;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(SigningKey))
        {
            yield return new ValidationResult("Jwt:SigningKey boş olamaz.", [nameof(SigningKey)]);
        }
        else if (SigningKey.Length < 32)
        {
            yield return new ValidationResult("Jwt:SigningKey en az 32 karakter uzunluğunda olmalıdır.", [nameof(SigningKey)]);
        }

        if (string.IsNullOrWhiteSpace(Issuer))
        {
            yield return new ValidationResult("Jwt:Issuer boş olamaz.", [nameof(Issuer)]);
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            yield return new ValidationResult("Jwt:Audience boş olamaz.", [nameof(Audience)]);
        }

        if (AccessTokenMinutes <= 0)
        {
            yield return new ValidationResult("Jwt:AccessTokenMinutes 0'dan büyük olmalıdır.", [nameof(AccessTokenMinutes)]);
        }

        if (RefreshTokenDays <= 0)
        {
            yield return new ValidationResult("Jwt:RefreshTokenDays 0'dan büyük olmalıdır.", [nameof(RefreshTokenDays)]);
        }
    }
}
