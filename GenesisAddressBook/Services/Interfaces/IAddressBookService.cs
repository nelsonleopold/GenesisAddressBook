namespace GenesisAddressBook.Services.Interfaces
{
    public interface IAddressBookService
    {
        Task AddContactToCategoryAsync(int categoryId, int contactId);

        Task<bool> IsContactInCategory(int categoryId, int contactId);
    }
}
