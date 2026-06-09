namespace iNaturalist_Lite.DTOs;

public class RegisterInput
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class UserInput
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public string? Email { get; set; } 
}

public class ForgotPasswordInput
{
    public required string Email { get; set; }
}

public class ResetPasswordInput
{
    public required string Email { get; set; }
    public required string OtpCode { get; set; }
    public required string NewPassword { get; set; }
}

public class ChangePasswordInput
{
    public required string OldPassword { get; set; }
    public required string NewPassword { get; set; }
}
