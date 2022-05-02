using GenesisAddressBook.Models;
using GenesisAddressBook.Models.ViewModels;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace GenesisAddressBook.Services.Interfaces
{
    public interface IAddressBookEmailSender : IEmailSender
    {
        string ComposeEmailBody(AppUser sender, EmailData emailData);
        Task SendEmailAsync(AppUser appUser, List<Contact> contacts, EmailData emailData);
    }
}
