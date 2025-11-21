using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using OrderManagementAPI.Models;

namespace OrderManagementAPI.Validators
{
    //custom validation attribute for ISBN format
    public class ValidISBNAttribute : ValidationAttribute, IClientModelValidator
    {
        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return false;

            string isbn = value.ToString()!.Replace("-", "").Replace(" ", "");
            
            return (isbn.Length == 10 || isbn.Length == 13) && isbn.All(char.IsDigit);
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            context.Attributes.Add("data-val", "true");
            context.Attributes.Add("data-val-isbn", ErrorMessage ?? "Invalid ISBN format");
        }
    }

    //custom validation attribute for order category
    public class OrderCategoryAttribute : ValidationAttribute
    {
        private readonly OrderCategory[] _allowedCategories;

        public OrderCategoryAttribute(params OrderCategory[] allowedCategories)
        {
            _allowedCategories = allowedCategories;
        }

        public override bool IsValid(object? value)
        {
            if (value is OrderCategory category)
            {
                return _allowedCategories.Contains(category);
            }
            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The field {name} must be one of: {string.Join(", ", _allowedCategories)}";
        }
    }

    //custom validation attribute for price range
    public class PriceRangeAttribute : ValidationAttribute
    {
        private readonly decimal _min;
        private readonly decimal _max;

        public PriceRangeAttribute(double min, double max)
        {
            _min = (decimal)min;
            _max = (decimal)max;
        }

        public override bool IsValid(object? value)
        {
            if (value is decimal price)
            {
                return price >= _min && price <= _max;
            }
            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The field {name} must be between {_min:C2} and {_max:C2}";
        }
    }
}