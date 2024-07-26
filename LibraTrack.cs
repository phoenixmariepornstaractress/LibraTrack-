using System;
using System.Collections.Generic;
using System.Linq;

namespace LibraryManagementSystem
{
    // Book class
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Genre { get; set; }
        public int PublicationYear { get; set; }
        public bool IsLoaned { get; set; }
        public List<Reservation> Reservations { get; set; }

        public Book(int id, string title, string author, string genre, int publicationYear)
        {
            Id = id;
            Title = title;
            Author = author;
            Genre = genre;
            PublicationYear = publicationYear;
            IsLoaned = false;
            Reservations = new List<Reservation>();
        }
    }

    // Patron class
    public class Patron
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string MembershipLevel { get; set; }
        public decimal FineBalance { get; private set; }

        public int ReservationLimit
        {
            get
            {
                return MembershipLevel switch
                {
                    "Premium" => 10,
                    "VIP" => 20,
                    _ => 5
                };
            }
        }

        public Patron(int id, string name, string email, string membershipLevel = "Regular")
        {
            Id = id;
            Name = name;
            Email = email;
            MembershipLevel = membershipLevel;
        }

        public void AddFine(decimal amount)
        {
            FineBalance += amount;
        }

        public void PayFine(decimal amount)
        {
            if (amount <= FineBalance)
            {
                FineBalance -= amount;
            }
        }
    }

    // Loan class
    public class Loan
    {
        public int BookId { get; set; }
        public int PatronId { get; set; }
        public DateTime LoanDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        public Loan(int bookId, int patronId, DateTime loanDate)
        {
            BookId = bookId;
            PatronId = patronId;
            LoanDate = loanDate;
            ReturnDate = null;
        }

        // Calculate if a loan is overdue
        public bool IsOverdue()
        {
            if (ReturnDate.HasValue) return false;
            return (DateTime.Now - LoanDate).TotalDays > 14; // Assuming 2 weeks loan period
        }

        // Calculate fine for overdue book
        public decimal CalculateFine()
        {
            if (!IsOverdue()) return 0;
            return (decimal)((DateTime.Now - LoanDate).TotalDays - 14) * 0.5m; // $0.50 per day fine
        }
    }

    // Reservation class
    public class Reservation
    {
        public int BookId { get; set; }
        public int PatronId { get; set; }
        public DateTime ReservationDate { get; set; }

        public Reservation(int bookId, int patronId, DateTime reservationDate)
        {
            BookId = bookId;
            PatronId = patronId;
            ReservationDate = reservationDate;
        }
    }

    // Library Management System class
    public class LibraryManagementSystem
    {
        private List<Book> books = new List<Book>();
        private List<Patron> patrons = new List<Patron>();
        private List<Loan> loans = new List<Loan>();
        private List<Reservation> reservations = new List<Reservation>();
        private int nextPatronId = 1;

        // Add a new book to the library
        public void AddBook(Book book)
        {
            books.Add(book);
        }

        // Remove a book from the library
        public bool RemoveBook(int bookId)
        {
            var book = books.FirstOrDefault(b => b.Id == bookId);
            if (book == null) return false;

            books.Remove(book);
            return true;
        }

        // Register a new patron
        public Patron RegisterPatron(string name, string email, string membershipLevel = "Regular")
        {
            var patron = new Patron(nextPatronId++, name, email, membershipLevel);
            patrons.Add(patron);
            return patron;
        }

        // Remove a patron from the library
        public bool RemovePatron(int patronId)
        {
            var patron = patrons.FirstOrDefault(p => p.Id == patronId);
            if (patron == null) return false;

            patrons.Remove(patron);
            return true;
        }

        // Loan a book to a patron
        public bool LoanBook(int bookId, int patronId)
        {
            var book = books.FirstOrDefault(b => b.Id == bookId);
            var patron = patrons.FirstOrDefault(p => p.Id == patronId);

            if (book == null || patron == null || book.IsLoaned)
            {
                return false;
            }

            // Check for reservations
            if (book.Reservations.Any(r => r.PatronId != patronId))
            {
                return false;
            }

            book.IsLoaned = true;
            loans.Add(new Loan(bookId, patronId, DateTime.Now));
            return true;
        }

        // Return a book
        public bool ReturnBook(int bookId)
        {
            var book = books.FirstOrDefault(b => b.Id == bookId);

            if (book == null || !book.IsLoaned)
            {
                return false;
            }

            var loan = loans.FirstOrDefault(l => l.BookId == bookId && l.ReturnDate == null);

            if (loan == null)
            {
                return false;
            }

            loan.ReturnDate = DateTime.Now;
            book.IsLoaned = false;

            // Check for reservations
            var reservation = book.Reservations.FirstOrDefault();
            if (reservation != null)
            {
                // Automatically loan the book to the next patron in line
                NotifyReservedBookAvailable(reservation.PatronId, bookId);
                book.Reservations.Remove(reservation);
            }

            return true;
        }

        // Reserve a book
        public bool ReserveBook(int bookId, int patronId)
        {
            var book = books.FirstOrDefault(b => b.Id == bookId);
            var patron = patrons.FirstOrDefault(p => p.Id == patronId);

            if (book == null || patron == null)
            {
                return false;
            }

            book.Reservations.Add(new Reservation(bookId, patronId, DateTime.Now));
            return true;
        }

        // Extend loan period
        public bool ExtendLoan(int bookId, int patronId)
        {
            var loan = loans.FirstOrDefault(l => l.BookId == bookId && l.PatronId == patronId && l.ReturnDate == null);
            if (loan == null || loan.IsOverdue()) return false;

            loan.LoanDate = DateTime.Now; // Reset the loan date
            return true;
        }

        // Search for books by title
        public List<Book> SearchBooksByTitle(string title)
        {
            return books.Where(b => b.Title.Contains(title, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Search for books by author
        public List<Book> SearchBooksByAuthor(string author)
        {
            return books.Where(b => b.Author.Contains(author, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Search for patrons by name
        public List<Patron> SearchPatronsByName(string name)
        {
            return patrons.Where(p => p.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Search for patrons by ID
        public Patron SearchPatronById(int id)
        {
            return patrons.FirstOrDefault(p => p.Id == id);
        }

        // Search for reservations by patron name
        public List<Reservation> SearchReservationsByPatronName(string patronName)
        {
            var patronIds = patrons.Where(p => p.Name.Contains(patronName, StringComparison.OrdinalIgnoreCase)).Select(p => p.Id).ToList();
            return reservations.Where(r => patronIds.Contains(r.PatronId)).ToList();
        }

        // Search for reservations by book title
        public List<Reservation> SearchReservationsByBookTitle(string bookTitle)
        {
            var bookIds = books.Where(b => b.Title.Contains(bookTitle, StringComparison.OrdinalIgnoreCase)).Select(b => b.Id).ToList();
            return reservations.Where(r => bookIds.Contains(r.BookId)).ToList();
        }

        // Get loan history
        public List<Loan> GetLoanHistory()
        {
            return loans;
        }

        // Get overdue loans
        public List<Loan> GetOverdueLoans()
        {
            return loans.Where(l => l.IsOverdue()).ToList();
        }

        // Notify patrons with overdue books
        public void NotifyOverdueBooks()
        {
            var overdueLoans = GetOverdueLoans();
            foreach (var loan in overdueLoans)
            {
                var patron = patrons.FirstOrDefault(p => p.Id == loan.PatronId);
                if (patron != null)
                {
                    Console.WriteLine($"Notification: Patron {patron.Name} has an overdue book (Book ID: {loan.BookId}). Fine: ${loan.CalculateFine()}");
                    SendEmailNotification(patron.Email, $"Overdue Book Notice", $"Dear {patron.Name},\n\nYou have an overdue book (Book ID: {loan.BookId}). Please return it as soon as possible. Your fine is ${loan.CalculateFine():0.00}.\n\nThank you.");
                }
            }
        }

        // Notify patrons when a reserved book becomes available
        public void NotifyReservedBookAvailable(int patronId, int bookId)
        {
            var patron = patrons.FirstOrDefault(p => p.Id == patronId);
            if (patron != null)
            {
                Console.WriteLine($"Notification: Patron {patron.Name} has a reserved book available (Book ID: {bookId}).");
                SendEmailNotification(patron.Email, "Reserved Book Available", $"Dear {patron.Name},\n\nThe book you reserved (Book ID: {bookId}) is now available for pickup.\n\nThank you.");
            }
        }

        // Generate a report of all books
        public void GenerateBookReport()
        {
            Console.WriteLine("Book Report:");
            foreach (var book in books)
            {
                Console.WriteLine($"Book: {book.Title}, Author: {book.Author}, Genre: {book.Genre}, Year: {book.PublicationYear}, Loaned: {book.IsLoaned}");
                if (book.Reservations.Any())
                {
                    Console.WriteLine("Reservations:");
                    foreach (var reservation in book.Reservations)
                    {
                        var patron = patrons.FirstOrDefault(p => p.Id == reservation.PatronId);
                        Console.WriteLine($"Reserved by: {patron.Name}, Date: {reservation.ReservationDate}");
                    }
                }
            }
        }

        // Process fine payments
        public void ProcessFinePayments()
        {
            var overdueLoans = GetOverdueLoans();
            foreach (var loan in overdueLoans)
            {
                var patron = patrons.FirstOrDefault(p => p.Id == loan.PatronId);
                if (patron != null)
                {
                    var fine = loan.CalculateFine();
                    patron.AddFine(fine);
                    Console.WriteLine($"Patron {patron.Name} has been fined ${fine:0.00} for overdue book (Book ID: {loan.BookId}). Total fine balance: ${patron.FineBalance:0.00}");
                }
            }
        }

        // Get library statistics
        public void GetLibraryStatistics()
        {
            Console.WriteLine($"Total Books: {books.Count}");
            Console.WriteLine($"Total Patrons: {patrons.Count}");
            Console.WriteLine($"Total Loans: {loans.Count}");
            Console.WriteLine($"Total Reservations: {reservations.Count}");
        }

        // Helper method to send email notifications
        private void SendEmailNotification(string email, string subject, string message)
        {
            // Simulate sending an email
            Console.WriteLine($"Sending email to {email} - Subject: {subject}, Message: {message}");
        }
    }

    // Main Program class
    class Program
    {
        static void Main(string[] args)
        {
            var library = new LibraryManagementSystem();

            // Adding some books
            library.AddBook(new Book(1, "1984", "George Orwell", "Dystopian", 1949));
            library.AddBook(new Book(2, "To Kill a Mockingbird", "Harper Lee", "Fiction", 1960));

            // Registering some patrons
            var alice = library.RegisterPatron("Alice", "alice@example.com", "Regular");
            var bob = library.RegisterPatron("Bob", "bob@example.com", "Premium");

            // Loaning a book
            library.LoanBook(1, alice.Id);

            // Returning a book
            library.ReturnBook(1);

            // Reserving a book
            library.ReserveBook(2, bob.Id);

            // Extending a loan period
            library.ExtendLoan(1, alice.Id);

            // Searching for books by title
            var booksByTitle = library.SearchBooksByTitle("1984");
            foreach (var book in booksByTitle)
            {
                Console.WriteLine($"Book: {book.Title}, Author: {book.Author}");
            }

            // Searching for books by author
            var booksByAuthor = library.SearchBooksByAuthor("Harper Lee");
            foreach (var book in booksByAuthor)
            {
                Console.WriteLine($"Book: {book.Title}, Author: {book.Author}");
            }

            // Searching for reservations by patron name
            var reservationsByPatronName = library.SearchReservationsByPatronName("Bob");
            foreach (var reservation in reservationsByPatronName)
            {
                Console.WriteLine($"Reservation: BookId={reservation.BookId}, PatronId={reservation.PatronId}, ReservationDate={reservation.ReservationDate}");
            }

            // Getting library statistics
            library.GetLibraryStatistics();

            // Generating book report
            library.GenerateBookReport();

            // Processing fine payments
            library.ProcessFinePayments();
        }
    }
}
