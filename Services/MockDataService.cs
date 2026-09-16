using AuraLiving.Models;

namespace AuraLiving.Services;

public class MockDataService : IMockDataService
{
    private readonly List<CategoryViewModel> _categories;
    private readonly List<ProductViewModel> _products;
    private readonly List<AddressViewModel> _addresses;
    private readonly List<OrderViewModel> _orders;
    private readonly List<ReturnRequestViewModel> _returns;
    private readonly List<ReviewItemViewModel> _accountReviews;

    public MockDataService()
    {
        _categories = InitializeCategories();
        _products = InitializeProducts();
        _addresses = InitializeAddresses();
        _orders = InitializeOrders();
        _returns = InitializeReturns();
        _accountReviews = InitializeAccountReviews();
    }

    public List<CategoryViewModel> GetCategories() => _categories;

    public CategoryViewModel? GetCategoryBySlug(string slug) =>
        _categories.FirstOrDefault(c => c.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

    public List<ProductViewModel> GetAllProducts() => _products;

    public ProductViewModel? GetProductBySlug(string slug) =>
        _products.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

    public List<ProductViewModel> GetFeaturedProducts() =>
        _products.Where(p => p.Badges.Contains("Featured") || p.Badges.Contains("New Launch")).Take(8).ToList();

    public List<ProductViewModel> GetBestSellers() =>
        _products.Where(p => p.Badges.Contains("Best Seller")).Take(6).ToList();

    public List<ProductViewModel> GetFlashDeals() =>
        _products.Where(p => p.DiscountPercent >= 20).Take(4).ToList();

    public List<ProductViewModel> GetRelatedProducts(string categorySlug, string excludeProductId) =>
        _products.Where(p => p.CategorySlug.Equals(categorySlug, StringComparison.OrdinalIgnoreCase) && p.Id != excludeProductId)
                 .Take(4)
                 .ToList();

    public List<string> GetAllBrands() =>
        _products.Select(p => p.Brand).Distinct().OrderBy(b => b).ToList();

    public CatalogFilterViewModel FilterProducts(
        string? categorySlug,
        string? query,
        List<string>? brands,
        decimal? minPrice,
        decimal? maxPrice,
        double? minRating,
        int? energyRating,
        bool inStockOnly,
        string sortBy,
        int page,
        int pageSize)
    {
        var items = _products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            items = items.Where(p => p.CategorySlug.Equals(categorySlug, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.ToLower();
            items = items.Where(p => p.Name.ToLower().Contains(q) ||
                                     p.Brand.ToLower().Contains(q) ||
                                     p.Category.ToLower().Contains(q) ||
                                     p.ShortDescription.ToLower().Contains(q));
        }

        if (brands != null && brands.Any())
        {
            items = items.Where(p => brands.Contains(p.Brand, StringComparer.OrdinalIgnoreCase));
        }

        if (minPrice.HasValue)
        {
            items = items.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            items = items.Where(p => p.Price <= maxPrice.Value);
        }

        if (minRating.HasValue)
        {
            items = items.Where(p => p.Rating >= minRating.Value);
        }

        if (energyRating.HasValue)
        {
            items = items.Where(p => p.EnergyRating >= energyRating.Value);
        }

        if (inStockOnly)
        {
            items = items.Where(p => p.InStock);
        }

        items = sortBy switch
        {
            "price-low" => items.OrderBy(p => p.Price),
            "price-high" => items.OrderByDescending(p => p.Price),
            "rating" => items.OrderByDescending(p => p.Rating),
            "newest" => items.OrderByDescending(p => p.Badges.Contains("New Launch")),
            _ => items.OrderByDescending(p => p.Rating).ThenByDescending(p => p.ReviewCount)
        };

        var totalCount = items.Count();
        var paginated = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var catObj = !string.IsNullOrWhiteSpace(categorySlug) ? GetCategoryBySlug(categorySlug) : null;

        return new CatalogFilterViewModel
        {
            CategorySlug = categorySlug,
            CategoryName = catObj?.Name,
            SearchQuery = query,
            SelectedBrands = brands ?? new List<string>(),
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            MinRating = minRating,
            EnergyRating = energyRating,
            InStockOnly = inStockOnly,
            SortBy = sortBy,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            AvailableBrands = GetAllBrands(),
            Products = paginated
        };
    }

    public CartViewModel GetSampleCart()
    {
        var p1 = _products[0]; // Samsung Bespoke Fridge
        var p2 = _products[2]; // Bosch Serie 8 Washer
        var p3 = _products[9]; // Philips Airfryer

        return new CartViewModel
        {
            Items = new List<CartItemViewModel>
            {
                new CartItemViewModel
                {
                    ProductId = p1.Id,
                    ProductName = p1.Name,
                    ProductSlug = p1.Slug,
                    Brand = p1.Brand,
                    Image = p1.MainImage,
                    Price = p1.Price,
                    OriginalPrice = p1.OriginalPrice,
                    SelectedCapacity = "670 Litres",
                    SelectedColor = "Clean Navy & White Glass",
                    Quantity = 1
                },
                new CartItemViewModel
                {
                    ProductId = p2.Id,
                    ProductName = p2.Name,
                    ProductSlug = p2.Slug,
                    Brand = p2.Brand,
                    Image = p2.MainImage,
                    Price = p2.Price,
                    OriginalPrice = p2.OriginalPrice,
                    SelectedCapacity = "9.0 kg",
                    SelectedColor = "Dark Granite Silver",
                    Quantity = 1
                },
                new CartItemViewModel
                {
                    ProductId = p3.Id,
                    ProductName = p3.Name,
                    ProductSlug = p3.Slug,
                    Brand = p3.Brand,
                    Image = p3.MainImage,
                    Price = p3.Price,
                    OriginalPrice = p3.OriginalPrice,
                    SelectedCapacity = "7.2 Litres (XXL)",
                    SelectedColor = "Deep Black & Copper",
                    Quantity = 1
                }
            },
            Discount = 5000,
            CouponCode = "LUXE5000"
        };
    }

    public List<AddressViewModel> GetSampleAddresses() => _addresses;

    public List<OrderViewModel> GetSampleOrders() => _orders;

    public OrderViewModel? GetOrderById(string id) =>
        _orders.FirstOrDefault(o => o.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public ProfileViewModel GetSampleProfile() => new ProfileViewModel();

    public List<ReturnRequestViewModel> GetSampleReturns() => _returns;

    public List<ReviewItemViewModel> GetSampleReviews() => _accountReviews;

    public NotificationSettingsViewModel GetNotificationSettings() => new NotificationSettingsViewModel();

    private List<CategoryViewModel> InitializeCategories()
    {
        return new List<CategoryViewModel>
        {
            new CategoryViewModel
            {
                Id = "cat-refrigerators",
                Name = "Refrigerators",
                Slug = "refrigerators",
                Description = "French-door, side-by-side, and multi-door cooling marvels with digital inverter engineering.",
                Icon = "bi-snow",
                Image = "https://images.unsplash.com/photo-1584992236310-6edddc08acff?auto=format&fit=crop&w=800&q=80",
                ProductCount = 28,
                SubCategories = new List<string> { "French Door", "Side-by-Side", "Double Door Inverter", "Wine Chillers" },
                Featured = true
            },
            new CategoryViewModel
            {
                Id = "cat-washing-machines",
                Name = "Washing Machines",
                Slug = "washing-machines",
                Description = "AI-powered front-load and smart top-load washers engineered for silent, fabric-protecting care.",
                Icon = "bi-water",
                Image = "https://images.unsplash.com/photo-1626806787461-102c1bfaaea1?auto=format&fit=crop&w=800&q=80",
                ProductCount = 22,
                SubCategories = new List<string> { "Front Load AI", "Top Load Inverter", "Washer Dryers Combo", "Steam Wash" },
                Featured = true
            },
            new CategoryViewModel
            {
                Id = "cat-air-conditioners",
                Name = "Air Conditioners",
                Slug = "air-conditioners",
                Description = "5-Star heavy duty dual-inverter split ACs with PM 2.5 air filtration and WiFi control.",
                Icon = "bi-wind",
                Image = "https://images.unsplash.com/photo-1621905251189-08b45d6a269e?auto=format&fit=crop&w=800&q=80",
                ProductCount = 25,
                SubCategories = new List<string> { "1.5 Ton 5-Star Split", "2.0 Ton Heavy Duty", "Hot & Cold Inverter", "Cassette ACs" },
                Featured = true
            },
            new CategoryViewModel
            {
                Id = "cat-smart-tvs",
                Name = "Smart Televisions",
                Slug = "smart-tvs",
                Description = "Immersive 4K OLED, Mini-LED and QLED cinematic screens with Dolby Atmos acoustic audio.",
                Icon = "bi-tv",
                Image = "https://images.unsplash.com/photo-1593784991095-a205069470b6?auto=format&fit=crop&w=800&q=80",
                ProductCount = 34,
                SubCategories = new List<string> { "OLED Cinema 65\"", "Neo QLED 4K", "Mini-LED Google TV", "Soundbars & Audio" },
                Featured = true
            },
            new CategoryViewModel
            {
                Id = "cat-kitchen-appliances",
                Name = "Kitchen & Cooking",
                Slug = "kitchen-appliances",
                Description = "Precision digital airfryers, induction cooktops, auto-clean chimneys, and silent stand mixers.",
                Icon = "bi-fire",
                Image = "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=800&q=80",
                ProductCount = 36,
                SubCategories = new List<string> { "Digital Air Fryers", "Auto-Clean Chimneys", "Induction Hobs", "Food Processors" },
                Featured = true
            },
            new CategoryViewModel
            {
                Id = "cat-dishwashers",
                Name = "Dishwashers",
                Slug = "dishwashers",
                Description = "Intensive Kadhai wash programs with hygienic Zeolith drying technology and silent 42dB motors.",
                Icon = "bi-droplet-half",
                Image = "https://images.unsplash.com/photo-1585659722983-3a675dabf23d?auto=format&fit=crop&w=800&q=80",
                ProductCount = 16,
                SubCategories = new List<string> { "14 Place Settings", "16 Place Built-in", "Cutlery Drawer Serie", "Zeolith Drying" },
                Featured = false
            },
            new CategoryViewModel
            {
                Id = "cat-vacuum-air",
                Name = "Vacuum & Air Purifiers",
                Slug = "vacuum-air",
                Description = "Laser-guided cordless vacuums and HEPA H13 medical-grade whole-room air purifiers.",
                Icon = "bi-fan",
                Image = "https://images.unsplash.com/photo-1558317374-067fb5f30001?auto=format&fit=crop&w=800&q=80",
                ProductCount = 18,
                SubCategories = new List<string> { "Cordless Stick Vacuums", "Robot Mop & Cleaners", "Formaldehyde Purifiers", "Dehumidifiers" },
                Featured = false
            },
            new CategoryViewModel
            {
                Id = "cat-microwaves-ovens",
                Name = "Microwaves & Ovens",
                Slug = "microwaves-ovens",
                Description = "Built-in convection ovens, charcoal rotisseries, and sensor-cooking microwave systems.",
                Icon = "bi-box-seam",
                Image = "https://images.unsplash.com/photo-1585659722983-3a675dabf23d?auto=format&fit=crop&w=800&q=80",
                ProductCount = 20,
                SubCategories = new List<string> { "Built-in Convection 32L", "Charcoal Grill Microwave", "Baking & OTG 50L", "Solo Micro" },
                Featured = false
            }
        };
    }

    private List<ProductViewModel> InitializeProducts()
    {
        return new List<ProductViewModel>
        {
            new ProductViewModel
            {
                Id = "AURA-REF-001",
                Name = "Samsung 670 L Bespoke AI French Door Refrigerator",
                Slug = "samsung-bespoke-french-door-refrigerator",
                Brand = "Samsung",
                Category = "Refrigerators",
                CategorySlug = "refrigerators",
                Price = 149990,
                OriginalPrice = 194990,
                EmiPerMonth = 7249,
                Rating = 4.9,
                ReviewCount = 218,
                InStock = true,
                StockCount = 8,
                EnergyRating = 5,
                Badges = new List<string> { "Best Seller", "Bespoke Design", "Free Installation" },
                MainImage = "https://images.unsplash.com/photo-1584992236310-6edddc08acff?auto=format&fit=crop&w=1000&q=80",
                GalleryImages = new List<string>
                {
                    "https://images.unsplash.com/photo-1584992236310-6edddc08acff?auto=format&fit=crop&w=1000&q=80",
                    "https://images.unsplash.com/photo-1571175443880-49e1d25b2bc5?auto=format&fit=crop&w=1000&q=80",
                    "https://images.unsplash.com/photo-1584992236310-6edddc08acff?auto=format&fit=crop&w=1000&q=80"
                },
                ShortDescription = "Customizable glass panels, dual auto ice-maker, and AI energy mode reducing power consumption by up to 10%.",
                FullDescription = "Elevate your modern kitchen with the Samsung Bespoke 670 L AI French Door Refrigerator. Featuring customizable color glass finishes, Beverage Center with auto-fill water pitcher, and Dual Auto Ice Maker that prepares both cubed ice and Ice Bites. The Digital Inverter Compressor with 20-year warranty operates whisper-quietly while SmartThings AI Energy mode optimizes cooling cycles according to ambient temperature and usage habits.",
                KeyFeatures = new List<string>
                {
                    "Beverage Center™ with AutoFill Pitcher & Infuser",
                    "Dual Auto Ice Maker: Cubed Ice & Ice Bites",
                    "SmartThings AI Energy Mode with 20-Year Compressor Warranty",
                    "Triple Cooling System with independent temperature zones",
                    "UV Deodorizing Filter ensures pure, odor-free air circulation"
                },
                Specifications = new Dictionary<string, string>
                {
                    { "Capacity", "670 Litres" },
                    { "Defrosting Type", "Frost Free Triple Cooling" },
                    { "Compressor", "Digital Inverter with 20-Yr Warranty" },
                    { "Energy Star Rating", "5 Star (BEE Certified)" },
                    { "Annual Electricity", "284 kWh / Year" },
                    { "Dimensions (W x H x D)", "912 x 1825 x 723 mm" },
                    { "Finish", "Bespoke Clean Navy / Glam White Glass" },
                    { "Weight", "138 kg" }
                },
                DeliveryEstimate = "Tomorrow by 2:00 PM",
                WarrantyYears = 2,
                ColorVariants = new List<string> { "Clean Navy Glass", "Glam White Glass", "Matte Charcoal Black" },
                CapacityVariants = new List<string> { "596 L", "670 L", "865 L" },
                Reviews = new List<ReviewViewModel>
                {
                    new ReviewViewModel
                    {
                        Id = "REV-101",
                        Author = "Vikramaditya Sengupta",
                        City = "Bengaluru",
                        Rating = 5,
                        Date = "12 Aug 2026",
                        Title = "Spectacular architectural centerpiece for our kitchen",
                        Comment = "The glass panels are gorgeous, and the ice maker with ice bites is unmatched. Aura Living delivered and leveled it within 24 hours with certified Samsung technicians.",
                        VerifiedBuyer = true,
                        HelpfulCount = 42
                    },
                    new ReviewViewModel
                    {
                        Id = "REV-102",
                        Author = "Pooja Singhania",
                        City = "Mumbai",
                        Rating = 5,
                        Date = "28 Jul 2026",
                        Title = "Whisper quiet and keeps veggies fresh for 2+ weeks",
                        Comment = "Extremely happy with the Beverage center and cold water infuser. The SmartThings app integration also sends door-ajar alerts to my phone.",
                        VerifiedBuyer = true,
                        HelpfulCount = 29
                    }
                }
            },
            new ProductViewModel
            {
                Id = "AURA-REF-002",
                Name = "LG 655 L Frost Free Side-by-Side InstaView Refrigerator",
                Slug = "lg-instaview-door-in-door-refrigerator",
                Brand = "LG",
                Category = "Refrigerators",
                CategorySlug = "refrigerators",
                Price = 118990,
                OriginalPrice = 159990,
                EmiPerMonth = 5749,
                Rating = 4.8,
                ReviewCount = 174,
                InStock = true,
                StockCount = 12,
                EnergyRating = 5,
                Badges = new List<string> { "InstaView Glass", "Featured" },
                MainImage = "https://images.unsplash.com/photo-1571175443880-49e1d25b2bc5?auto=format&fit=crop&w=1000&q=80",
                GalleryImages = new List<string>
                {
                    "https://images.unsplash.com/photo-1571175443880-49e1d25b2bc5?auto=format&fit=crop&w=1000&q=80",
                    "https://images.unsplash.com/photo-1584992236310-6edddc08acff?auto=format&fit=crop&w=1000&q=80"
                },
                ShortDescription = "Knock twice on the mirrored glass panel to illuminate the interior without opening the door and losing cold air.",
                FullDescription = "LG's InstaView Door-in-Door refrigerator lets you view groceries without opening the door, preserving up to 41% more cold air. Featuring DoorCooling+ for 19% faster cooling and Hygiene Fresh+ 5-step air purification filter to eliminate 99.999% of bacteria and pungent food odors.",
                KeyFeatures = new List<string>
                {
                    "InstaView™ Mirror Glass: Knock twice to see inside",
                    "Linear Cooling™ reduces temperature fluctuations to ±0.5°C",
                    "Hygiene Fresh+™ 5-stage anti-bacterial air filtration",
                    "LG ThinQ™ with Smart Diagnosis and remote WiFi control",
                    "Non-plumbed water and crushed ice dispenser"
                },
                Specifications = new Dictionary<string, string>
                {
                    { "Capacity", "655 Litres" },
                    { "Compressor", "Inverter Linear Compressor" },
                    { "Dispenser", "UVnano Water & Ice Dispenser" },
                    { "Finish", "Matte Black Stainless Steel" },
                    { "Noise Level", "36 dB" }
                },
                DeliveryEstimate = "Thursday by 5:00 PM",
                WarrantyYears = 2,
                ColorVariants = new List<string> { "Matte Black", "Platinum Silver" },
                CapacityVariants = new List<string> { "655 L", "688 L" }
            },
            new ProductViewModel
            {
                Id = "AURA-WSH-001",
                Name = "Bosch Serie 8 9 kg 1400 RPM Fully Automatic Front Load",
                Slug = "bosch-serie-8-front-load-washing-machine",
                Brand = "Bosch",
                Category = "Washing Machines",
                CategorySlug = "washing-machines",
                Price = 54990,
                OriginalPrice = 69990,
                EmiPerMonth = 2658,
                Rating = 4.9,
                ReviewCount = 340,
                InStock = true,
                StockCount = 14,
                EnergyRating = 5,
                Badges = new List<string> { "Best Seller", "German Engineered", "AntiStain" },
                MainImage = "https://images.unsplash.com/photo-1626806787461-102c1bfaaea1?auto=format&fit=crop&w=1000&q=80",
                GalleryImages = new List<string>
                {
                    "https://images.unsplash.com/photo-1626806787461-102c1bfaaea1?auto=format&fit=crop&w=1000&q=80"
                },
                ShortDescription = "i-DOS automatic detergent dosing, AntiStain automated spot removal, and EcoSilence Drive motor with 12-yr warranty.",
                FullDescription = "Precision German engineering for delicate and heavy Indian laundry. Bosch Serie 8 automatically calculates the exact milliliter of liquid detergent and water needed per wash load with i-DOS intelligent sensors. AntiStain system easily removes 4 of the most stubborn stains without pre-treatment.",
                KeyFeatures = new List<string>
                {
                    "i-DOS™: Precision liquid detergent auto-dosing",
                    "EcoSilence Drive™ brushless friction-free magnetic motor",
                    "AntiVibration™ side wall design for supreme balance at 1400 RPM",
                    "SpeedPerfect™ cuts cycle duration by up to 65% with pristine results",
                    "AllergyPlus programme certified by ECARF for sensitive skin"
                },
                Specifications = new Dictionary<string, string>
                {
                    { "Capacity", "9.0 kg" },
                    { "Spin Speed", "1400 RPM" },
                    { "Motor", "EcoSilence Drive Frictionless Inverter" },
                    { "Water Protection", "AquaStop with 100% Leak Warranty" },
                    { "Energy Rating", "5 Star 2026 BEE Certified" }
                },
                DeliveryEstimate = "Tomorrow by 11:00 AM",
                WarrantyYears = 3,
                ColorVariants = new List<string> { "Dark Granite Silver", "Classic White" },
                CapacityVariants = new List<string> { "8.0 kg", "9.0 kg", "10.0 kg" },
                Reviews = new List<ReviewViewModel>
                {
                    new ReviewViewModel
                    {
                        Id = "REV-201",
                        Author = "Deepak Nambiar",
                        City = "Kochi",
                        Rating = 5,
                        Date = "05 Sep 2026",
                        Title = "Zero vibration even at 1400 spin speed",
                        Comment = "A glass filled with water placed on top doesn't even ripple during spin cycle. The i-DOS detergent compartment lasts for almost a month of daily washes.",
                        VerifiedBuyer = true,
                        HelpfulCount = 51
                    }
                }
            },
            new ProductViewModel
            {
                Id = "AURA-WSH-002",
                Name = "LG 8 kg AI Direct Drive 6 Motion Inverter Front Load",
                Slug = "lg-ai-direct-drive-front-load-8kg",
                Brand = "LG",
                Category = "Washing Machines",
                CategorySlug = "washing-machines",
                Price = 42990,
                OriginalPrice = 52990,
                EmiPerMonth = 2079,
                Rating = 4.7,
                ReviewCount = 289,
                InStock = true,
                StockCount = 19,
                EnergyRating = 5,
                Badges = new List<string> { "AI Direct Drive", "Steam Hygiene" },
                MainImage = "https://images.unsplash.com/photo-1582735689369-4fe89db7114c?auto=format&fit=crop&w=1000&q=80",
                GalleryImages = new List<string>
                {
                    "https://images.unsplash.com/photo-1582735689369-4fe89db7114c?auto=format&fit=crop&w=1000&q=80"
                },
                ShortDescription = "AI DD detects fabric weight and softness to select the optimal drum motion pattern from 20,000 wash algorithms.",
                FullDescription = "LG AI Direct Drive front load washer offers 18% greater fabric care. The integrated Steam+ cycle removes 99.9% of dust mites and allergens while smoothing out 30% of wrinkles. Seamless tempered glass door and hygienic stainless steel lifter.",
                KeyFeatures = new List<string>
                {
                    "AI DD™ detects fabric softness and auto-tunes 6 Motion patterns",
                    "Steam+™ reduces wrinkles and destroys allergens",
                    "TurboWash™ 59 for sparkling fresh clean clothes in 59 minutes",
                    "ThinQ™ Wi-Fi control: download cycles and voice assist"
                },
                Specifications = new Dictionary<string, string>
                {
                    { "Capacity", "8.0 kg" },
                    { "Spin Speed", "1200 RPM" },
                    { "Drum", "Stainless Steel with Embossed Texture" },
                    { "Energy Rating", "5 Star" }
                },
                DeliveryEstimate = "Tomorrow by 4:00 PM",
                WarrantyYears = 2,
                ColorVariants = new List<string> { "Middle Black", "Luxury Silver" },
                CapacityVariants = new List<string> { "7.0 kg", "8.0 kg", "9.0 kg" }
            },
            new ProductViewModel
            {
                Id = "AURA-AC-001",
                Name = "Daikin 1.5 Ton 5-Star Triple Display Inverter Split AC",
                Slug = "daikin-1-5-ton-5-star-inverter-split-ac",
                Brand = "Daikin",
                Category = "Air Conditioners",
                CategorySlug = "air-conditioners",
                Price = 45990,
                OriginalPrice = 58990,
                EmiPerMonth = 2224,
                Rating = 4.8,
                ReviewCount = 512,
                InStock = true,
                StockCount = 20,
                EnergyRating = 5,
                Badges = new List<string> { "Best Seller", "5★ Inverter", "54°C Extreme Cool" },
                MainImage = "https://images.unsplash.com/photo-1621905251189-08b45d6a269e?auto=format&fit=crop&w=1000&q=80",
                GalleryImages = new List<string>
                {
                    "https://images.unsplash.com/photo-1621905251189-08b45d6a269e?auto=format&fit=crop&w=1000&q=80"
                },
                ShortDescription = "Patented Swing Compressor, Dew Clean technology, PM 2.5 air filtration, and 3D airflow for uncompromised cooling at 54°C.",
                FullDescription = "Daikin's flagship 1.5 Ton 5-Star inverter air conditioner is built to withstand extreme Indian summers up to 54°C ambient temperatures. Features Triple Display showing set temperature, room temperature, and real-time power consumption percentage. Dew Clean auto-cleans the indoor heat exchanger with condensation water at the touch of a button.",
                KeyFeatures = new List<string>
                {
                    "Cools effortlessly even at extreme 54°C ambient weather",
                    "Patented Neo Swing Compressor for whisper operation and durability",
                    "Dew Clean Technology auto-washes evaporator fins",
                    "Built-in PM 2.5 filter ensures sterile indoor air",
                    "100% Grooved Copper Tubes with Anti-Corrosion Treatment"
                },
                Specifications = new Dictionary<string, string>
                {
                    { "Cooling Capacity", "5280 Watts (1.5 Ton)" },
                    { "ISEER Value", "5.2 (Top 5-Star efficiency)" },
                    { "Noise Level", "32 dB(A) Silent Mode" },
                    { "Refrigerant", "Eco-friendly R-32" },
                    { "Warranty", "10 Years on Compressor, 5 Years on PCB" }
                },
                DeliveryEstimate = "Tomorrow by 1:00 PM",
                WarrantyYears = 5,
                ColorVariants = new List<string> { "Pure Arctic White" },
                CapacityVariants = new List<string> { "1.0 Ton", "1.5 Ton", "2.0 Ton" }
            },
            new ProductViewModel
            {
                Id = "AURA-AC-002",
                Name = "Voltas 1.5 Ton 5-Star Adjustable Inverter Split AC",
                Slug = "voltas-1-5-ton-inverter-split-ac",
                Brand = "Voltas",
                Category = "Air Conditioners",
                CategorySlug = "air-conditioners",
                Price = 37990,
                OriginalPrice = 49990,
                EmiPerMonth = 1836,
                Rating = 4.6,
                ReviewCount = 388,
                InStock = true,
                StockCount = 25,
                EnergyRating = 5,
                Badges = new List<string> { "Value Pick", "Multi-Stage Cool" },
                MainImage = "https://images.unsplash.com/photo-1621905251189-08b45d6a269e?auto=format&fit=crop&w=1000&q=80",
                GalleryImages = new List<string>
                {
                    "https://images.unsplash.com/photo-1621905251189-08b45d6a269e?auto=format&fit=crop&w=1000&q=80"
                },
                ShortDescription = "4-in-1 Adjustable Mode switches capacity between 0.75 Ton to 1.5 Ton based on room occupancy, saving huge power.",
                FullDescription = "The Voltas Adjustable Inverter AC operates on multi-stage tonnages to suit every cooling need while keeping electricity bills minimal. 100% copper condenser with SuperDry mode dehumidifies muggy monsoon days instantly.",
                KeyFeatures = new List<string>
                {
                    "4-in-1 Adjustable Tonnage cooling modes",
                    "High Ambient Cooling up to 52°C",
                    "SuperDry mode for rapid monsoon moisture extraction",
                    "Stabilizer-free operation between 110V to 285V"
                },
                Specifications = new Dictionary<string, string>
                {
                    { "Tonnage", "1.5 Ton" },
                    { "Energy Rating", "5 Star" },
                    { "ISEER", "5.0" },
                    { "Condenser", "100% Copper" }
                },
                DeliveryEstimate = "Friday by 12:00 PM",
                WarrantyYears = 5,
                ColorVariants = new List<string> { "White" },
                CapacityVariants = new List<string> { "1.0 Ton", "1.5 Ton" }
            },
            new ProductViewModel
            {
                Id = "AURA-TV-001",
                Name = "Sony BRAVIA XR 65 Inch 4K HDR Google Smart OLED TV",
                Slug = "sony-bravia-xr-65-inch-4k-oled-tv",
                Brand = "Sony",
                Category = "Smart Televisions",
                CategorySlug = "smart-tvs",
                Price = 189990,
                OriginalPrice = 249990,
                EmiPerMonth = 9183,
                Rating = 4.9,
                ReviewCount = 142,
                InStock = true,
                StockCount = 7,
                EnergyRating = 5,
                Badges = new List<string> { "Master Series", "Pure Black OLED", "Free Wallmount" },
                MainImage = "https://images.unsplash.com/photo-1593784991095-a205069470b6?auto=format&fit=crop&w=1000&q=80",
                GalleryImages = new List<string>
                {
                    "https://images.unsplash.com/photo-1593784991095-a205069470b6?auto=format&fit=crop&w=1000&q=80",
                    "https://images.unsplash.com/photo-1574375927938-d5a98e8ffe85?auto=format&fit=crop&w=1000&q=80"
                },
                ShortDescription = "Cognitive Processor XR mimics human perception for infinite contrast, pure blacks, and Acoustic Surface Audio+ where sound comes from the screen.",
                FullDescription = "Experience true cinematic perfection with the Sony BRAVIA XR 65 Inch 4K OLED. Powered by the revolutionary Cognitive Processor XR, every frame is analyzed and cross-referenced just like how the human eye focuses. Acoustic Surface Audio+ turns the entire OLED panel into a 3.2 channel speaker, synchronizing dialogue directly with on-screen actors. HDMI 2.1 4K 120fps with Auto HDR Tone Mapping makes it the pinnacle choice for PlayStation 5 gaming.",
                KeyFeatures = new List<string>
                {
                    "Cognitive Processor XR™ creates depth, lifelike texture and infinite contrast",
                    "XR OLED Contrast Pro: Peak brightness with pure deep blacks",
                    "Acoustic Surface Audio+™: The screen vibrates to produce spatial sound",
                    "Perfect for PlayStation®5 with 4K/120fps, VRR & ALLM",
                    "Hands-free Google TV with Far-field Mic and Apple AirPlay 2"
                },
                Specifications = new Dictionary<string, string>
                {
                    { "Display Resolution", "4K Ultra HD (3840 x 2160)" },
                    { "Panel Type", "OLED Self-Illuminating Pixels" },
                    { "Refresh Rate", "Native 120 Hz" },
                    { "Audio Output", "60 Watts Acoustic Surface Audio+" },
                    { "Operating System", "Google TV (Android 14)" },
                    { "HDMI Ports", "4 (2x HDMI 2.1 with eARC)" },
                    { "Dimensions", "1448 x 836 x 53 mm" }
                },
                DeliveryEstimate = "Tomorrow by 3:00 PM",
                WarrantyYears = 3,
                ColorVariants = new List<string> { "Seamless Slate Titanium" },
                CapacityVariants = new List<string> { "55 Inch", "65 Inch", "77 Inch" }
            },
            new ProductViewModel
            {
                Id = "AURA-TV-002",
                Name = "Samsung 65 Inch Neo QLED 4K Neural Quantum Smart TV",
                Slug = "samsung-neo-qled-65-inch-4k-tv",
                Brand = "Samsung",
                Category = "Smart Televisions",
                CategorySlug = "smart-tvs",
                Price = 139990,
                OriginalPrice = 184990,
                EmiPerMonth = 6766,
                Rating = 4.8,
                ReviewCount = 98,
                InStock = true,
                StockCount = 10,
                EnergyRating = 5,
                Badges = new List<string> { "Quantum Mini LED", "Infinity One Design" },
                MainImage = "https://images.unsplash.com/photo-1574375927938-d5a98e8ffe85?auto=format&fit=crop&w=1000&q=80",
                GalleryImages = new List<string>
                {
                    "https://images.unsplash.com/photo-1574375927938-d5a98e8ffe85?auto=format&fit=crop&w=1000&q=80"
                },
                ShortDescription = "Quantum Matrix Technology controls tiny Quantum Mini LEDs for blazing peak brightness in sunlit living rooms with anti-glare coating.",
                FullDescription = "The Samsung Neo QLED 65 Inch brings stadium brilliance and theater immersion into your living room. Quantum Mini LEDs that are 1/40th the size of conventional LEDs provide ultra-fine light control without halo effects. 20 neural networks upscale every detail into crisp 4K.",
                KeyFeatures = new List<string>
                {
                    "Quantum Matrix Technology with Quantum Mini LEDs",
                    "Neural Quantum Processor 4K with 20 AI neural networks",
                    "Anti-Reflection Screen: Zero glare from room windows",
                    "Dolby Atmos® with top-channel speakers for multi-dimensional sound"
                },
                Specifications = new Dictionary<string, string>
                {
                    { "Resolution", "4K Ultra HD (3840 x 2160)" },
                    { "Screen Size", "65 Inches (163 cm)" },
                    { "HDR", "Neo Quantum HDR+" },
                    { "Sound", "70W 4.2.2 Channel Dolby Atmos" }
                },
                DeliveryEstimate = "Tomorrow by 6:00 PM",
                WarrantyYears = 3,
                ColorVariants = new List<string> { "Titan Black Ultra Slim" },
                CapacityVariants = new List<string> { "55 Inch", "65 Inch", "75 Inch", "85 Inch" }
            },
            new ProductViewModel
            {
                Id = "AURA-VAC-001",
                Name = "Dyson V15 Detect Absolute Extra Cordless Vacuum Cleaner",
                Slug = "dyson-v15-detect-cordless-vacuum",
                Brand = "Dyson",
                Category = "Vacuum & Air Purifiers",
                CategorySlug = "vacuum-air",
                Price = 62900,
                OriginalPrice = 74900,
                EmiPerMonth = 3041,
                Rating = 4.9,
                ReviewCount = 425,
                InStock = true,
                StockCount = 15,
                EnergyRating = 5,
                Badges = new List<string> { "Best Seller", "Laser Dust Reveal", "Cordless" },
                MainImage = "https://images.unsplash.com/photo-1558317374-067fb5f30001?auto=format&fit=crop&w=1000&q=80",
                GalleryImages = new List<string>
                {
                    "https://images.unsplash.com/photo-1558317374-067fb5f30001?auto=format&fit=crop&w=1000&q=80"
                },
                ShortDescription = "Piezo sensor counts microscopic dust particles, green laser reveals invisible dirt, with 240AW powerful suction.",
                FullDescription = "Dyson's most intelligent cordless vacuum. Engineered with an illuminated cleaner head that reveals hidden dust on hard floors at a 1.5° angle. A piezo acoustic sensor measures microscopic dust 15,000 times a second and automatically ramps up suction when heavy debris is encountered. LCD screen displays real-time scientific proof of deep clean.",
                KeyFeatures = new List<string>
                {
                    "Fluffy Optic™ cleaner head illuminates microscopic dirt",
                    "Acoustic Piezo Sensor auto-adjusts suction power dynamically",
                    "LCD Screen shows particle count graph and remaining runtime",
                    "Up to 60 minutes of fade-free click-in battery run time",
                    "Whole-machine HEPA filtration traps 99.99% of particles down to 0.1 microns"
                },
                Specifications = new Dictionary<string, string>
                {
                    { "Suction Power", "240 Air Watts" },
                    { "Bin Volume", "0.77 Litres" },
                    { "Run Time", "Up to 60 Minutes" },
                    { "Charge Time", "4.5 Hours" },
                    { "Weight", "3.0 kg" }
                },
                DeliveryEstimate = "Tomorrow by 12:00 PM",
                WarrantyYears = 2,
                ColorVariants = new List<string> { "Sprayed Yellow / Iron", "Prussian Blue / Rich Copper" },
                CapacityVariants = new List<string> { "Standard (0.77L)" }
            },
            new ProductViewModel
            {
                Id = "AURA-KIT-001",
                Name = "Philips XXL Smart Sensing Technology Digital Airfryer",
                Slug = "philips-airfryer-xxl-smart-sensing",
                Brand = "Philips",
                Category = "Kitchen & Cooking",
                CategorySlug = "kitchen-appliances",
                Price = 19990,
                OriginalPrice = 24990,
                EmiPerMonth = 966,
                Rating = 4.8,
                ReviewCount = 680,
                InStock = true,
                StockCount = 30,
                EnergyRating = 5,
                Badges = new List<string> { "Best Seller", "Smart Sensing", "Fat Removal" },
                MainImage = "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=1000&q=80",
                GalleryImages = new List<string>
                {
                    "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=1000&q=80"
                },
                ShortDescription = "Smart Chef programs adjust time and temperature automatically. Twin TurboStar technology extracts and captures excess fat.",
                FullDescription = "Cook family-sized healthy meals with up to 90% less fat. The Philips XXL Airfryer features patented Smart Sensing technology: simply choose the food type and the appliance automatically adjusts cooking temperature and duration for crispy, juicy results.",
                KeyFeatures = new List<string>
                {
                    "Smart Sensing Technology: Auto-sets ideal temperature & time",
                    "Twin TurboStar Fat Removal Technology captures excess fat",
                    "XXL family size fits a whole chicken or 1.4 kg of fries",
                    "Keep Warm mode keeps food at ideal serving temperature for 30 min"
                },
                Specifications = new Dictionary<string, string>
                {
                    { "Basket Capacity", "7.3 Litres (1.4 kg)" },
                    { "Power", "2225 Watts" },
                    { "Presets", "5 One-Touch Programs" },
                    { "Dishwasher Safe", "Yes, QuickClean basket & drawer" }
                },
                DeliveryEstimate = "Tomorrow by 2:00 PM",
                WarrantyYears = 2,
                ColorVariants = new List<string> { "Deep Black & Rose Gold", "White & Rose Gold" },
                CapacityVariants = new List<string> { "XXL 7.3L", "XL 4.1L" }
            },
            new ProductViewModel
            {
                Id = "AURA-DSH-001",
                Name = "Bosch Serie 6 14-Place Settings Freestanding Dishwasher",
                Slug = "bosch-14-place-freestanding-dishwasher",
                Brand = "Bosch",
                Category = "Dishwashers",
                CategorySlug = "dishwashers",
                Price = 49990,
                OriginalPrice = 64990,
                EmiPerMonth = 2416,
                Rating = 4.7,
                ReviewCount = 196,
                InStock = true,
                StockCount = 11,
                EnergyRating = 5,
                Badges = new List<string> { "Kadhai Intensive", "Zeolith Drying" },
                MainImage = "https://images.unsplash.com/photo-1585659722983-3a675dabf23d?auto=format&fit=crop&w=1000&q=80",
                GalleryImages = new List<string>
                {
                    "https://images.unsplash.com/photo-1585659722983-3a675dabf23d?auto=format&fit=crop&w=1000&q=80"
                },
                ShortDescription = "Customized Indian utensils Kadhai 70°C cycle, 3-stage Rackmatic height adjustment, and EcoSilence drive motor.",
                FullDescription = "Specifically tailored for Indian kitchens with stubborn oil, masala, and burnt grease. The Intensive Kadhai 70° program uses high water temperatures and concentrated spray jets to sterilize and wash oily pots and pressure cookers effortlessly with only 9.5 liters of water per cycle.",
                KeyFeatures = new List<string>
                {
                    "Intensive Kadhai 70°C program removes tough oil & burnt-on grease",
                    "SpeedMatic hydraulic system uses just 9.5 litres of water",
                    "Rackmatic: Height-adjustable upper basket in 3 stages",
                    "Home Connect: Remote monitoring and cycle customization"
                },
                Specifications = new Dictionary<string, string>
                {
                    { "Place Settings", "14 Place Settings" },
                    { "Water Consumption", "9.5 L per cycle" },
                    { "Noise Level", "44 dB" },
                    { "Finish", "Fingerprint-free Inox Stainless Steel" }
                },
                DeliveryEstimate = "Thursday by 4:00 PM",
                WarrantyYears = 2,
                ColorVariants = new List<string> { "Stainless Steel Inox" },
                CapacityVariants = new List<string> { "14 Place Settings", "16 Place Settings" }
            },
            new ProductViewModel
            {
                Id = "AURA-MIC-001",
                Name = "Bosch Serie 6 Built-in Convection Microwave Oven 32L",
                Slug = "bosch-built-in-convection-microwave-32l",
                Brand = "Bosch",
                Category = "Microwaves & Ovens",
                CategorySlug = "microwaves-ovens",
                Price = 38990,
                OriginalPrice = 49990,
                EmiPerMonth = 1884,
                Rating = 4.8,
                ReviewCount = 114,
                InStock = true,
                StockCount = 9,
                EnergyRating = 5,
                Badges = new List<string> { "Built-in Luxury", "AutoPilot 14" },
                MainImage = "https://images.unsplash.com/photo-1585659722983-3a675dabf23d?auto=format&fit=crop&w=1000&q=80",
                GalleryImages = new List<string>
                {
                    "https://images.unsplash.com/photo-1585659722983-3a675dabf23d?auto=format&fit=crop&w=1000&q=80"
                },
                ShortDescription = "Flush seamless stainless steel cabinet integration, quartz grill, and 14 pre-set automated Indian chef recipes.",
                FullDescription = "Bring professional culinary perfection into your modular kitchen. Combines the rapid heating speed of a microwave with the even roasting of hot air convection and quartz broiling. Features AutoPilot 14 for fool-proof cakes, tandoori marinades, and crispy snacks.",
                KeyFeatures = new List<string>
                {
                    "AutoPilot 14: 14 pre-programmed automatic recipes",
                    "Hotair Convection evenly distributes heat for uniform baking",
                    "Stainless Steel Interior with catalytic self-cleaning rear wall",
                    "Touch control with LED display and rotary selector"
                },
                Specifications = new Dictionary<string, string>
                {
                    { "Capacity", "32 Litres" },
                    { "Microwave Power", "1000 Watts" },
                    { "Grill Power", "1500 Watts" },
                    { "Dimensions (Cut-out)", "560 x 450 x 550 mm" }
                },
                DeliveryEstimate = "Friday by 3:00 PM",
                WarrantyYears = 2,
                ColorVariants = new List<string> { "Brushed Stainless Steel & Black" },
                CapacityVariants = new List<string> { "32 Litres", "45 Litres" }
            }
        };
    }

    private List<AddressViewModel> InitializeAddresses()
    {
        return new List<AddressViewModel>
        {
            new AddressViewModel
            {
                Id = "ADDR-01",
                FullName = "Arjun Mehta",
                Phone = "+91 98201 45890",
                Email = "arjun.mehta@example.com",
                AddressLine1 = "Penthouse 14B, Horizon Royale Towers",
                AddressLine2 = "Off Palm Beach Road, Sector 18",
                City = "Mumbai",
                State = "Maharashtra",
                Pincode = "400705",
                Landmark = "Near Palm Beach Galleria",
                Type = "Home",
                IsDefault = true
            },
            new AddressViewModel
            {
                Id = "ADDR-02",
                FullName = "Arjun Mehta",
                Phone = "+91 98201 45890",
                Email = "arjun.mehta@example.com",
                AddressLine1 = "Level 8, Nexus Cyber City, Tower C",
                AddressLine2 = "Outer Ring Road, Bellandur",
                City = "Bengaluru",
                State = "Karnataka",
                Pincode = "560103",
                Landmark = "Opposite Ecospace Tech Park",
                Type = "Work",
                IsDefault = false
            }
        };
    }

    private List<OrderViewModel> InitializeOrders()
    {
        var p1 = _products[0]; // Samsung Bespoke Fridge
        var p2 = _products[4]; // Daikin AC
        var p3 = _products[8]; // Dyson V15 Vacuum

        return new List<OrderViewModel>
        {
            new OrderViewModel
            {
                Id = "AURA-98214",
                OrderDate = DateTime.Now.AddDays(-1),
                Status = "In Transit",
                EstimatedDelivery = "Tomorrow, by 2:00 PM",
                SubTotal = 195980,
                Discount = 5000,
                GrandTotal = 190980,
                PaymentMethod = "UPI (Google Pay - arjun@oksbi)",
                ShippingAddress = _addresses[0],
                Items = new List<CartItemViewModel>
                {
                    new CartItemViewModel
                    {
                        ProductId = p1.Id,
                        ProductName = p1.Name,
                        ProductSlug = p1.Slug,
                        Brand = p1.Brand,
                        Image = p1.MainImage,
                        Price = p1.Price,
                        OriginalPrice = p1.OriginalPrice,
                        SelectedCapacity = "670 L",
                        SelectedColor = "Clean Navy Glass",
                        Quantity = 1
                    },
                    new CartItemViewModel
                    {
                        ProductId = p2.Id,
                        ProductName = p2.Name,
                        ProductSlug = p2.Slug,
                        Brand = p2.Brand,
                        Image = p2.MainImage,
                        Price = p2.Price,
                        OriginalPrice = p2.OriginalPrice,
                        SelectedCapacity = "1.5 Ton",
                        SelectedColor = "Pure White",
                        Quantity = 1
                    }
                },
                Timeline = new List<OrderTimelineStep>
                {
                    new OrderTimelineStep
                    {
                        Title = "Order Placed & Verified",
                        Description = "Payment confirmed via UPI. Electronic invoice generated.",
                        Timestamp = "15 Sep 2026, 04:30 PM",
                        IsCompleted = true,
                        IsCurrent = false
                    },
                    new OrderTimelineStep
                    {
                        Title = "Quality Inspected & Packed",
                        Description = "Multi-layer shockproof crating completed at Aura Central Hub, Bhiwandi.",
                        Timestamp = "16 Sep 2026, 08:15 AM",
                        IsCompleted = true,
                        IsCurrent = false
                    },
                    new OrderTimelineStep
                    {
                        Title = "Dispatched with White-Glove Fleet",
                        Description = "Heavy appliance specialized logistics vehicle in transit to Mumbai Hub.",
                        Timestamp = "16 Sep 2026, 02:40 PM",
                        IsCompleted = true,
                        IsCurrent = true
                    },
                    new OrderTimelineStep
                    {
                        Title = "Out for Delivery & Installation",
                        Description = "Certified technician assigned for delivery, unboxing and demo.",
                        Timestamp = "Expected 17 Sep 2026, 11:00 AM",
                        IsCompleted = false,
                        IsCurrent = false
                    },
                    new OrderTimelineStep
                    {
                        Title = "Delivered & Demo Signed",
                        Description = "Appliance leveled, test run completed, warranty registered.",
                        Timestamp = "Pending delivery",
                        IsCompleted = false,
                        IsCurrent = false
                    }
                }
            },
            new OrderViewModel
            {
                Id = "AURA-83421",
                OrderDate = DateTime.Now.AddDays(-24),
                Status = "Delivered",
                EstimatedDelivery = "Delivered on 24 Aug 2026",
                SubTotal = 62900,
                Discount = 2000,
                GrandTotal = 60900,
                PaymentMethod = "Credit Card (HDFC Regalia •••• 9812)",
                ShippingAddress = _addresses[0],
                Items = new List<CartItemViewModel>
                {
                    new CartItemViewModel
                    {
                        ProductId = p3.Id,
                        ProductName = p3.Name,
                        ProductSlug = p3.Slug,
                        Brand = p3.Brand,
                        Image = p3.MainImage,
                        Price = p3.Price,
                        OriginalPrice = p3.OriginalPrice,
                        SelectedCapacity = "Standard 0.77L",
                        SelectedColor = "Iron / Yellow",
                        Quantity = 1
                    }
                },
                Timeline = new List<OrderTimelineStep>
                {
                    new OrderTimelineStep
                    {
                        Title = "Delivered & Registered",
                        Description = "Delivered safely by Aura White-Glove logistics. 2-Year official warranty activated.",
                        Timestamp = "24 Aug 2026, 01:20 PM",
                        IsCompleted = true,
                        IsCurrent = false
                    }
                }
            }
        };
    }

    private List<ReturnRequestViewModel> InitializeReturns()
    {
        return new List<ReturnRequestViewModel>
        {
            new ReturnRequestViewModel
            {
                Id = "RET-1092",
                OrderId = "AURA-76120",
                ProductName = "Philips Digital Airfryer Basket Accessory Set",
                Reason = "Ordered incorrect model size for existing appliance",
                Status = "Refund Initiated",
                RequestDate = "10 Aug 2026",
                RefundAmount = 2490
            }
        };
    }

    private List<ReviewItemViewModel> InitializeAccountReviews()
    {
        return new List<ReviewItemViewModel>
        {
            new ReviewItemViewModel
            {
                Id = "AR-01",
                ProductId = "AURA-VAC-001",
                ProductName = "Dyson V15 Detect Absolute Extra Cordless Vacuum Cleaner",
                ProductImage = "https://images.unsplash.com/photo-1558317374-067fb5f30001?auto=format&fit=crop&w=400&q=80",
                Rating = 5,
                Title = "Worth every rupee - dust reveal laser is magical",
                Comment = "Cleaned carpets and mattresses like nothing else could. The laser illuminates dust you literally couldn't see with regular room lights.",
                Date = "26 Aug 2026",
                IsSubmitted = true
            },
            new ReviewItemViewModel
            {
                Id = "AR-02",
                ProductId = "AURA-AC-001",
                ProductName = "Daikin 1.5 Ton 5-Star Triple Display Inverter Split AC",
                ProductImage = "https://images.unsplash.com/photo-1621905251189-08b45d6a269e?auto=format&fit=crop&w=400&q=80",
                Rating = 5,
                Title = "Cools in under 3 minutes silently",
                Comment = "Installation was handled flawlessly by Aura Living technicians. Completely satisfied.",
                Date = "Pending Your Review",
                IsSubmitted = false
            }
        };
    }
}
