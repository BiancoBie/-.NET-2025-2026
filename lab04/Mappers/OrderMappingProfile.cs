using AutoMapper;
using OrderManagementAPI.DTOs;
using OrderManagementAPI.Models;

namespace OrderManagementAPI.Mappers
{
    //advanced AutoMapper profile for Order mappings with custom resolvers and conditional logic
    public class OrderMappingProfile : Profile
    {
        public OrderMappingProfile()
        {
            //createOrderProfileRequest -> Order mapping
            CreateMap<CreateOrderProfileRequest, Order>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                
                // conditional CoverImageUrl mapping - null for Children category
                .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src =>
                    src.Category == OrderCategory.Children ? null : src.CoverImageUrl))
                
                // conditional Price mapping - 10% discount for Children category
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src =>
                    src.Category == OrderCategory.Children ? src.Price * 0.9m : src.Price));

            // order -> OrderProfileDto mapping with custom resolvers
            CreateMap<Order, OrderProfileDto>()
                .ForMember(dest => dest.CategoryDisplayName, opt => opt.MapFrom<CategoryDisplayResolver>())
                .ForMember(dest => dest.FormattedPrice, opt => opt.MapFrom<PriceFormatterResolver>())
                .ForMember(dest => dest.PublishedAge, opt => opt.MapFrom<PublishedAgeResolver>())
                .ForMember(dest => dest.AuthorInitials, opt => opt.MapFrom<AuthorInitialsResolver>())
                .ForMember(dest => dest.AvailabilityStatus, opt => opt.MapFrom<AvailabilityStatusResolver>());
        }
    }

    //CUSTOM VALUE RESOLVERS

    //resolver for category display names
    public class CategoryDisplayResolver : IValueResolver<Order, OrderProfileDto, string>
    {
        public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
        {
            return source.Category switch
            {
                OrderCategory.Fiction => "Fiction & Literature",
                OrderCategory.NonFiction => "Non-Fiction",
                OrderCategory.Technical => "Technical & Professional",
                OrderCategory.Children => "Children's Orders",
                _ => "Uncategorized"
            };
        }
    }

    //resolver for formatting price as currency
    public class PriceFormatterResolver : IValueResolver<Order, OrderProfileDto, string>
    {
        public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
        {
            return source.Price.ToString("C2");
        }
    }

    //resolver for human-readable published age
    public class PublishedAgeResolver : IValueResolver<Order, OrderProfileDto, string>
    {
        public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
        {
            int daysSincePublished = (DateTime.UtcNow - source.PublishedDate).Days;
            
            if (daysSincePublished < 30)
            {
                return "New Release";
            }
            else if (daysSincePublished < 365)
            {
                int months = daysSincePublished / 30;
                return $"{months} months old";
            }
            else if (daysSincePublished < 1825) // 5 years
            {
                int years = daysSincePublished / 365;
                return $"{years} years old";
            }
            else
            {
                return "Classic";
            }
        }
    }

    //resolver for author initials
    public class AuthorInitialsResolver : IValueResolver<Order, OrderProfileDto, string>
    {
        public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
        {
            if (string.IsNullOrWhiteSpace(source.Author)) 
                return "?";
                
            string[] parts = source.Author.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length == 1)
            {
                return parts[0][0].ToString().ToUpper();
            }
            else if (parts.Length >= 2)
            {
                return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
            }
            
            return "?";
        }
    }

    //resolver for availability status based on stock
    public class AvailabilityStatusResolver : IValueResolver<Order, OrderProfileDto, string>
    {
        public string Resolve(Order source, OrderProfileDto destination, string destMember, ResolutionContext context)
        {
            if (!source.IsAvailable)
            {
                return "Out of Stock";
            }
            else if (source.StockQuantity == 0)
            {
                return "Unavailable";  
            }
            else if (source.StockQuantity == 1)
            {
                return "Last Copy";
            }
            else if (source.StockQuantity <= 5)
            {
                return "Limited Stock";
            }
            else
            {
                return "In Stock";
            }
        }
    }
}