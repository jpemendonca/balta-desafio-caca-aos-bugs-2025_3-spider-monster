using System.ComponentModel.DataAnnotations;

namespace BugStore.Api.Requests.Products;

public class Update
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Range(0.01, (double)decimal.MaxValue)]
    public decimal Price { get; set; }
}