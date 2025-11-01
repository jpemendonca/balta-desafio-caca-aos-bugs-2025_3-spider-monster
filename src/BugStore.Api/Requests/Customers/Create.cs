using System.ComponentModel.DataAnnotations;

namespace BugStore.Api.Requests.Customers;

public class Create
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;
    
    [Required]
    public DateTime BirthDate { get; set; }
}