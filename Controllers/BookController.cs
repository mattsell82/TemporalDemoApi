using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TemporalDemoApi.Model;
using System.Linq;
using System.Collections;
using System.Dynamic;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore.Internal;

namespace TemporalDemoApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BookController : ControllerBase
    {
        private readonly TemporalDbContext _context;
        private readonly ILogger<BookController> _logger;

        public BookController(
            TemporalDbContext temporalDbContext,
            ILogger<BookController> logger)
        {
            _context = temporalDbContext;
            _logger = logger;
        }


        [HttpGet]
        public async Task<IEnumerable<Book>> Get()
        {
            return await _context.Books.Include(x => x.Authors).ToListAsync();

        }

        [HttpPost]
        public async Task<int> Create(CreateBookDto book)
        {
            var result = await _context.Books.AddAsync(new Book(book.Title));
            await _context.SaveChangesAsync();

            return result.Entity.Id;
        }

        [HttpPut]
        public async Task<IActionResult> Update(Book book)
        {
            var existing = await _context.Books.FindAsync(book.Id);

            try
            {
                existing.Title = book.Title;
                existing.Price = book.Price;
                existing.ConcurrencyToken = Guid.NewGuid();

                await _context.SaveChangesAsync();
                return Ok(existing);

            }
            catch (Exception)
            {
                return BadRequest("Unable to update book");
            }

        }



        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var book = await _context.Books.FindAsync(id);

            if (book is null)
                return BadRequest("Book could not be found.");


            var result = _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return Ok("Book deleted with id: " +  result.Entity.Id);
        }

        [HttpPost]
        [Route("AddAuthor")]
        public async Task<IActionResult> AddAuthorToBook(BookAuthor bookAuthor)
        {
            var author = await _context.Author.FindAsync(bookAuthor.authorId);
            author.ConcurrencyToken = Guid.NewGuid();

            var book = await _context.Books.FindAsync(bookAuthor.bookId);
            book.ConcurrencyToken = Guid.NewGuid();

            if (author is null || book is null)
                return BadRequest("Unable to complete operation.");


            book.Authors.Add(author);

            try
            {
                await _context.SaveChangesAsync();

                return Ok($"{author.FirstName} {author.LastName} added to {book.Title}");
            }
            catch (Exception)
            {
                return BadRequest("Unable to save changes.");
            }

        }

        [HttpPost]
        [Route("RemoveAuthor")]
        public async Task<IActionResult> RemoveAuthorFromBook(BookAuthor bookAuthor)
        {
            var author = await _context.Author.FindAsync(bookAuthor.authorId);
            var book = await _context.Books.Include(x => x.Authors).Where(x => x.Id == bookAuthor.bookId).FirstOrDefaultAsync();

            if (author is null || book is null)
                return BadRequest("Unable to complete operation.");

            try
            {
                book.Authors.Remove(author);
                var result = await _context.SaveChangesAsync();

                return Ok($"{author.FirstName} {author.LastName} has been removed from {book.Title}");
            }
            catch (Exception)
            {
                return BadRequest("Unable to save changes.");
            }
        }



        [HttpGet]
        [Route("History/{id}")]
        public async Task<IEnumerable<TemporalResult<Book>>> GetHistory(int id)
        {
            return await _context.Books
                .TemporalAll()
                .Where(x => x.Id == id)
                .OrderBy(x => EF.Property<DateTime>(x, "PeriodStart"))
                .Select(x => new TemporalResult<Book>(x, EF.Property<DateTime>(x, "PeriodStart"), EF.Property<DateTime>(x, "PeriodEnd")))
                .ToListAsync();
        }

        [HttpGet]
        [Route("HistoryRelation/{id}")]
        public async Task<IEnumerable<TemporalResult<Book>>> GetHistoryRelational(int id)
        {
            var historical = await GetHistory(id);

            var dates = historical.Select(x => x.validFrom).ToList();

            List<TemporalResult<Book>> results = new ();

            foreach (var date in dates)
            {
                results.Add(await GetHistory(id, date));
            }

            return results;
        }


        [HttpGet]
        [Route("History/{id}/{dateTime}")]
        public async Task<TemporalResult<Book>> GetHistory(int id, DateTime dateTime)
        {
            return await _context.Books
                .TemporalAsOf(dateTime)
                .Where(x => x.Id == id)
                .Include(x => x.Authors)
                .Select(x => new TemporalResult<Book>(x, EF.Property<DateTime>(x, "PeriodStart"), EF.Property<DateTime>(x, "PeriodEnd")))
                .FirstAsync();
        }

    }

    public record TemporalResult<T>(T Value, DateTime validFrom, DateTime validTo) where T : class
    {
    }

}
