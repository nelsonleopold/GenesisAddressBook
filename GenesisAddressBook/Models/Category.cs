using System.ComponentModel.DataAnnotations;

namespace GenesisAddressBook.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string? AppUserId { get; set; }

        [Required]
        [Display(Name = "Category Name")]
        public string? Name { get; set; }

        //TODO: Add virtuals

    }
}
