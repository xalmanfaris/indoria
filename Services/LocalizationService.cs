using Microsoft.AspNetCore.Http;

namespace AuraLiving.Services;

public class LanguageInfo
{
    public string Code { get; set; } = "en";
    public string Name { get; set; } = "English";
    public string NativeName { get; set; } = "English";
    public string Flag { get; set; } = "🇺🇸";
    public bool IsRtl { get; set; } = false;
}

public interface ILocalizationService
{
    List<LanguageInfo> GetSupportedLanguages();
    string GetCurrentLanguageCode();
    LanguageInfo GetCurrentLanguage();
    void SetCurrentLanguage(string languageCode);
    bool IsRtl();
    string GetString(string key);
    string this[string key] { get; }
}

public class LocalizationService : ILocalizationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    private static readonly Dictionary<string, LanguageInfo> Languages = new(StringComparer.OrdinalIgnoreCase)
    {
        { "en", new LanguageInfo { Code = "en", Name = "English", NativeName = "English", Flag = "🇺🇸", IsRtl = false } },
        { "es", new LanguageInfo { Code = "es", Name = "Spanish", NativeName = "Español", Flag = "🇪🇸", IsRtl = false } },
        { "fr", new LanguageInfo { Code = "fr", Name = "French", NativeName = "Français", Flag = "🇫🇷", IsRtl = false } },
        { "de", new LanguageInfo { Code = "de", Name = "German", NativeName = "Deutsch", Flag = "🇩🇪", IsRtl = false } },
        { "hi", new LanguageInfo { Code = "hi", Name = "Hindi", NativeName = "हिन्दी", Flag = "🇮🇳", IsRtl = false } },
        { "ar", new LanguageInfo { Code = "ar", Name = "Arabic", NativeName = "العربية", Flag = "🇦🇪", IsRtl = true } }
    };

    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new(StringComparer.OrdinalIgnoreCase)
    {
        { "es", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "Home", "Inicio" },
            { "Categories", "Categorías" },
            { "Shop", "Tienda" },
            { "Search", "Buscar" },
            { "Shop Now", "Comprar Ahora" },
            { "Cart", "Carrito" },
            { "Wishlist", "Lista de Deseos" },
            { "Account", "Mi Cuenta" },
            { "Sign In", "Iniciar Sesión" },
            { "Sign Out", "Cerrar Sesión" },
            { "Customer Support", "Soporte al Cliente" },
            { "Support Tickets", "Tickets de Soporte" }
        }},
        { "fr", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "Home", "Accueil" },
            { "Categories", "Catégories" },
            { "Shop", "Boutique" },
            { "Search", "Rechercher" },
            { "Shop Now", "Acheter Maintenant" },
            { "Cart", "Panier" },
            { "Wishlist", "Favoris" },
            { "Account", "Mon Compte" },
            { "Sign In", "Se Connecter" },
            { "Sign Out", "Déconnexion" },
            { "Customer Support", "Service Client" },
            { "Support Tickets", "Tickets de Support" }
        }},
        { "de", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "Home", "Startseite" },
            { "Categories", "Kategorien" },
            { "Shop", "Geschäft" },
            { "Search", "Suchen" },
            { "Shop Now", "Jetzt Kaufen" },
            { "Cart", "Warenkorb" },
            { "Wishlist", "Wunschliste" },
            { "Account", "Konto" },
            { "Sign In", "Anmelden" },
            { "Sign Out", "Abmelden" },
            { "Customer Support", "Kundenservice" },
            { "Support Tickets", "Support-Tickets" }
        }},
        { "hi", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "Home", "होम" },
            { "Categories", "श्रेणियां" },
            { "Shop", "दुकान" },
            { "Search", "खोजें" },
            { "Shop Now", "अभी खरीदें" },
            { "Cart", "कार्ट" },
            { "Wishlist", "विशलिस्ट" },
            { "Account", "खाता" },
            { "Sign In", "साइन इन करें" },
            { "Sign Out", "साइन आउट करें" },
            { "Customer Support", "ग्राहक सहायता" },
            { "Support Tickets", "सहायता टिकट" }
        }},
        { "ar", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "Home", "الرئيسية" },
            { "Categories", "الفئات" },
            { "Shop", "المتجر" },
            { "Search", "بحث" },
            { "Search appliances...", "البحث عن الأجهزة المنزلية..." },
            { "Shop Now", "تسوق الآن" },
            { "Cart", "سلة التسوق" },
            { "Shopping Bag", "حقيبة التسوق" },
            { "Wishlist", "قائمة الرغبات" },
            { "Account", "حسابي" },
            { "My Account", "حسابي الشخصي" },
            { "Dashboard", "لوحة التحكم" },
            { "Sign In", "تسجيل الدخول" },
            { "Sign Out", "تسجيل الخروج" },
            { "Log Out", "تسجيل الخروج" },
            { "Create Account", "إنشاء حساب" },
            { "Welcome to Indoria", "مرحباً بكم في إندوريا" },
            { "Sign in to access your profile", "سجل الدخول للوصول إلى ملفك الشخصي" },
            { "Customer Support", "دعم العملاء" },
            { "Support Tickets", "تذاكر الدعم" },
            { "Support Desk", "مكتب الدعم" },
            { "Regional Preferences", "التفضيلات الإقليمية" },
            { "Shipping Country", "بلد الشحن" },
            { "Currency", "العملة" },
            { "Language", "اللغة" },
            { "Notifications", "الإشعارات" },
            { "Mark all as read", "تحديد الكل كمقروء" },
            { "Loading alerts...", "جاري تحميل التنبيهات..." },
            { "No new notifications", "لا توجد إشعارات جديدة" },
            { "All Orders", "جميع الطلبات" },
            { "Processing", "قيد المعالجة" },
            { "Shipped", "تم الشحن" },
            { "In-Transit", "في الطريق" },
            { "Delivered", "تم التوصيل" },
            { "Cancelled", "ملغى" },
            { "Orders", "الطلبات" },
            { "My Orders", "طلباتي" },
            { "Addresses", "العناوين" },
            { "Saved Addresses", "العناوين المحفوظة" },
            { "Wishlist Saved", "العناصر المحفوظة" },
            { "Total Orders Placed", "إجمالي الطلبات" },
            { "Privilege Points", "نقاط الامتياز" },
            { "Reviews", "المراجعات" },
            { "My Product Reviews", "مراجعات منتجاتي" },
            { "Returns & Claims", "الإرجاع والمطالبات" },
            { "Profile Settings", "إعدادات الملف الشخصي" },
            { "Admin Console", "لوحة الإدارة" },
            { "Support Console", "لوحة الدعم" },
            { "Settings", "الإعدادات" },
            { "Add to Cart", "إضافة إلى السلة" },
            { "Buy Now", "اشترِ الآن" },
            { "Out of Stock", "نفدت الكمية" },
            { "In Stock", "متوفر في المخزون" },
            { "Price", "السعر" },
            { "Quantity", "الكمية" },
            { "Total", "الإجمالي" },
            { "Subtotal", "المجموع الفرعي" },
            { "Tax", "الضريبة" },
            { "Shipping", "الشحن" },
            { "Free Shipping", "شحن مجاني" },
            { "Checkout", "إتمام الطلب" },
            { "Place Order", "تأكيد الطلب" },
            { "Cancel Order", "إلغاء الطلب" },
            { "Track Order", "تتبع الطلب" },
            { "Order Details", "تفاصيل الطلب" },
            { "Write Review", "كتابة مراجعة" },
            { "Submitted", "تم الإرسال" },
            { "Published & Verified", "منشور وموثق" },
            { "Under Moderation", "قيد المراجعة" },
            { "Rating", "التقييم" },
            { "Verified Buyer", "مشتري موثق" },
            { "Indoria Support Desk", "مكتب دعم إندوريا" },
            { "Customer Care Ready", "خدمة العملاء جاهزة" },
            { "Customer Care Online", "خدمة العملاء متصلة" },
            { "AI Assistant Active", "مساعد الذكاء الاصطناعي نشط" },
            { "End Conversation", "إنهاء المحادثة" },
            { "Type a message...", "اكتب رسالة..." },
            { "Refrigerators", "الثلاجات" },
            { "Air Conditioners", "مكيفات الهواء" },
            { "Washing Machines", "غسالات الملابس" },
            { "Kitchen Appliances", "أجهزة المطبخ" },
            { "Smart TVs", "التلفزيونات الذكية" },
            { "Microwaves & Ovens", "المايكروويف والأفران" },
            { "Air Purifiers", "منقيات الهواء" },
            { "Dishwashers", "غسالات الأطباق" },
            { "Home Appliances", "الأجهزة المنزلية" },
            { "Luxury Suites", "الأجهزة الفاخرة" },
            { "Cooling", "التبريد" },
            { "Kitchen", "المطبخ" },
            { "Laundry", "الغسيل" },
            { "Smart Refrigerators", "الثلاجات الذكية" },
            { "Front-Load Washers", "غسالات التعبئة الأمامية" },
            { "5-Star Inverter ACs", "مكيفات إنفرتر 5 نجوم" },
            { "4K OLED & QLED TVs", "تلفزيونات 4K OLED و QLED" },
            { "Built-in Kitchen Hobs", "مواقد المطبخ المدمجة" },
            { "14-Place Dishwashers", "غسالات أطباق 14 مكان" },
            { "Modern Home Appliances", "أجهزة منزلية حديثة" },
            { "Elevate Your Living Space", "ارتقِ بمساحة معيشك" },
            { "Featured Appliances", "أجهزة مميزة" },
            { "Trending Collection", "التشكيلة الأكثر رواجاً" },
            { "Best Sellers", "الأكثر مبيعاً" },
            { "Verified Experiences", "تجارب موثوقة" },
            { "What Our Patrons Say", "ماذا يقول عملاؤنا" },
            { "4.9 / 5 Verified Satisfaction", "تقييم 4.9 / 5 رضا موثق" },
            { "Verified Owner", "مالك موثق" },
            { "Customer Review", "مراجعة عميل" },
            { "Authorized Direct Manufacturer Warranty Honors", "ضمان معتمد مباشرة من الشركات المصنعة" },

            { "Customer Care", "عناية العملاء" },
            { "Indoria Insider Club", "نادي إندوريا الحصري" },
            { "Curated Launch Deals & Private Offers", "عروض إطلاق تقييمية وعروض خاصة" },
            { "Enter your email address", "أدخل عنوان بريدك الإلكتروني" },
            { "Subscribe", "اشتراك" },
            { "100% Brand Sealed", "مختوم 100% من المصنع" },
            { "Direct manufacturer stock", "مخزون مباشر من المصنع" },
            { "Free Expert Install", "تركيب مجاني من الخبراء" },
            { "Brand certified technicians", "فنيون معتمدون" },
            { "2-Year Indoria Care", "رعاية إندوريا لمدة سنتين" },
            { "Comprehensive warranty", "ضمان شامل" },
            { "White-Glove Delivery", "توصيل عالي الجودة" },
            { "Curated Appliances", "الأجهزة المختارة" },
            { "Help & Support Centre", "مركز المساعدة والدعم" },
            { "Track Your Shipment", "تتبع شحنتك" },
            { "Request a Return", "طلب إرجاع" },
            { "Delivery & Installation", "التوصيل والتركيب" },
            { "15-Day Return Policy", "سياسة إرجاع خلال 15 يوماً" },
            { "Experience Showrooms", "معارض التجربة" },
            { "About & Hotline", "عنا والمعلومات" },
            { "Our Story & Craft", "قصتنا وخبرتنا" },
            { "Appliance Buying Guides", "دليل شراء الأجهزة" },
            { "Terms & Conditions", "الشروط والأحكام" },
            { "Privacy Policy", "سياسة الخصوصية" },
            { "Concierge Hotline", "الخط الساخن للمساعدة" },
            { "Protected Payments:", "مدفوعات محمية:" },
            { "All rights reserved.", "جميع الحقوق محفوظة." },
            { "Quick View", "عرض سريع" },
            { "Filter Products", "تصفية المنتجات" },
            { "Sort By", "ترتيب حسب" },
            { "Price: Low to High", "السعر: من الأقل للأعلى" },
            { "Price: High to Low", "السعر: من الأعلى للأقل" },
            { "Newest Arrivals", "أحدث المنتجات" },
            { "Featured", "مميز" },
            { "All Categories", "جميع الفئات" },
            { "All Brands", "جميع العلامات التجارية" },
            { "Check availability", "التحقق من التوفر" },
            { "Enter 6-digit PIN code", "أدخل الرمز البريدي 6 أرقام" },
            { "Check", "تحقق" },
            { "Shopping Cart", "سلة التسوق" },
            { "Order Summary", "ملخص الطلب" },
            { "Coupon Code", "رمز القسيمة" },
            { "Apply", "تطبيق" },
            { "Shipping Address", "عنوان الشحن" },
            { "Payment Method", "طريقة الدفع" },
            { "Cash on Delivery", "الدفع عند الاستلام" },
            { "Credit/Debit Card", "بطاقة ائتمان / مدى" },
            { "UPI / NetBanking", "تحويل بنكي / إلكتروني" },
            { "Sign Out Confirmation", "تأكيد تسجيل الخروج" },
            { "Are you sure you want to sign out?", "هل أنت تأكد من أنك تريد تسجيل الخروج؟" },
            { "Item added to cart", "تمت إضافة المنتج إلى السلة" },
            { "Coupon applied successfully!", "تم تطبيق القسيمة بنجاح!" }
        }}
    };

    public LocalizationService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public List<LanguageInfo> GetSupportedLanguages()
    {
        return Languages.Values.ToList();
    }

    public string GetCurrentLanguageCode()
    {
        var cookie = _httpContextAccessor.HttpContext?.Request.Cookies["indoria_culture"];
        if (!string.IsNullOrEmpty(cookie) && Languages.ContainsKey(cookie))
        {
            return cookie.ToLowerInvariant();
        }
        return "en";
    }

    public LanguageInfo GetCurrentLanguage()
    {
        var code = GetCurrentLanguageCode();
        return Languages.TryGetValue(code, out var info) ? info : Languages["en"];
    }

    public void SetCurrentLanguage(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode) || !Languages.ContainsKey(languageCode))
            return;

        _httpContextAccessor.HttpContext?.Response.Cookies.Append("indoria_culture", languageCode.ToLowerInvariant(), new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            Path = "/",
            HttpOnly = false,
            IsEssential = true
        });
    }

    public bool IsRtl()
    {
        return GetCurrentLanguage().IsRtl;
    }

    public string GetString(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        var code = GetCurrentLanguageCode();
        if (Translations.TryGetValue(code, out var dict) && dict.TryGetValue(key, out var translated))
        {
            return translated;
        }
        return key; // Default to English key
    }

    public string this[string key] => GetString(key);
}
