using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MDP.Companies.Entities;

public class CustomerNote : IValidatableObject
{
    public DateTime CreateTime { get; set; }
    public Guid CustomerNoteId { get; set; }
    public string CompanyId { get; set; }
    public string CustomerId { get; set; }
    public string Content { get; set; }
    public bool IsExpired { get; set; }
    public DateTime ExpireTime { get; set; }

    public void Save(ICustomerNoteRepository repository)
    {
        repository.Update(this);
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Content)) yield return new ValidationResult("Content required");
    }
}
