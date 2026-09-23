using AuraLiving.Services;
using AuraLiving.Services.Database;
using AuraLiving.Services.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register Dapper Database Connection Factory & Initializer
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddSingleton<DatabaseInitializer>();

// Register HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Register Repositories
builder.Services.AddSingleton<IReviewRepository, ReviewRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IProductRepository, ProductRepository>();
builder.Services.AddSingleton<IAddressRepository, AddressRepository>();
builder.Services.AddSingleton<IWishlistRepository, WishlistRepository>();
builder.Services.AddSingleton<ICartRepository, CartRepository>();
builder.Services.AddSingleton<IOrderRepository, OrderRepository>();
builder.Services.AddSingleton<IReturnRepository, ReturnRepository>();
builder.Services.AddSingleton<ISupportTicketRepository, SupportTicketRepository>();
builder.Services.AddSingleton<ICouponRepository, CouponRepository>();

// Register MemoryCache & HttpClient
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// Register Service Layer
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IMockDataService, MockDataService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IProductService, ProductService>();
builder.Services.AddSingleton<IAdminService, AdminService>();
builder.Services.AddSingleton<ICurrencyService, CurrencyService>();
builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
builder.Services.AddSingleton<ICountryService, CountryService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddSingleton<IInstagramService, InstagramService>();

// Register Instagram Background Auto-Refresh Hosted Service
builder.Services.AddHostedService<InstagramRefreshHostedService>();

// Role-Based Cookie & JWT Authentication Setup
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Cookie.Name = "IndoriaAuthCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

var app = builder.Build();

// Run Database Initializer (Creates Dapper SQL Tables if connected)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        dbInitializer.InitializeDatabase();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DB Initializer notice: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

// JWT Token Cookie Reader Middleware (Validates HttpOnly JWT Cookie before Auth pipeline)
app.Use(async (context, next) =>
{
    if (context.Request.Cookies.TryGetValue("IndoriaJwtToken", out var jwtToken) && !string.IsNullOrEmpty(jwtToken))
    {
        var jwtService = context.RequestServices.GetRequiredService<IJwtTokenService>();
        var principal = jwtService.ValidateToken(jwtToken);
        if (principal != null)
        {
            context.User = principal;
        }
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// Map Attribute-Routed Controllers (AdminController, ApiControllers)
app.MapControllers();

// Dedicated SEO & Application Routes
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

app.MapControllerRoute(
    name: "login",
    pattern: "login",
    defaults: new { controller = "Account", action = "Login" });

app.MapControllerRoute(
    name: "register",
    pattern: "register",
    defaults: new { controller = "Account", action = "Register" });

app.MapControllerRoute(
    name: "logout",
    pattern: "logout",
    defaults: new { controller = "Account", action = "Logout" });

app.MapControllerRoute(
    name: "accountOrderDetails",
    pattern: "account/orders/{id}",
    defaults: new { controller = "Account", action = "OrderDetails" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
