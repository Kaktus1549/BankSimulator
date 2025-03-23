using System.ComponentModel.DataAnnotations;

public class LoginDTO{
    [Required (ErrorMessage = "Username is required")]
    public required string Email { get; set; }
    [Required (ErrorMessage = "Password is required")]
    public required string Password { get; set; }
}

public class RegisterDTO{
    [Required (ErrorMessage = "First name is required")]
    public required string FirstName { get; set; }
    [Required (ErrorMessage = "Last name is required")]
    public required string LastName { get; set; }
    [Required (ErrorMessage = "Email is required")]
    public required string Email { get; set; }
    [Required (ErrorMessage = "Password is required")]
    public required string Password { get; set; }
    [Required (ErrorMessage = "Role is required")]
    public required Role Role { get; set; } = Role.User;
    public bool Student { get; set; }
    public decimal FreeAccountBalance { get; set; } = 0;
    public decimal SavingAccountBalance { get; set; } = 0;
    public decimal CreditAccountBalance { get; set; } = 0;
}

public class ChangeRoleDTO{
    [Required (ErrorMessage = "Email is required")]
    public required string Email { get; set; }
    [Required (ErrorMessage = "Role is required")]
    public required Role Role { get; set; }
}

public class DeleteUserDTO{
    [Required (ErrorMessage = "Email is required")]
    public required string Email { get; set; }
}

public class transactionsByUserDTO{
    [Required (ErrorMessage = "Email is required")]
    public required string Email { get; set; }
}

public class transactionsByAccountDTO{
    [Required (ErrorMessage = "Account number is required")]
    public required string AccountNumber { get; set; }
}

public class transferDTO{
    [Required (ErrorMessage = "From account number is required")]
    public required string FromAccountNumber { get; set; }
    [Required (ErrorMessage = "To account number is required")]
    public required string ToAccountNumber { get; set; }
    [Required (ErrorMessage = "Amount is required")]
    public required decimal Amount { get; set; }
}