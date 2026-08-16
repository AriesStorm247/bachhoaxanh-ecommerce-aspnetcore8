using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".aspnet-data-protection-keys")));

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;

    // Password requirements
    options.Password.RequiredLength = 8;            // tối thiểu 8 ký tự
    options.Password.RequireDigit = true;           // phải có chữ số
    options.Password.RequireUppercase = true;        // phải có chữ hoa
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = true;  // phải có ký tự đặc biệt
    options.Password.RequiredUniqueChars = 1;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "dummy-google-client-id";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "dummy-google-client-secret";
        options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
        {
            OnRedirectToAuthorizationEndpoint = context =>
            {
                if (context.RedirectUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    context.RedirectUri = "https://" + context.RedirectUri.Substring(7);
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRemoteFailure = context =>
            {
                var factory = (Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory)context.HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory));
                var tempData = factory?.GetTempData(context.HttpContext);
                if (tempData != null)
                {
                    tempData["ErrorMessage"] = "Đăng nhập bằng tài khoản Google thất bại hoặc đã bị hủy.";
                }
                context.Response.Redirect("/Identity/Account/Login");
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    })
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? "dummy-facebook-app-id";
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "dummy-facebook-app-secret";
        options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
        {
            OnRedirectToAuthorizationEndpoint = context =>
            {
                if (context.RedirectUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    context.RedirectUri = "https://" + context.RedirectUri.Substring(7);
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRemoteFailure = context =>
            {
                var factory = (Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory)context.HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory));
                var tempData = factory?.GetTempData(context.HttpContext);
                if (tempData != null)
                {
                    tempData["ErrorMessage"] = "Đăng nhập bằng tài khoản Facebook thất bại hoặc đã bị hủy.";
                }
                context.Response.Redirect("/Identity/Account/Login");
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    });

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<LoyaltyService>();
builder.Services.AddScoped<AprioriService>();
builder.Services.AddScoped<OpenAIService>();
builder.Services.AddScoped<InventoryBatchService>();
builder.Services.AddScoped<IDiscountService, DiscountService>();
builder.Services.AddHttpClient<IPaymentGatewayService, PaymentGatewayService>();
builder.Services.AddTransient<IEmailSender, EmailService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<LeaderboardService>();
builder.Services.AddHostedService<LeaderboardSettlementJob>();
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
var app = builder.Build();

// Auto-upgrade database schema to add EquippedAvatarFrame and LastActiveTime columns if missing
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        context.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (
                SELECT * FROM sys.columns 
                WHERE object_id = OBJECT_ID('Products') 
                  AND name = 'IsVisible'
            )
            BEGIN
                ALTER TABLE Products ADD IsVisible BIT NOT NULL DEFAULT 1;
            END

            IF NOT EXISTS (
                SELECT * FROM sys.columns 
                WHERE object_id = OBJECT_ID('Products') 
                  AND name = 'IsHot'
            )
            BEGIN
                ALTER TABLE Products ADD IsHot BIT NOT NULL DEFAULT 0;
            END

            IF NOT EXISTS (
                SELECT * FROM sys.columns 
                WHERE object_id = OBJECT_ID('Products') 
                  AND name = 'IsBestSeller'
            )
            BEGIN
                ALTER TABLE Products ADD IsBestSeller BIT NOT NULL DEFAULT 0;
            END

            IF NOT EXISTS (
                SELECT * FROM sys.columns 
                WHERE object_id = OBJECT_ID('CustomerProfiles') 
                  AND name = 'EquippedAvatarFrame'
            )
            BEGIN
                ALTER TABLE CustomerProfiles ADD EquippedAvatarFrame NVARCHAR(50) NULL;
            END

            IF NOT EXISTS (
                SELECT * FROM sys.columns 
                WHERE object_id = OBJECT_ID('CustomerProfiles') 
                  AND name = 'LastActiveTime'
            )
            BEGIN
                ALTER TABLE CustomerProfiles ADD LastActiveTime DATETIME NULL;
            END

            IF NOT EXISTS (
                SELECT * FROM sys.columns 
                WHERE object_id = OBJECT_ID('CustomerProfiles') 
                  AND name = 'IsOnline'
            )
            BEGIN
                ALTER TABLE CustomerProfiles ADD IsOnline BIT NOT NULL DEFAULT 0;
            END

            IF NOT EXISTS (
                SELECT * FROM sys.columns 
                WHERE object_id = OBJECT_ID('ChatHistories') 
                  AND name = 'UserId'
            )
            BEGIN
                ALTER TABLE ChatHistories ADD UserId NVARCHAR(450) NULL;
            END

            IF NOT EXISTS (
                SELECT * FROM sys.columns 
                WHERE object_id = OBJECT_ID('CustomerProfiles') 
                  AND name = 'GoogleId'
            )
            BEGIN
                ALTER TABLE CustomerProfiles ADD GoogleId NVARCHAR(100) NULL;
            END

            IF NOT EXISTS (
                SELECT * FROM sys.columns 
                WHERE object_id = OBJECT_ID('CustomerProfiles') 
                  AND name = 'GoogleEmail'
            )
            BEGIN
                ALTER TABLE CustomerProfiles ADD GoogleEmail NVARCHAR(256) NULL;
            END

            IF NOT EXISTS (
                SELECT * FROM sys.columns 
                WHERE object_id = OBJECT_ID('CustomerProfiles') 
                  AND name = 'FacebookId'
            )
            BEGIN
                ALTER TABLE CustomerProfiles ADD FacebookId NVARCHAR(100) NULL;
            END

            IF NOT EXISTS (
                SELECT * FROM sys.columns 
                WHERE object_id = OBJECT_ID('CustomerProfiles') 
                  AND name = 'FacebookName'
            )
            BEGIN
                ALTER TABLE CustomerProfiles ADD FacebookName NVARCHAR(256) NULL;
            END

            -- Migrate old daily normal frames to new names
            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CustomerProfiles')
            BEGIN
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-normal-khung-thuong-01.webp' WHERE EquippedAvatarFrame = 'daily-normal-khung-ban-tay.webp';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-normal-khung-thuong-02.png' WHERE EquippedAvatarFrame = 'daily-normal-khung-basketball.png';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-normal-khung-thuong-03.webp' WHERE EquippedAvatarFrame = 'daily-normal-khung-be-ca.webp';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-normal-khung-thuong-04.png' WHERE EquippedAvatarFrame = 'daily-normal-khung-dark-dragon.png';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-normal-khung-thuong-05.png' WHERE EquippedAvatarFrame = 'daily-normal-khung-he-moc.png';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-normal-khung-thuong-06.webp' WHERE EquippedAvatarFrame = 'daily-normal-khung-kinh-mat.webp';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-normal-khung-thuong-07.webp' WHERE EquippedAvatarFrame = 'daily-normal-khung-love-you.webp';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-normal-khung-thuong-08.png' WHERE EquippedAvatarFrame = 'daily-normal-khung-mu-phu-thuy-2.png';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-normal-khung-thuong-09.png' WHERE EquippedAvatarFrame = 'daily-normal-khung-mu-phu-thuy.png';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-normal-khung-thuong-10.webp' WHERE EquippedAvatarFrame = 'daily-normal-khung-no-vit.webp';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-normal-khung-thuong-11.webp' WHERE EquippedAvatarFrame = 'daily-normal-khung-noi-gian.webp';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-normal-khung-thuong-12.png' WHERE EquippedAvatarFrame = 'daily-normal-khung-ruong-bau.png';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-normal-khung-thuong-13.png' WHERE EquippedAvatarFrame = 'daily-normal-khung-star-war.png';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-normal-khung-thuong-14.webp' WHERE EquippedAvatarFrame = 'daily-normal-khung-tieu-quy.webp';

                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-limited-khung 01.webp' WHERE EquippedAvatarFrame = 'daily-limited-khung-ac-quy.webp';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-limited-khung 02.png' WHERE EquippedAvatarFrame = 'daily-limited-khung-dau-lan.png';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-limited-khung 03.png' WHERE EquippedAvatarFrame = 'daily-limited-khung-hac-am.png';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-limited-khung 04.png' WHERE EquippedAvatarFrame = 'daily-limited-khung-mat-na-quy.png';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-limited-khung 05.png' WHERE EquippedAvatarFrame = 'daily-limited-khung-rubik-co.png';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-limited-khung 06.png' WHERE EquippedAvatarFrame = 'daily-limited-khung-soul-dragon.png';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-limited-khung 07.webp' WHERE EquippedAvatarFrame = 'daily-limited-khung-thi-nghiem.png';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-limited-khung 08.png' WHERE EquippedAvatarFrame = 'daily-limited-khung-twin-cat.webp';
                UPDATE CustomerProfiles SET EquippedAvatarFrame = 'daily-limited-khung 09.png' WHERE EquippedAvatarFrame = 'daily-limited-khung-xe-rach-hu-khong.png';

                -- Reset daily frame rotation JSON to force regeneration with new names
                UPDATE CustomerProfiles SET DailyFramesJson = NULL, DailyFramesLastResetDate = NULL;
            END

            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CustomerVouchers')
            BEGIN
                UPDATE CustomerVouchers SET [Key] = 'daily-normal-khung-thuong-01.webp' WHERE [Key] = 'daily-normal-khung-ban-tay.webp';
                UPDATE CustomerVouchers SET [Key] = 'daily-normal-khung-thuong-02.png' WHERE [Key] = 'daily-normal-khung-basketball.png';
                UPDATE CustomerVouchers SET [Key] = 'daily-normal-khung-thuong-03.webp' WHERE [Key] = 'daily-normal-khung-be-ca.webp';
                UPDATE CustomerVouchers SET [Key] = 'daily-normal-khung-thuong-04.png' WHERE [Key] = 'daily-normal-khung-dark-dragon.png';
                UPDATE CustomerVouchers SET [Key] = 'daily-normal-khung-thuong-05.png' WHERE [Key] = 'daily-normal-khung-he-moc.png';
                UPDATE CustomerVouchers SET [Key] = 'daily-normal-khung-thuong-06.webp' WHERE [Key] = 'daily-normal-khung-kinh-mat.webp';
                UPDATE CustomerVouchers SET [Key] = 'daily-normal-khung-thuong-07.webp' WHERE [Key] = 'daily-normal-khung-love-you.webp';
                UPDATE CustomerVouchers SET [Key] = 'daily-normal-khung-thuong-08.png' WHERE [Key] = 'daily-normal-khung-mu-phu-thuy-2.png';
                UPDATE CustomerVouchers SET [Key] = 'daily-normal-khung-thuong-09.png' WHERE [Key] = 'daily-normal-khung-mu-phu-thuy.png';
                UPDATE CustomerVouchers SET [Key] = 'daily-normal-khung-thuong-10.webp' WHERE [Key] = 'daily-normal-khung-no-vit.webp';
                UPDATE CustomerVouchers SET [Key] = 'daily-normal-khung-thuong-11.webp' WHERE [Key] = 'daily-normal-khung-noi-gian.webp';
                UPDATE CustomerVouchers SET [Key] = 'daily-normal-khung-thuong-12.png' WHERE [Key] = 'daily-normal-khung-ruong-bau.png';
                UPDATE CustomerVouchers SET [Key] = 'daily-normal-khung-thuong-13.png' WHERE [Key] = 'daily-normal-khung-star-war.png';
                UPDATE CustomerVouchers SET [Key] = 'daily-normal-khung-thuong-14.webp' WHERE [Key] = 'daily-normal-khung-tieu-quy.webp';

                UPDATE CustomerVouchers SET [Key] = 'daily-limited-khung 01.webp' WHERE [Key] = 'daily-limited-khung-ac-quy.webp';
                UPDATE CustomerVouchers SET [Key] = 'daily-limited-khung 02.png' WHERE [Key] = 'daily-limited-khung-dau-lan.png';
                UPDATE CustomerVouchers SET [Key] = 'daily-limited-khung 03.png' WHERE [Key] = 'daily-limited-khung-hac-am.png';
                UPDATE CustomerVouchers SET [Key] = 'daily-limited-khung 04.png' WHERE [Key] = 'daily-limited-khung-mat-na-quy.png';
                UPDATE CustomerVouchers SET [Key] = 'daily-limited-khung 05.png' WHERE [Key] = 'daily-limited-khung-rubik-co.png';
                UPDATE CustomerVouchers SET [Key] = 'daily-limited-khung 06.png' WHERE [Key] = 'daily-limited-khung-soul-dragon.png';
                UPDATE CustomerVouchers SET [Key] = 'daily-limited-khung 07.webp' WHERE [Key] = 'daily-limited-khung-thi-nghiem.png';
                UPDATE CustomerVouchers SET [Key] = 'daily-limited-khung 08.png' WHERE [Key] = 'daily-limited-khung-twin-cat.webp';
                UPDATE CustomerVouchers SET [Key] = 'daily-limited-khung 09.png' WHERE [Key] = 'daily-limited-khung-xe-rach-hu-khong.png';
            END
        ");

        // Create ProductReviews table if not exists
        context.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductReviews')
            BEGIN
                CREATE TABLE ProductReviews (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    ProductId INT NOT NULL,
                    UserId NVARCHAR(450) NOT NULL,
                    UserEmail NVARCHAR(256) NOT NULL,
                    UserFullName NVARCHAR(256) NOT NULL,
                    Rating INT NOT NULL,
                    Comment NVARCHAR(1000) NOT NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                    CONSTRAINT FK_ProductReviews_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
                );
            END
        ");

        context.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InventoryBatches')
            BEGIN
                CREATE TABLE InventoryBatches (
                    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryBatches PRIMARY KEY,
                    ProductId INT NOT NULL,
                    BranchId INT NOT NULL,
                    BatchCode NVARCHAR(60) NOT NULL,
                    ImportDate DATETIME2 NOT NULL,
                    ExpiryDate DATETIME2 NOT NULL,
                    Quantity DECIMAL(18,3) NOT NULL,
                    OriginalQuantity DECIMAL(18,3) NOT NULL,
                    SupplierName NVARCHAR(200) NULL,
                    Note NVARCHAR(500) NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT FK_InventoryBatches_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE,
                    CONSTRAINT FK_InventoryBatches_Branches FOREIGN KEY (BranchId) REFERENCES Branches(Id) ON DELETE CASCADE
                );

                CREATE INDEX IX_InventoryBatches_ProductId_BranchId_ExpiryDate
                    ON InventoryBatches(ProductId, BranchId, ExpiryDate);
            END

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InventoryBatchDeductions')
            BEGIN
                CREATE TABLE InventoryBatchDeductions (
                    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryBatchDeductions PRIMARY KEY,
                    OrderId INT NOT NULL,
                    OrderDetailId INT NULL,
                    InventoryBatchId INT NOT NULL,
                    ProductId INT NOT NULL,
                    BranchId INT NOT NULL,
                    Quantity DECIMAL(18,3) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    IsRestored BIT NOT NULL DEFAULT 0,
                    CONSTRAINT FK_InventoryBatchDeductions_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id),
                    CONSTRAINT FK_InventoryBatchDeductions_OrderDetails FOREIGN KEY (OrderDetailId) REFERENCES OrderDetails(Id),
                    CONSTRAINT FK_InventoryBatchDeductions_InventoryBatches FOREIGN KEY (InventoryBatchId) REFERENCES InventoryBatches(Id),
                    CONSTRAINT FK_InventoryBatchDeductions_Products FOREIGN KEY (ProductId) REFERENCES Products(Id),
                    CONSTRAINT FK_InventoryBatchDeductions_Branches FOREIGN KEY (BranchId) REFERENCES Branches(Id)
                );

                CREATE INDEX IX_InventoryBatchDeductions_OrderId
                    ON InventoryBatchDeductions(OrderId);
                CREATE INDEX IX_InventoryBatchDeductions_InventoryBatchId
                    ON InventoryBatchDeductions(InventoryBatchId);
            END

            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductInventories')
            BEGIN
                IF NOT EXISTS (
                    SELECT * FROM sys.indexes
                    WHERE name = 'IX_ProductInventories_BranchId_ProductId'
                      AND object_id = OBJECT_ID('ProductInventories')
                )
                BEGIN
                    CREATE INDEX IX_ProductInventories_BranchId_ProductId
                        ON ProductInventories(BranchId, ProductId)
                        INCLUDE (Quantity);
                END

                IF NOT EXISTS (
                    SELECT * FROM sys.indexes
                    WHERE name = 'IX_ProductInventories_ProductId_BranchId'
                      AND object_id = OBJECT_ID('ProductInventories')
                )
                BEGIN
                    CREATE INDEX IX_ProductInventories_ProductId_BranchId
                        ON ProductInventories(ProductId, BranchId)
                        INCLUDE (Quantity);
                END
            END

            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'InventoryBatches')
               AND NOT EXISTS (
                    SELECT * FROM sys.indexes
                    WHERE name = 'IX_InventoryBatches_ProductId_BranchId_ExpiryDate'
                      AND object_id = OBJECT_ID('InventoryBatches')
                )
            BEGIN
                CREATE INDEX IX_InventoryBatches_ProductId_BranchId_ExpiryDate
                    ON InventoryBatches(ProductId, BranchId, ExpiryDate);
            END
        ");

        // Bổ sung cột Barcode vào bảng Products
        context.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (
                SELECT * FROM sys.columns 
                WHERE object_id = OBJECT_ID('Products') 
                  AND name = 'Barcode'
            )
            BEGIN
                ALTER TABLE Products ADD Barcode NVARCHAR(50) NULL;
            END
        ");

        // Tự động gán mã vạch EAN-13 chuẩn có số kiểm tra (check digit) cho các sản phẩm
        var productsWithNoBarcode = context.Products.ToList();
        bool hasChanges = false;
        foreach (var p in productsWithNoBarcode)
        {
            // Nếu chưa có mã vạch
            if (string.IsNullOrWhiteSpace(p.Barcode))
            {
                string baseStr = $"893{p.Id:D9}"; // pads ID to 9 digits, e.g. 893000000001
                int sumOdd = 0;
                int sumEven = 0;
                for (int i = 0; i < 12; i++)
                {
                    int digit = baseStr[i] - '0';
                    if (i % 2 == 0) // Vị trí lẻ (1, 3, 5...) tương ứng index chẵn 0, 2...
                        sumOdd += digit;
                    else
                        sumEven += digit;
                }
                int total = sumOdd + sumEven * 3;
                int check = (10 - (total % 10)) % 10;
                string newBarcode = baseStr + check.ToString();
                
                if (p.Barcode != newBarcode)
                {
                    p.Barcode = newBarcode;
                    hasChanges = true;
                }
            }
        }
        if (hasChanges)
        {
            context.SaveChanges();
        }

        // Seed default categories and products if empty
        DbSeeder.Seed(context);
    }
    catch { /* Table doesn't exist yet */ }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Middleware to sync persistent branch choice cookies to Session
app.Use(async (context, next) =>
{
    if (!context.Session.GetInt32("ActiveBranchId").HasValue)
    {
        if (context.Request.Cookies.TryGetValue("ActiveBranchId", out var cookieBranchIdStr) &&
            int.TryParse(cookieBranchIdStr, out int cookieBranchId))
        {
            context.Session.SetInt32("ActiveBranchId", cookieBranchId);
            if (context.Request.Cookies.TryGetValue("ActiveBranchName", out var cookieBranchName))
            {
                context.Session.SetString("ActiveBranchName", cookieBranchName);
            }
            if (context.Request.Cookies.TryGetValue("IsBranchExplicitlyChosen", out var isExplicit))
            {
                context.Session.SetString("IsBranchExplicitlyChosen", isExplicit);
            }
        }
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// Middleware to track customer activity for Online/Offline status
app.Use(async (context, next) =>
{
    try
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            using var scope = context.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var userId = userManager.GetUserId(context.User);
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    // Check if the user is locked out
                    var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
                    if (isLocked)
                    {
                        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<IdentityUser>>();
                        await signInManager.SignOutAsync();

                        var lockedProfile = await dbContext.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                        if (lockedProfile != null && lockedProfile.IsOnline)
                        {
                            lockedProfile.IsOnline = false;
                            lockedProfile.UpdatedAt = DateTime.Now;
                            await dbContext.SaveChangesAsync();
                        }

                        if (context.Request.Path.Value?.Equals("/Home/CheckLockoutStatus", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsJsonAsync(new { isLocked = true, email = user.Email });
                            return;
                        }

                        context.Response.Redirect($"/Identity/Account/Lockout?email={Uri.EscapeDataString(user.Email ?? string.Empty)}");
                        return; // Terminate request and redirect
                    }

                    var profile = await dbContext.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                    if (profile == null)
                    {
                        profile = new CustomerProfile
                        {
                            UserId = userId,
                            MembershipLevel = 0,
                            LoyaltyPoints = 0,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        dbContext.CustomerProfiles.Add(profile);
                    }

                    // Throttle database updates to once every minute, OR if IsOnline is false
                    bool shouldUpdate = !profile.LastActiveTime.HasValue || 
                                         (DateTime.Now - profile.LastActiveTime.Value).TotalMinutes >= 1 || 
                                         !profile.IsOnline;
                    if (shouldUpdate)
                    {
                        profile.LastActiveTime = DateTime.Now;
                        profile.IsOnline = true;
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
        }
    }
    catch { /* Ignore errors to avoid breaking the request pipeline */ }
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.Run();
