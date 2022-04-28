using GenesisAddressBook.Data;
using GenesisAddressBook.Models;
using GenesisAddressBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GenesisAddressBook.Services
{
    public class AddressBookService : IAddressBookService
    {
        private readonly ApplicationDbContext _context;

        public AddressBookService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task AddContactToCategoryAsync(int categoryId, int contactId)
        {
            try
            {
                if(!await IsContactInCategory(categoryId, contactId))
                {
                    Contact? contact = await _context.Contacts.FindAsync(contactId);
                    Category? category = await _context.Categories.FindAsync(categoryId);

                    if(category != null && contact != null)
                    {
                        category.Contacts.Add(contact);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<bool> IsContactInCategory(int categoryId, int contactId)
        {
            try
            {
                Contact? contact = await _context.Contacts.FindAsync(contactId);

                return await _context.Categories.Include(c => c.Contacts)
                                                .Where(c => c.Id == categoryId && c.Contacts.Contains(contact))
                                                .AnyAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
