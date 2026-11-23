using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PrjProje.Application.Models;

/// <summary>
/// Validation messages and display names are resource keys resolved through SharedResource (.resx).
/// </summary>
public class FairLeadFormModel
{
    // Whether the e-mail is required is a per-fair setting, checked in FairController.
    [Display(Name = "Form.Email")]
    [EmailAddress(ErrorMessage = "Validation.Email")]
    [StringLength(254, ErrorMessage = "Validation.MaxLength")]
    public string? Email { get; set; }

    public bool MarketingConsent { get; set; }
}

public class ContactFormModel
{
    [Display(Name = "Form.FullName")]
    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(120, ErrorMessage = "Validation.MaxLength")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Form.Company")]
    [StringLength(150, ErrorMessage = "Validation.MaxLength")]
    public string? Company { get; set; }

    [Display(Name = "Form.Email")]
    [Required(ErrorMessage = "Validation.Required")]
    [EmailAddress(ErrorMessage = "Validation.Email")]
    [StringLength(150, ErrorMessage = "Validation.MaxLength")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Form.Phone")]
    [Phone(ErrorMessage = "Validation.Phone")]
    [StringLength(30, ErrorMessage = "Validation.MaxLength")]
    public string? Phone { get; set; }

    [Display(Name = "Form.Subject")]
    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(200, ErrorMessage = "Validation.MaxLength")]
    public string Subject { get; set; } = string.Empty;

    [Display(Name = "Form.Message")]
    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "Validation.Length")]
    public string Message { get; set; } = string.Empty;

    [Display(Name = "Form.PrivacyConsent")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "Validation.Consent")]
    public bool PrivacyConsent { get; set; }
}

public class JobApplicationFormModel
{
    [Display(Name = "Form.FullName")]
    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(120, ErrorMessage = "Validation.MaxLength")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Form.Email")]
    [Required(ErrorMessage = "Validation.Required")]
    [EmailAddress(ErrorMessage = "Validation.Email")]
    [StringLength(150, ErrorMessage = "Validation.MaxLength")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Form.Phone")]
    [Required(ErrorMessage = "Validation.Required")]
    [Phone(ErrorMessage = "Validation.Phone")]
    [StringLength(30, ErrorMessage = "Validation.MaxLength")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Careers.City")]
    [StringLength(80, ErrorMessage = "Validation.MaxLength")]
    public string? City { get; set; }

    /// <summary>One of the area keys listed on the careers page (Careers.Area.*).</summary>
    [Display(Name = "Careers.Position")]
    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(40, ErrorMessage = "Validation.MaxLength")]
    public string Position { get; set; } = string.Empty;

    [Display(Name = "Careers.CoverLetter")]
    [StringLength(4000, ErrorMessage = "Validation.MaxLength")]
    public string? Message { get; set; }

    [Display(Name = "Careers.Resume")]
    [Required(ErrorMessage = "Validation.Required")]
    public IFormFile? Resume { get; set; }

    [Display(Name = "Form.PrivacyConsent")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "Validation.Consent")]
    public bool PrivacyConsent { get; set; }
}

public class QuoteFormModel
{
    [Display(Name = "Form.FullName")]
    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(120, ErrorMessage = "Validation.MaxLength")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Form.CompanyName")]
    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(150, ErrorMessage = "Validation.MaxLength")]
    public string CompanyName { get; set; } = string.Empty;

    [Display(Name = "Form.Email")]
    [Required(ErrorMessage = "Validation.Required")]
    [EmailAddress(ErrorMessage = "Validation.Email")]
    [StringLength(150, ErrorMessage = "Validation.MaxLength")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Form.Phone")]
    [Required(ErrorMessage = "Validation.Required")]
    [Phone(ErrorMessage = "Validation.Phone")]
    [StringLength(30, ErrorMessage = "Validation.MaxLength")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Form.Product")]
    public int? ProductId { get; set; }

    [Display(Name = "Form.ProductName")]
    [StringLength(200, ErrorMessage = "Validation.MaxLength")]
    public string? ProductName { get; set; }

    [Display(Name = "Form.Quantity")]
    [StringLength(50, ErrorMessage = "Validation.MaxLength")]
    public string? Quantity { get; set; }

    [Display(Name = "Form.Description")]
    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "Validation.Length")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Form.Attachment")]
    public IFormFile? Attachment { get; set; }

    [Display(Name = "Form.PrivacyConsent")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "Validation.Consent")]
    public bool PrivacyConsent { get; set; }
}

public record RequestContext(string LanguageCode, string? IpAddress);
