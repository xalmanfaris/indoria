using Microsoft.AspNetCore.Mvc;
using AuraLiving.Services;

namespace AuraLiving.Controllers;

public class AccountController : Controller
{
    private readonly IMockDataService _dataService;

    public AccountController(IMockDataService dataService)
    {
        _dataService = dataService;
    }

    public IActionResult Login() => View();

    public IActionResult Register() => View();

    public IActionResult Index()
    {
        var profile = _dataService.GetSampleProfile();
        var recentOrders = _dataService.GetSampleOrders().Take(2).ToList();
        var addresses = _dataService.GetSampleAddresses();

        ViewBag.RecentOrders = recentOrders;
        ViewBag.Addresses = addresses;
        return View(profile);
    }

    public IActionResult Profile()
    {
        var profile = _dataService.GetSampleProfile();
        return View(profile);
    }

    public IActionResult Addresses()
    {
        var addresses = _dataService.GetSampleAddresses();
        return View(addresses);
    }

    public IActionResult Orders()
    {
        var orders = _dataService.GetSampleOrders();
        return View(orders);
    }

    public IActionResult OrderDetails(string id)
    {
        var order = _dataService.GetOrderById(id) ?? _dataService.GetSampleOrders().First();
        return View(order);
    }

    public IActionResult Returns()
    {
        var returns = _dataService.GetSampleReturns();
        return View(returns);
    }

    public IActionResult Reviews()
    {
        var reviews = _dataService.GetSampleReviews();
        return View(reviews);
    }

    public IActionResult Notifications()
    {
        var settings = _dataService.GetNotificationSettings();
        return View(settings);
    }

    public IActionResult Settings() => View();
}
