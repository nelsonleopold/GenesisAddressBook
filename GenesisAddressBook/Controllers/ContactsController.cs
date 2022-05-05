#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GenesisAddressBook.Data;
using GenesisAddressBook.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using GenesisAddressBook.Enums;
using GenesisAddressBook.Services.Interfaces;
using GenesisAddressBook.Services;
using GenesisAddressBook.Models.ViewModels;

namespace GenesisAddressBook.Controllers
{
    public class ContactsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IAddressBookService _addressBookService;
        private readonly IImageService _imageService;
        private readonly SearchService _searchService;
        private readonly IAddressBookEmailSender _emailSender;

        public ContactsController(ApplicationDbContext context,
                                  UserManager<AppUser> userManager,
                                  IAddressBookService addressBookService,
                                  IImageService imageService,
                                  SearchService searchService,
                                  IAddressBookEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _addressBookService = addressBookService;
            _imageService = imageService;
            _searchService = searchService;
            _emailSender = emailSender;
        }

        // GET: Contacts
        [Authorize]
        public async Task<IActionResult> Index(int id)
        {
            List<Contact> contacts = new List<Contact>();
            // AppUser appUser = await _userManager.GetUserAsync(User);
            string appUserId = _userManager.GetUserId(User);
            AppUser appUser = _context.Users.Include(c => c.Contacts)
                                                .ThenInclude(c => c.Categories)
                                            .FirstOrDefault(u => u.Id == appUserId);

            if (id == 0)
            {
                contacts = appUser.Contacts.ToList();
            }
            else
            {
                contacts = appUser.Categories.FirstOrDefault(c => c.Id == id).Contacts.ToList();
            }

            ViewData["CategoryId"] = new SelectList(appUser.Categories, "Id", "Name", id);

            return View(contacts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SearchContacts(string searchString)
        {
            var userId = _userManager.GetUserId(User);
            List<Contact> contacts = _searchService.SearchContacts(searchString, userId).ToList();

            return View(nameof(Index), contacts);
        }

        // Email
        // GET: Contacts/EmailContact
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EmailContact(int id)
        {
            Contact contact = await _context.Contacts.Include(c => c.Categories)
                                                     .FirstOrDefaultAsync(c => c.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            EmailData emailData = new()
            {
                EmailAddress = contact.Email,
                FirstName = contact.FirstName,
                LastName = contact.LastName
            };

            EmailContactViewModel model = new()
            {
                Contact = contact,
                EmailData = emailData
            };


            return View(model);
        }

        // POST: Contacts/EmailContact
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailContact(EmailData emailData)
        {
            if (ModelState.IsValid)
            {
                AppUser appUser = await _userManager.GetUserAsync(User);
                string emailBody = _emailSender.ComposeEmailBody(appUser, emailData);

                await _emailSender.SendEmailAsync(emailData.EmailAddress, emailData.Subject, emailBody);

                return RedirectToAction("Index", "Contacts");
            }

            return View();
        }


        // GET: Contacts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Contact contact = await _context.Contacts
                .Include(c => c.AppUser)
                .Include(c => c.Categories)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // GET: Contacts/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            string appUserId = _userManager.GetUserId(User);

            //ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "Id");
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>().ToList());
            ViewData["CategoryList"] = new MultiSelectList(await _addressBookService.GetUserCategoriesAsync(appUserId), "Id", "Name");
            return View();
        }

        // POST: Contacts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,BirthDate,Address1,Address2,City,State,ZipCode,Email,PhoneNumber,ImageFile")] Contact contact, List<int> categoryList)
        {
            // ModelState.Remove("AppUserId");
            // AppUserId is null but required when the model comes in
            if (ModelState.IsValid || contact.AppUserId == null)
            {
                contact.AppUserId = _userManager.GetUserId(User);
                contact.Created = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);

                if (contact.BirthDate != null)
                {
                    contact.BirthDate = DateTime.SpecifyKind(contact.BirthDate.Value, DateTimeKind.Utc);
                }

                if (contact.ImageFile != null)
                {
                    //TODO: Image Service
                    contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                    contact.ImageType = contact.ImageFile.ContentType;
                }



                _context.Add(contact);
                await _context.SaveChangesAsync();

                // Add Contact to Categories
                
                foreach (int categoryId in categoryList)
                {
                    await _addressBookService.AddContactToDefaultCategory(categoryId, contact.Id);
                    await _addressBookService.AddContactToCategoryAsync(categoryId, contact.Id);
                }


                return RedirectToAction(nameof(Index));
            }
            string appUserId = _userManager.GetUserId(User);

            // ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "Id", contact.AppUserId);
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>().ToList());
            ViewData["CategoriesList"] = new MultiSelectList(await _addressBookService.GetUserCategoriesAsync(appUserId), "Id", "Name");
            return View(contact);
        }

        // GET: Contacts/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Contact contact = await _context.Contacts.FindAsync(id);

            if (contact == null)
            {
                return NotFound();
            }

            string appUserId = _userManager.GetUserId(User);

            // ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "Id", contact.AppUserId);
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>().ToList());
            ViewData["CategoryList"] = new MultiSelectList(await _addressBookService.GetUserCategoriesAsync(appUserId), "Id", "Name", await _addressBookService.GetContactCategoryIdsAsync(contact.Id));
            return View(contact);
        }

        // POST: Contacts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AppUserId,FirstName,LastName,BirthDate,Address1,Address2,City,State,ZipCode,Email,PhoneNumber,Created,ImageFile,ImageData,ImageType")] Contact contact, List<int> categoryList)
        {
            if (id != contact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // change time back to 
                    contact.Created = DateTime.SpecifyKind(contact.Created, DateTimeKind.Utc);

                    if (contact.BirthDate != null)
                    {
                        contact.BirthDate = DateTime.SpecifyKind(contact.BirthDate.Value, DateTimeKind.Utc);
                    }

                    if (contact.ImageFile != null)
                    {
                        //TODO: Image Service
                        contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                        contact.ImageType = contact.ImageFile.ContentType;
                    }

                    _context.Update(contact);
                    await _context.SaveChangesAsync();

                    List<Category> oldCategories = (await _addressBookService.GetContactCategoriesAsync(contact.Id)).ToList();
                    // Remove Contact from Categories
                    foreach (Category category in oldCategories)
                    {
                        await _addressBookService.RemoveContactFromCategoryAsync(category.Id, contact.Id);
                    }

                    // Add Contact to Categories
                    // 
                    foreach (int categoryId in categoryList)
                    {
                        await _addressBookService.AddContactToCategoryAsync(categoryId, contact.Id);
                        await _addressBookService.AddContactToDefaultCategory(categoryId, contact.Id);
                    }

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactExists(contact.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>().ToList());

            return View(contact);
        }

        // GET: Contacts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Contact contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Contact contact = await _context.Contacts.FindAsync(id);
            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContactExists(int id)
        {
            return _context.Contacts.Any(e => e.Id == id);
        }
    }
}
