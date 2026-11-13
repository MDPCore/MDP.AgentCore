using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace MDP.Companies.Entities;

// 記得在 Program.cs 加上 services.AddSingleton<CustomerNoteContext>();
public class CustomerNoteContext
{
    private ICustomerNoteRepository repo;
    public CustomerNoteContext(ICustomerNoteRepository repo)
    {
        this.repo = repo;
    }
    public void AddNote(CustomerNote note)
    {
        note.CustomerNoteId = Guid.NewGuid();
        note.CreateTime = DateTime.Now;
        repo.Add(note);
    }
    public void ExtendNote(string companyId, Guid customerNoteId)
    {
        var note = repo.FindByCustomerNoteId(companyId, customerNoteId);
        if (note == null)
        {
            return;
        }
        if (!(note.ExpireTime > DateTime.Now))
        {
            note.IsExpired = true;
            repo.Update(note);
            return;
        }
        note.ExpireTime = DateTime.Now.AddDays(7);
        repo.Update(note);
    }
}
