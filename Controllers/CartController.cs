using Microsoft.AspNetCore.Mvc;
using AuraLiving.Models;
using AuraLiving.Services;

namespace AuraLiving.Controllers;

public class CartController : Controller
{
    private readonly IMockDataService _dataService;

    public CartController(IMockDataService dataService)
    {
        _dataService = dataService;
    }

    public IActionResult Index()
    {
        var cart = _dataService.GetSampleCart();
        return View(cart);
    }

    public IActionResult Checkout([FromQuery] int step = 1)
    {
        var addresses = _dataService.GetSampleAddresses();
        var cart = _dataService.GetSampleCart();

        var model = new CheckoutViewModel
        {
            Address = addresses.First(),
            SavedAddresses = addresses,
            Cart = cart,
            Step = Math.Clamp(step, 1, 4)
        };

        return View(model);
    }

    public IActionResult OrderConfirmation(string id)
    {
        var order = _dataService.GetOrderById(id) ?? _dataService.GetSampleOrders().First();
        return View(order);
    }
}
