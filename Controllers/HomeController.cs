using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AuraLiving.Models;
using AuraLiving.Services;

namespace AuraLiving.Controllers;

public class HomeController : Controller
{
    private readonly IMockDataService _dataService;

    public HomeController(IMockDataService dataService)
    {
        _dataService = dataService;
    }

    public IActionResult Index()
    {
        var model = new HomeViewModel
        {
            Categories = _dataService.GetCategories(),
            FeaturedProducts = _dataService.GetFeaturedProducts(),
            BestSellers = _dataService.GetBestSellers(),
            FlashDeals = _dataService.GetFlashDeals(),
            HeroProduct = _dataService.GetAllProducts().First(),
            Brands = _dataService.GetAllBrands(),
            Testimonials = new List<ReviewViewModel>
            {
                new ReviewViewModel
                {
                    Author = "Rajesh Sharma",
                    City = "South Delhi",
                    Rating = 5,
                    Date = "August 2026",
                    Title = "White-glove delivery & unboxing is flawless",
                    Comment = "Purchased the Bosch Serie 8 washer and Samsung French Door fridge together. The Aura installation team arrived on time, completed the civil connections cleanly, and briefed our family on operation."
                },
                new ReviewViewModel
                {
                    Author = "Dr. Ananya Roy",
                    City = "Kolkata",
                    Rating = 5,
                    Date = "July 2026",
                    Title = "A breath of fresh air in appliance shopping",
                    Comment = "The curated selection beats walking through chaotic electronics mega-marts. Genuine brand warranties and no hassle checkout."
                },
                new ReviewViewModel
                {
                    Author = "Chetan Parekh",
                    City = "Ahmedabad",
                    Rating = 5,
                    Date = "September 2026",
                    Title = "Best price with instant bank cashbacks",
                    Comment = "Saved ₹12,000 on my Sony BRAVIA OLED TV with their festive card offers. Delivered within 24 hours in pristine condition."
                }
            }
        };

        return View(model);
    }

    public IActionResult About() => View();
    public IActionResult Contact() => View();
    public IActionResult Help() => View();
    public IActionResult Faq() => View();
    public IActionResult Blog() => View();
    public IActionResult Privacy() => View();
    public IActionResult Terms() => View();
    public IActionResult ShippingPolicy() => View();
    public IActionResult ReturnPolicy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
