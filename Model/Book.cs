using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text.Json.Serialization;

namespace TemporalDemoApi.Model
{
    public class Book(string title)
    {
        [Key]
        public int Id { get; set; }

        public string Title { get; set; } = title;

        public decimal Price { get; set; }

        public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();

        [JsonIgnore]
        public List<Author> Authors { get; set; } = new List<Author>();

        [NotMapped]
        public List<string> AuthorNames => Authors.Select(x => x.FirstName + " " + x.LastName).ToList();

    }

    public record CreateBookDto(string Title);
    public record EditBookDto(int Id, string? Title);

    public record BookAuthor(int bookId, int authorId);


    public class Author
    {
        [Key]
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public List<Book> Books { get; set; } = new List<Book>();

        public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();

        public Author(string firstName, string lastName, int id)
        {
            FirstName = firstName;
            LastName = lastName;
            Id = id;
        }

        public Author() { }

    }

}
