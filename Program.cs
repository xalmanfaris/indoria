using AuraLiving.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IMockDataService, MockDataService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders(new Microsoft.AspNetCore.HttpOverrides.ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Dedicated and intuitive SEO routes
app.MapControllerRoute(
    name: "category",
    pattern: "category/{slug}",
    defaults: new { controller = "Catalog", action = "Category" });

app.MapControllerRoute(
    name: "categories",
    pattern: "categories",
    defaults: new { controller = "Catalog", action = "Categories" });

app.MapControllerRoute(
    name: "productDetail",
    pattern: "product/{slug}",
    defaults: new { controller = "Catalog", action = "ProductDetail" });

app.MapControllerRoute(
    name: "products",
    pattern: "products",
    defaults: new { controller = "Catalog", action = "Products" });

app.MapControllerRoute(
    name: "search",
    pattern: "search",
    defaults: new { controller = "Catalog", action = "Search" });

app.MapControllerRoute(
    name: "cart",
    pattern: "cart",
    defaults: new { controller = "Cart", action = "Index" });

app.MapControllerRoute(
    name: "checkout",
    pattern: "checkout",
    defaults: new { controller = "Cart", action = "Checkout" });

app.MapControllerRoute(
    name: "orderConfirmation",
    pattern: "order-confirmation/{id}",
    defaults: new { controller = "Cart", action = "OrderConfirmation" });

app.MapControllerRoute(
    name: "wishlist",
    pattern: "wishlist",
    defaults: new { controller = "Wishlist", action = "Index" });

// Informational and policy routes
app.MapControllerRoute(
    name: "about",
    pattern: "about",
    defaults: new { controller = "Home", action = "About" });

app.MapControllerRoute(
    name: "contact",
    pattern: "contact",
    defaults: new { controller = "Home", action = "Contact" });

app.MapControllerRoute(
    name: "help",
    pattern: "help",
    defaults: new { controller = "Home", action = "Help" });

app.MapControllerRoute(
    name: "faq",
    pattern: "faq",
    defaults: new { controller = "Home", action = "Faq" });

app.MapControllerRoute(
    name: "blog",
    pattern: "blog",
    defaults: new { controller = "Home", action = "Blog" });

app.MapControllerRoute(
    name: "privacy",
    pattern: "privacy",
    defaults: new { controller = "Home", action = "Privacy" });

app.MapControllerRoute(
    name: "terms",
    pattern: "terms",
    defaults: new { controller = "Home", action = "Terms" });

app.MapControllerRoute(
    name: "shippingPolicy",
    pattern: "shipping-policy",
    defaults: new { controller = "Home", action = "ShippingPolicy" });

app.MapControllerRoute(
    name: "returnPolicy",
    pattern: "return-policy",
    defaults: new { controller = "Home", action = "ReturnPolicy" });

// Auth routes
app.MapControllerRoute(
    name: "login",
    pattern: "login",
    defaults: new { controller = "Account", action = "Login" });

app.MapControllerRoute(
    name: "register",
    pattern: "register",
    defaults: new { controller = "Account", action = "Register" });

// Account routes
app.MapControllerRoute(
    name: "accountOrderDetails",
    pattern: "account/orders/{id}",
    defaults: new { controller = "Account", action = "OrderDetails" });

// Default conventional route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
