using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderManagementAPI.Data;
using OrderManagementAPI.DTOs;
using OrderManagementAPI.Extensions;
using OrderManagementAPI.Models;

namespace OrderManagementAPI.Validators
{
    //fluentValidation validator for CreateOrderProfileRequest
    public class CreateOrderProfileValidator : AbstractValidator<CreateOrderProfileRequest>
    {
        private readonly IApplicationContext _context;
        private readonly ILogger<CreateOrderProfileValidator> _logger;

        //inappropriate words for content filtering
        private readonly string[] _inappropriateWords = { "violence", "adult", "mature", "explicit" };
        
        //technical keywords for validation
        private readonly string[] _technicalKeywords = { "programming", "software", "computer", "technology", "technical", "engineering", "development", "code", "algorithm", "database" };

        public CreateOrderProfileValidator(IApplicationContext context, ILogger<CreateOrderProfileValidator> logger)
        {
            _context = context;
            _logger = logger;

            //title Validation Rules
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .Length(1, 200).WithMessage("Title must be between 1 and 200 characters")
                .Must(BeValidTitle).WithMessage("Title contains inappropriate content")
                .MustAsync(BeUniqueTitle).WithMessage("A book with this title by the same author already exists");

            //author Validation Rules
            RuleFor(x => x.Author)
                .NotEmpty().WithMessage("Author is required")
                .Length(2, 100).WithMessage("Author name must be between 2 and 100 characters")
                .Must(BeValidAuthorName).WithMessage("Author name contains invalid characters");

            //ISBN Validation Rules
            RuleFor(x => x.ISBN)
                .NotEmpty().WithMessage("ISBN is required")
                .Must(BeValidISBN).WithMessage("Invalid ISBN format")
                .MustAsync(BeUniqueISBN).WithMessage("An order with this ISBN already exists");

            //category Validation Rules
            RuleFor(x => x.Category)
                .IsInEnum().WithMessage("Invalid category");

            //price Validation Rules
            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0")
                .LessThan(10000m).WithMessage("Price must be less than $10,000");

            //publishedDate Validation Rules
            RuleFor(x => x.PublishedDate)
                .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Published date cannot be in the future")
                .GreaterThanOrEqualTo(new DateTime(1400, 1, 1)).WithMessage("Published date cannot be before year 1400");

            //stockQuantity Validation Rules
            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative")
                .LessThanOrEqualTo(100000).WithMessage("Stock quantity cannot exceed 100,000");

            // coverImageUrl Validation Rules
            RuleFor(x => x.CoverImageUrl)
                .Must(BeValidImageUrl).When(x => !string.IsNullOrEmpty(x.CoverImageUrl))
                .WithMessage("Cover image URL must be a valid image URL");

            //business Rules Validation
            RuleFor(x => x)
                .MustAsync(PassBusinessRules).WithMessage("Order does not meet business requirements");

            //conditional Validation based on Category
            When(x => x.Category == OrderCategory.Technical, () =>
            {
                RuleFor(x => x.Price)
                    .GreaterThanOrEqualTo(20.00m).WithMessage("Technical orders must have a minimum price of $20.00");
                
                RuleFor(x => x.Title)
                    .Must(ContainTechnicalKeywords).WithMessage("Technical orders must contain technical keywords in the title");
                
                RuleFor(x => x.PublishedDate)
                    .GreaterThanOrEqualTo(DateTime.UtcNow.AddYears(-5)).WithMessage("Technical orders must be published within the last 5 years");
            });

            When(x => x.Category == OrderCategory.Children, () =>
            {
                RuleFor(x => x.Price)
                    .LessThanOrEqualTo(50.00m).WithMessage("Children's orders must have a maximum price of $50.00");
                
                RuleFor(x => x.Title)
                    .Must(BeAppropriateForChildren).WithMessage("Title is not appropriate for children");
            });

            When(x => x.Category == OrderCategory.Fiction, () =>
            {
                RuleFor(x => x.Author)
                    .MinimumLength(5).WithMessage("Fiction orders require a full author name (minimum 5 characters)");
            });

            //cross-field validation
            RuleFor(x => x)
                .Must(x => x.Price <= 100m || x.StockQuantity <= 20)
                .WithMessage("Expensive orders (>$100) must have limited stock (≤20 units)");
        }

        private bool BeValidTitle(string title)
        {
            return !_inappropriateWords.Any(word => title.ToLower().Contains(word.ToLower()));
        }

        private async Task<bool> BeUniqueTitle(CreateOrderProfileRequest request, string title, CancellationToken cancellationToken)
        {
            _logger.LogInformation(LogEvents.ISBNValidationPerformed, "Checking title uniqueness for: {Title} by {Author}", title, request.Author);
            
            var exists = await _context.Orders
                .AnyAsync(o => o.Title.ToLower() == title.ToLower() && o.Author.ToLower() == request.Author.ToLower(), cancellationToken);
            
            return !exists;
        }

        private bool BeValidAuthorName(string author)
        {
            var validAuthorPattern = @"^[a-zA-Z\s\-\.\']+$";
            return Regex.IsMatch(author, validAuthorPattern);
        }

        private bool BeValidISBN(string isbn)
        {
            var cleanIsbn = isbn.Replace("-", "").Replace(" ", "");
            return (cleanIsbn.Length == 10 || cleanIsbn.Length == 13) && cleanIsbn.All(char.IsDigit);
        }

        private async Task<bool> BeUniqueISBN(string isbn, CancellationToken cancellationToken)
        {
            _logger.LogInformation(LogEvents.ISBNValidationPerformed, "Checking ISBN uniqueness: {ISBN}", isbn);
            
            var exists = await _context.Orders.AnyAsync(o => o.ISBN == isbn, cancellationToken);
            return !exists;
        }

        private bool BeValidImageUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return true;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            if (uri.Scheme != "http" && uri.Scheme != "https")
                return false;

            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            return validExtensions.Any(ext => uri.AbsolutePath.ToLower().EndsWith(ext));
        }

        private async Task<bool> PassBusinessRules(CreateOrderProfileRequest request, CancellationToken cancellationToken)
        {
            //rule 1: Daily order addition limit (max 500 per day)
            var today = DateTime.UtcNow.Date;
            var dailyCount = await _context.Orders.CountAsync(o => o.CreatedAt.Date == today, cancellationToken);
            if (dailyCount >= 500)
            {
                _logger.LogWarning("Daily order limit reached: {Count}", dailyCount);
                return false;
            }

            //rule 2: Technical orders minimum price check ($20.00)
            if (request.Category == OrderCategory.Technical && request.Price < 20.00m)
            {
                _logger.LogWarning("Technical order below minimum price: {Price}", request.Price);
                return false;
            }

            //rule 3: Children's order content restrictions
            if (request.Category == OrderCategory.Children && !BeAppropriateForChildren(request.Title))
            {
                _logger.LogWarning("Children's order contains inappropriate content: {Title}", request.Title);
                return false;
            }

            //rule 4: High-value order stock limit (>$500 = max 10 stock)
            if (request.Price > 500m && request.StockQuantity > 10)
            {
                _logger.LogWarning("High-value order exceeds stock limit: Price={Price}, Stock={Stock}", request.Price, request.StockQuantity);
                return false;
            }

            return true;
        }

        private bool ContainTechnicalKeywords(string title)
        {
            return _technicalKeywords.Any(keyword => title.ToLower().Contains(keyword.ToLower()));
        }

        private bool BeAppropriateForChildren(string title)
        {
            var restrictedWords = new[] { "violence", "adult", "mature", "explicit", "horror", "scary" };
            return !restrictedWords.Any(word => title.ToLower().Contains(word.ToLower()));
        }
    }
}