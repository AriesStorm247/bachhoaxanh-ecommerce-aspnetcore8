using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Data
{
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (!context.Categories.Any())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Categories ON");
                        var cats = new List<Category>
                        {
                            new Category { Id = 1, Name = "Thịt, cá, trứng" },
                            new Category { Id = 2, Name = "Rau, củ, trái cây" },
                            new Category { Id = 21, Name = "Dầu ăn, gia vị" },
                            new Category { Id = 22, Name = "Gạo, bột, đồ khô" },
                            new Category { Id = 23, Name = "Mì, miến, cháo" },
                            new Category { Id = 24, Name = "Sữa các loại" },
                            new Category { Id = 25, Name = "Kem, sữa chua" },
                            new Category { Id = 26, Name = "Thực phẩm đông mát" },
                            new Category { Id = 27, Name = "Bia, nước giải khát" },
                            new Category { Id = 28, Name = "Bánh kẹo các loại" },
                            new Category { Id = 29, Name = "Chăm sóc cá nhân" },
                            new Category { Id = 30, Name = "Vệ sinh nhà cửa" },
                            new Category { Id = 31, Name = "Sản phẩm mẹ & bé" }
                        };
                        context.Categories.AddRange(cats);
                        context.SaveChanges();
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Categories OFF");
                        transaction.Commit();
                    }
                    catch (Exception) { transaction.Rollback(); }
                }
            }

            if (!context.Products.Any())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Products ON");
                        var prods = new List<Product>
                        {
                            new Product { Id = 1, Name = "Mì 3 Miền tôm chua cay", Price = 94000.00m, Amount = 99, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/production/2026/4/image/production/2026/4/image/Products/2565/80211/mi-chua-cay-3-mien-65g-thung_202604201515155809.jpg", CategoryId = 23, IsVisible = true, IsHot = false, IsBestSeller = true },
                            new Product { Id = 2, Name = "Chuối già giống Nam Mỹ", Price = 29000.00m, Amount = 102, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/8788/228923/bhx/chuoi-gia-giong-nam-my_202512031458490151.jpg", CategoryId = 2, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 3, Name = "Dưa hấu đỏ (Trái 3 kg)", Price = 36000.00m, Amount = 102, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/8788/358118/bhx/dua-hau-do-trai-29kg-31kg-1-trai_202510170938436960.jpg", CategoryId = 2, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 4, Name = "Nho mẫu đơn nội địa Trung", Price = 79000.00m, Amount = 200, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/8788/312875/bhx/nho-mau-don-noi-dia-trung_202510291307224156.jpg", CategoryId = 2, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 5, Name = "Thùng 24 lon bia Blanc 1664 330ml", Price = 410000.00m, Amount = 300, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2282/252737/bhx/thung-24-lon-bia-kronenbourg-1664-blanc-330ml_202508221613291052.jpg", CategoryId = 27, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 6, Name = "Bộ bàn chải và tăm chỉ Oral-Clean Optima", Price = 48000.00m, Amount = 288, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2491/337010/bhx/bo-ban-chai-danh-rang-va-tam-chi-nha-khoa-oral-clean_202504151021124210.jpg", CategoryId = 29, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 7, Name = "Kẹo dẻo vị soda kem trái cây Bibica Zoo Fruity Soda gói 24g", Price = 6000.00m, Amount = 206, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/7199/334879/bhx/keo-deo-vi-soda-kem-trai-cay-bibica-zoo-fruity-soda-goi-24g_202502201331154737.jpg", CategoryId = 28, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 8, Name = "Ổi Đài Loan", Price = 12500.00m, Amount = 121, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/8788/226921/bhx/oi-dai-loan-tui-1kg_202511031551328365.jpg", CategoryId = 2, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 9, Name = "Sữa tắm Johnson's sữa, gạo 500ml", Price = 147000.00m, Amount = 100, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdn.tgdd.vn/Products/Images/8678/76814/bhx/sua-tam-cho-be-johnsons-chua-sua-va-gao-500ml-202210241635302858.jpg", CategoryId = 31, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 10, Name = "Nước giặt OMO Matic hoa oải hương 2.1kg", Price = 115000.00m, Amount = 213, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2464/334295/bhx/nuoc-giat-omo-matic-cua-truoc-huong-oai-huong-thu-thai-tui-21kg_202512150955255912.jpg", CategoryId = 30, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 11, Name = "Bò xay Vinabeef vỉ 200g", Price = 68900.00m, Amount = 242, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/8139/358588/bhx/bo-xay-vinabeef-200g_202511041114113023.jpg", CategoryId = 1, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 44, Name = "Nạc dăm heo tuyển chọn khay 400g", Price = 52800.00m, Amount = 100, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/production/2026/1/image/Products/Images/8781/226842/bhx/nac-dam-heo-1kg_202601080950454483.jpg", CategoryId = 1, IsVisible = true, IsHot = false, IsBestSeller = true },
                            new Product { Id = 45, Name = "Trứng gà tươi CP hộp 10 quả", Price = 26000.00m, Amount = 150, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/8783/228775/bhx/hop-10-trung-ga-tuoi-tfood_202509301104427641.jpg", CategoryId = 1, IsVisible = true, IsHot = true, IsBestSeller = false },
                            new Product { Id = 46, Name = "Đùi tỏi gà tươi khay 400g", Price = 40016.00m, Amount = 120, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/production/2026/5/image/production/2026/5/image/Products/8790/233801/dui-toi-ga-1kg_202605251509137722.jpg", CategoryId = 1, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 47, Name = "Cải ngọt tươi xanh túi 400g", Price = 7000.00m, Amount = 79, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/production/2026/4/image/production/2026/4/image/Products/8820/335195/cai-ngot-tui-400gr_202604170939251833.jpg", CategoryId = 2, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 49, Name = "Dầu đậu nành Simply chai 1 lít", Price = 63000.00m, Amount = 200, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2286/76166/bhx/dau-an-simply-dau-nanh-1l_202510280955231988.jpg", CategoryId = 21, IsVisible = true, IsHot = true, IsBestSeller = true },
                            new Product { Id = 50, Name = "Nước mắm Nam Ngư chai 500ml", Price = 38500.00m, Amount = 180, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2289/76426/bhx/nmam-nam-ngu-chai-pet-500ml-24_202512021131236974.jpg", CategoryId = 21, IsVisible = true, IsHot = false, IsBestSeller = true },
                            new Product { Id = 51, Name = "Hạt nêm Knorr thịt thăn 170g", Price = 18100.00m, Amount = 149, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2806/195880/bhx/195880-thumb-moi_202501211125199922.jpg", CategoryId = 21, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 52, Name = "Gạo thơm Neptune ST25+ Extra túi 5kg", Price = 190000.00m, Amount = 100, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2513/324448/bhx/324448-1-2_202410301127133450.jpg", CategoryId = 22, IsVisible = true, IsHot = true, IsBestSeller = true },
                            new Product { Id = 53, Name = "Bột chiên giòn Aji-Quick gói 150g", Price = 11000.00m, Amount = 298, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2388/91563/bhx/91563-2_202411131034282234.jpg", CategoryId = 22, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 54, Name = "Nấm hương khay ", Price = 33000.00m, Amount = 119, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/8779/281521/bhx/nam-huong-hop-150g_202506051702492340.jpg", CategoryId = 2, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 55, Name = "Mì Hảo Hảo tôm chua cay gói 75g", Price = 4500.00m, Amount = 500, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2565/77622/bhx/77622_202410151353279924.jpg", CategoryId = 23, IsVisible = true, IsHot = true, IsBestSeller = true },
                            new Product { Id = 56, Name = "Phở bò trộn Đệ Nhất 84g", Price = 10600.00m, Amount = 400, Image = "https://cdn.tgdd.vn/Products/Images/2566/197492/bhx/pho-tron-de-nhat-vi-bo-goi-84g-202306171954314608.jpg", CategoryId = 23, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 57, Name = "Lốc 4 hộp sữa tươi Vinamilk ít đường 110ml", Price = 24000.00m, Amount = 300, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/production/2025/12/image/production/2025/12/image/Products/2386/198353/loc-4-hop-sua-tuoi-tiet-trung-vinamilk-100-sua-tuoi-it-duong-hop-110ml_202512221436394169.jpg", CategoryId = 24, IsVisible = true, IsHot = false, IsBestSeller = true },
                            new Product { Id = 58, Name = "Lốc 6 hộp sữa đậu nành ít đường Fami Canxi 200ml", Price = 29500.00m, Amount = 250, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2943/203659/bhx/203659-thumb-moi_202411061015341650.jpg", CategoryId = 24, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 59, Name = "Lốc 4 hộp sữa lúa mạch Milo Active Go 180ml", Price = 31000.00m, Amount = 200, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2945/84361/bhx/thuc-uong-dd-milo-180ml-loc_202511201438230324.jpg", CategoryId = 24, IsVisible = true, IsHot = true, IsBestSeller = true },
                            new Product { Id = 60, Name = "Lốc 4 hộp sữa chua TH True Yogurt nha đam 100g", Price = 35000.00m, Amount = 200, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/7558/214725/bhx/214725_202410290958211539.jpg", CategoryId = 25, IsVisible = true, IsHot = false, IsBestSeller = true },
                            new Product { Id = 61, Name = "Kem cacao sô cô la Merino Yeah! cây 68g", Price = 15000.00m, Amount = 150, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/7462/203951/bhx/kem-que-yeah-merino-cacao-socola-68g_202508201455552106.jpg", CategoryId = 25, IsVisible = true, IsHot = true, IsBestSeller = false },
                            new Product { Id = 63, Name = "Xúc xích Đức xông khói LC Foods gói 500g", Price = 80000.00m, Amount = 120, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/7618/239083/bhx/xuc-xich-duc-la-cusina-goi-500g_202512091525272492.jpg", CategoryId = 26, IsVisible = true, IsHot = true, IsBestSeller = true },
                            new Product { Id = 64, Name = "Chả giò thịt đặc biệt Cầu Tre gói 500g", Price = 65000.00m, Amount = 140, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/7171/195763/bhx/cha-gio-dac-biet-cau-tre-thit-heo-500g_202509291418266983.jpg", CategoryId = 26, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 65, Name = "Đậu hũ non sạch Ichiban hộp 280g", Price = 13000.00m, Amount = 180, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdn.tgdd.vn/Products/Images/7459/206302/bhx/dau-hu-non-vi-nguyen-hop-280g-202306191352542414.png", CategoryId = 26, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 66, Name = "Nước ngọt Coca Cola chai 390ml", Price = 7900.00m, Amount = 200, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2443/76450/bhx/76450_202411041427423848.jpg", CategoryId = 27, IsVisible = true, IsHot = false, IsBestSeller = true },
                            new Product { Id = 67, Name = "Nước tăng lực Redbull Thái Lan lon 250ml", Price = 16400.00m, Amount = 300, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/3226/336097/bhx/nuoc-tang-luc-redbull-thai-lon-250ml_202503311640192940.jpg", CategoryId = 27, IsVisible = true, IsHot = true, IsBestSeller = true },
                            new Product { Id = 68, Name = "Bánh chocopie Orion Dark ca cao 180g", Price = 34500.00m, Amount = 149, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdn.tgdd.vn/Products/Images/7622/145095/bhx/banh-chocopie-orion-dark-vi-ca-cao-hop-180g-6-cai-202401201138266542.jpg", CategoryId = 28, IsVisible = true, IsHot = false, IsBestSeller = true },
                            new Product { Id = 69, Name = "Snack vị sò điệp nướng bơ tỏi Lay's gói 50g", Price = 12000.00m, Amount = 200, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/production/2026/4/image/production/2026/4/image/Products/3364/365296/snack-khoai-tay-vi-so-diep-nuong-bo-toi-goi-50g_202604151633333241.png", CategoryId = 28, IsVisible = true, IsHot = true, IsBestSeller = false },
                            new Product { Id = 70, Name = "Dầu gội Clear sạch gầu mát lạnh hương gừng và hoa cúc 623ml", Price = 168000.00m, Amount = 149, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2483/339962/bhx/dau-goi-clear-gung-hoa-cuc-sach-gau-nuoi-duong-toc-623ml_202506301008329967.jpg", CategoryId = 29, IsVisible = true, IsHot = true, IsBestSeller = true },
                            new Product { Id = 71, Name = "Kem đánh răng Colgate ngừa sâu răng MaxFresh 225g", Price = 57000.00m, Amount = 179, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2446/161261/bhx/kem-danh-rang-colgate-maxfresh-huong-bac-ha-230g_202507161654053697.jpg", CategoryId = 29, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 72, Name = "Nước lau sàn Sunlight 1kg", Price = 41000.00m, Amount = 150, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/production/2026/5/image/production/2026/5/image/Products/2510/312850/nuoc-lau-san-sunlight-tinh-dau-thao-moc-huong-lavender-chai-997ml_202605251335313513.jpg", CategoryId = 30, IsVisible = true, IsHot = false, IsBestSeller = true },
                            new Product { Id = 73, Name = "Nước rửa chén Sunlight chanh mới túi 2.9kg", Price = 69000.00m, Amount = 200, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/production/2026/2/image/production/2026/2/image/Products/2387/299864/nuoc-rua-chen-sunlight-chiet-xuat-chanh-100-trai-chanh-tui-33-lit_202602041447157796.jpg", CategoryId = 30, IsVisible = true, IsHot = true, IsBestSeller = false },
                            new Product { Id = 76, Name = "Dầu đậu nành Simply chai 2 lít", Price = 123000.00m, Amount = 10, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2286/79394/bhx/dau-dau-nanh-nguyen-chat-simply-can-2-lit_202510281010361101.jpg", CategoryId = 21, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 77, Name = "6 lon Redbull 250ml", Price = 68000.00m, Amount = 189, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/3226/83742/bhx/83742_202410311535152280.jpg", CategoryId = 27, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 78, Name = "Thùng 24 lon Redbull Thái kẽm và vitamin 250ml", Price = 240000.00m, Amount = 180, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/3226/322546/bhx/thung-24-lon-nuoc-tang-luc-redbull-thai-kem-va-vitamin-250ml_202505261543146382.jpg", CategoryId = 27, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 79, Name = "Nước ngọt Coca Cola giảm đường chai 1.5 lít", Price = 21000.00m, Amount = 170, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/production/2026/6/image/Products/Images/2443/222274/bhx/frame-173_202606111553202923.jpg", CategoryId = 27, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 80, Name = "6 chai nước ngọt Coca Cola 390ml", Price = 46000.00m, Amount = 10, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2443/88651/bhx/88651_202411041455585772.jpg", CategoryId = 27, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 81, Name = "6 chai nước ngọt Coca Cola giảm đường 1.5 lít", Price = 122000.00m, Amount = 9, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2443/222276/bhx/222276_202411041549349439.jpg", CategoryId = 27, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 82, Name = "Gạo thơm lài túi 5kg", Price = 88000.00m, Amount = 177, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/production/2026/6/image/production/2026/6/image/Products/282956/gao-thom-thien-nhat-tui-5kg_202606101521279362.jpg", CategoryId = 22, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 83, Name = "Hộp 6 cây kem cacao sôcôla Merino Yeah! 68g", Price = 78000.00m, Amount = 179, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/7462/332718/bhx/hop-6-cay-kem-cacao-socola-merino-yeah-68g_202508201504189829.jpg", CategoryId = 25, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 84, Name = "Thùng 30 gói phở bò Đệ Nhất 65g", Price = 220000.00m, Amount = 0, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2566/82584/bhx/pho-an-lien-de-nhat-pho-bo-30goi-x-65g_202505130927168288.jpg", CategoryId = 23, IsVisible = true, IsHot = false, IsBestSeller = false },
                            new Product { Id = 85, Name = "Thùng 30 gói mì Hảo Hảo tôm chua cay 75g", Price = 125000.00m, Amount = 351, Image = "https://img.tgdd.vn/imgt/bhx/f_webp,fit_outside,quality_95/https://cdnv2.tgdd.vn/bhx-static/bhx/Products/Images/2565/85959/bhx/85959_202410151353322317.jpg", CategoryId = 23, IsVisible = true, IsHot = false, IsBestSeller = false }
                        };
                        context.Products.AddRange(prods);
                        context.SaveChanges();
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Products OFF");
                        transaction.Commit();
                    }
                    catch (Exception) { transaction.Rollback(); }
                }
            }
            if (context.Branches.Count() < 100 || !context.Branches.Any(b => b.District.StartsWith("Phường ") || b.District.StartsWith("Xã ")))
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        context.Database.ExecuteSqlRaw("DELETE FROM ProductInventories;");
                        context.Database.ExecuteSqlRaw("DELETE FROM Branches;");
                        transaction.Commit();
                        context.ChangeTracker.Clear(); // Xóa cache để query DB thực ở bước tiếp theo
                    }
                    catch
                    {
                        transaction.Rollback();
                        context.ChangeTracker.Clear();
                    }
                }
            }

            if (!context.Branches.Any())
            {
                var branches = new List<Branch>();
                string jsonPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "data", "branches.json");
                if (System.IO.File.Exists(jsonPath))
                {
                    try
                    {
                        var json = System.IO.File.ReadAllText(jsonPath);
                        var rawBranches = System.Text.Json.JsonSerializer.Deserialize<List<RawBranchJson>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (rawBranches != null)
                        {
                            var provinceMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "Tho", "Cần Thơ" },
                                { "D N?ng", "Đà Nẵng" },
                                { "H N?i", "Hà Nội" },
                                { "H?i Phng", "Hải Phòng" },
                                { "Thnh Ph? H? Ch Minh", "Hồ Chí Minh" },
                                { "Hu?", "Thừa Thiên Huế" },
                                { "An Giang", "An Giang" },
                                { "B?c Ninh", "Bắc Ninh" },
                                { "C Mau", "Cà Mau" },
                                { "D?k L?k", "Đắk Lắk" },
                                { "D?ng Nai", "Đồng Nai" },
                                { "D?ng Thp", "Đồng Tháp" },
                                { "Gia Lai", "Gia Lai" },
                                { "H Tinh", "Hà Tĩnh" },
                                { "Hung Yn", "Hưng Yên" },
                                { "Khnh Ha", "Khánh Hòa" },
                                { "Lm D?ng", "Lâm Đồng" },
                                { "Ngh? An", "Nghệ An" },
                                { "Ninh Bnh", "Ninh Bình" },
                                { "Qu?ng Ngai", "Quảng Ngãi" },
                                { "Qu?ng Ninh", "Quảng Ninh" },
                                { "Qu?ng Tr?", "Quảng Trị" },
                                { "Ty Ninh", "Tây Ninh" },
                                { "Thanh Ha", "Thanh Hóa" },
                                { "Vinh Long", "Vĩnh Long" }
                            };

                            int id = 1;
                            foreach (var rb in rawBranches)
                            {
                                var rawProv = rb.Province ?? "";
                                string prov = "";
                                if (provinceMap.TryGetValue(rawProv, out var cleanProv))
                                {
                                    prov = cleanProv;
                                }
                                else
                                {
                                    prov = rawProv.Replace("Thơ", "Cần Thơ").Replace("Huế", "Thừa Thiên Huế").Replace("Thành Phố Hồ Chí Minh", "Hồ Chí Minh");
                                    prov = prov.Replace("Qu?ng", "Quảng").Replace("B?c", "Bắc").Replace("C Mau", "Cà Mau")
                                               .Replace("D?k L?k", "Đắk Lắk").Replace("D?ng", "Đồng").Replace("H Tinh", "Hà Tĩnh")
                                               .Replace("Hung", "Hưng").Replace("Khnh", "Khánh").Replace("Lm", "Lâm")
                                               .Replace("Ninh", "Ninh").Replace("Ty", "Tây");
                                }

                                if (string.IsNullOrEmpty(prov)) prov = "Cần Thơ";

                                var rawDist = rb.District ?? "";
                                var cleanDist = GetCleanWard(rb.Address, rawDist, prov);

                                branches.Add(new Branch
                                {
                                    Id = id++,
                                    Name = rb.Name ?? $"BHX Chi nhánh {id}",
                                    Address = rb.Address ?? "",
                                    Province = prov,
                                    District = cleanDist
                                });
                            }
                        }
                    }
                    catch { }
                }

                if (!branches.Any())
                {
                    branches.Add(new Branch { Id = 1, Name = "BHX 151/6 Trần Hoàng Na", Address = "151/6 Trần Hoàng Na, Phường Hưng Lợi, Quận Ninh Kiều, Thành phố Cần Thơ, Việt Nam", Province = "Cần Thơ", District = "Phường Hưng Lợi" });
                    branches.Add(new Branch { Id = 2, Name = "BHX 3G Nguyễn Văn Linh", Address = "3G Nguyễn Văn Linh, Phường Hưng Lợi, Quận Ninh Kiều, Thành phố Cần Thơ, Việt Nam", Province = "Cần Thơ", District = "Phường Hưng Lợi" });
                    branches.Add(new Branch { Id = 3, Name = "BHX 220 Cách Mạng Tháng 8", Address = "220 Cách Mạng Tháng 8, Phường Bùi Hữu Nghĩa, Quận Bình Thủy, Thành phố Cần Thơ, Việt Nam", Province = "Cần Thơ", District = "Phường Bùi Hữu Nghĩa" });
                }

                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Branches ON");
                        context.Branches.AddRange(branches);
                        context.SaveChanges();
                        context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Branches OFF");
                        transaction.Commit();
                    }
                    catch { transaction.Rollback(); }
                }
            }

            // Xóa inventory cũ nếu partial seed (< 50% tổng expected)
            int productCount = context.Products.Count();
            int branchCount  = context.Branches.Count();
            int invCount     = context.ProductInventories.Count();
            int expectedMin  = (productCount * branchCount) / 2; // ngưỡng 50%
            if (invCount > 0 && invCount < expectedMin)
            {
                context.Database.SetCommandTimeout(120);
                context.Database.ExecuteSqlRaw("DELETE FROM ProductInventories;");
                context.Database.SetCommandTimeout(30);
                context.ChangeTracker.Clear();
            }

            if (!context.ProductInventories.Any())
            {
                context.ChangeTracker.AutoDetectChangesEnabled = false;
                var products = context.Products.ToList();
                var branches = context.Branches.ToList();
                var rand = new Random(42); // seed cố định để tái tạo được

                // ── 1. Cập nhật Unit & IsSoldByWeight cho từng sản phẩm ──────────────
                foreach (var p in products)
                {
                    if (p.CategoryId == 1) // Thịt, cá, trứng
                    {
                        if (p.Name.Contains("khay") || p.Name.Contains("hộp") || p.Name.Contains("vỉ") || p.Name.Contains("quả"))
                        {
                            p.Unit = p.Name.Contains("khay") ? "Khay" : (p.Name.Contains("hộp") ? "Hộp" : (p.Name.Contains("vỉ") ? "Vỉ" : "Quả"));
                            p.IsSoldByWeight = false;
                        }
                        else { p.Unit = "kg"; p.IsSoldByWeight = true; }
                    }
                    else if (p.CategoryId == 2) // Rau, củ, trái cây
                    {
                        if (p.Name.Contains("túi") || p.Name.Contains("khay") || p.Name.Contains("hộp") || p.Name.Contains("Trái") || p.Name.Contains("trái"))
                        {
                            p.Unit = p.Name.Contains("túi") ? "Túi" : (p.Name.Contains("khay") ? "Khay" : (p.Name.Contains("hộp") ? "Hộp" : "Trái"));
                            p.IsSoldByWeight = false;
                        }
                        else { p.Unit = "kg"; p.IsSoldByWeight = true; }
                    }
                    else
                    {
                        p.Unit = p.Name.Contains("Thùng") ? "Thùng" : (p.Name.Contains("lon") ? "Lon" : (p.Name.Contains("chai") ? "Chai" : (p.Name.Contains("hộp") ? "Hộp" : "Cái")));
                        p.IsSoldByWeight = false;
                    }
                }
                context.SaveChanges(); // Cập nhật Unit & IsSoldByWeight cho Product

                // ── 2. Tạo dữ liệu tồn kho ──────────────────────────────────────────
                var rows = new List<(int productId, int branchId, decimal qty)>(products.Count * branches.Count);
                foreach (var p in products)
                {
                    foreach (var b in branches)
                    {
                        decimal baseStock = p.Id % 3 == 0 ? 0 : rand.Next(15, 100);
                        if (p.IsSoldByWeight)
                            baseStock = baseStock + (decimal)Math.Round(rand.NextDouble(), 2);
                        rows.Add((p.Id, b.Id, baseStock));
                    }
                }

                // ── 3. Bulk insert bằng raw SQL (nhanh hơn EF Core ~15 lần) ─────────
                // Dùng batch 2000 rows × SQL VALUES để giảm số round-trips
                const int sqlBatchSize = 1000; // SQL Server giới hạn tối đa 1000 rows/INSERT
                context.Database.SetCommandTimeout(300); // 5 phút cho toàn bộ seed
                for (int i = 0; i < rows.Count; i += sqlBatchSize)
                {
                    var batch = rows.Skip(i).Take(sqlBatchSize);
                    var valuesClauses = string.Join(",\n", batch.Select(r =>
                        $"({r.productId}, {r.branchId}, {r.qty.ToString(System.Globalization.CultureInfo.InvariantCulture)})"));
                    var sql = $"INSERT INTO ProductInventories (ProductId, BranchId, Quantity) VALUES\n{valuesClauses};";
                    context.Database.ExecuteSqlRaw(sql);
                }
                context.Database.SetCommandTimeout(30); // khôi phục timeout mặc định

                context.ChangeTracker.AutoDetectChangesEnabled = true;
            }
        }

        private static string CleanDistrict(string rawDistrict, string address, string province)
        {
            if (string.IsNullOrEmpty(rawDistrict)) return "Khác";
            string dist = rawDistrict.Trim();
            string addr = address ?? "";

            // Normalize corrupted characters in raw district
            dist = dist.Replace("Qu?n", "Quận")
                       .Replace("Qun", "Quận")
                       .Replace("Huy?n", "Huyện")
                       .Replace("Phu?ng", "Phường")
                       .Replace("Th?", "Thị")
                       .Replace("Xa", "Xã")
                       .Replace("Di An", "Dĩ An")
                       .Replace("Thu?n An", "Thuận An")
                       .Replace("Tn Uyn", "Tân Uyên")
                       .Replace("B?n Ct", "Bến Cát")
                       .Replace("Ph Gio", "Phú Giáo")
                       .Replace("D?u Ti?ng", "Dầu Tiếng")
                       .Replace("Bu Bng", "Bàu Bàng");

            if (province == "Hồ Chí Minh")
            {
                // First try to check address text for standard HCMC districts
                if (addr.Contains("Thủ Đức", StringComparison.OrdinalIgnoreCase) || 
                    addr.Contains("Quận 2", StringComparison.OrdinalIgnoreCase) || 
                    addr.Contains("Quận 9", StringComparison.OrdinalIgnoreCase))
                {
                    return "Thành phố Thủ Đức";
                }
                if (addr.Contains("Bình Tân", StringComparison.OrdinalIgnoreCase)) return "Quận Bình Tân";
                if (addr.Contains("Tân Phú", StringComparison.OrdinalIgnoreCase)) return "Quận Tân Phú";
                if (addr.Contains("Bình Thạnh", StringComparison.OrdinalIgnoreCase)) return "Quận Bình Thạnh";
                if (addr.Contains("Gò Vấp", StringComparison.OrdinalIgnoreCase)) return "Quận Gò Vấp";
                if (addr.Contains("Phú Nhuận", StringComparison.OrdinalIgnoreCase)) return "Quận Phú Nhuận";
                if (addr.Contains("Tân Bình", StringComparison.OrdinalIgnoreCase)) return "Quận Tân Bình";
                if (addr.Contains("Bình Chánh", StringComparison.OrdinalIgnoreCase)) return "Huyện Bình Chánh";
                if (addr.Contains("Hóc Môn", StringComparison.OrdinalIgnoreCase)) return "Huyện Hóc Môn";
                if (addr.Contains("Củ Chi", StringComparison.OrdinalIgnoreCase)) return "Huyện Củ Chi";
                if (addr.Contains("Nhà Bè", StringComparison.OrdinalIgnoreCase)) return "Huyện Nhà Bè";
                if (addr.Contains("Cần Giờ", StringComparison.OrdinalIgnoreCase)) return "Huyện Cần Giờ";
                if (addr.Contains("Quận 12", StringComparison.OrdinalIgnoreCase)) return "Quận 12";
                if (addr.Contains("Quận 10", StringComparison.OrdinalIgnoreCase)) return "Quận 10";
                if (addr.Contains("Quận 11", StringComparison.OrdinalIgnoreCase)) return "Quận 11";
                if (addr.Contains("Quận 1", StringComparison.OrdinalIgnoreCase) || addr.Contains("Quận 01", StringComparison.OrdinalIgnoreCase)) return "Quận 1";
                if (addr.Contains("Quận 3", StringComparison.OrdinalIgnoreCase) || addr.Contains("Quận 03", StringComparison.OrdinalIgnoreCase)) return "Quận 3";
                if (addr.Contains("Quận 4", StringComparison.OrdinalIgnoreCase) || addr.Contains("Quận 04", StringComparison.OrdinalIgnoreCase)) return "Quận 4";
                if (addr.Contains("Quận 5", StringComparison.OrdinalIgnoreCase) || addr.Contains("Quận 05", StringComparison.OrdinalIgnoreCase)) return "Quận 5";
                if (addr.Contains("Quận 6", StringComparison.OrdinalIgnoreCase) || addr.Contains("Quận 06", StringComparison.OrdinalIgnoreCase)) return "Quận 6";
                if (addr.Contains("Quận 7", StringComparison.OrdinalIgnoreCase) || addr.Contains("Quận 07", StringComparison.OrdinalIgnoreCase)) return "Quận 7";
                if (addr.Contains("Quận 8", StringComparison.OrdinalIgnoreCase) || addr.Contains("Quận 08", StringComparison.OrdinalIgnoreCase)) return "Quận 8";

                // Map clean names or old districts
                if (dist == "Quận 01" || dist == "Quận 1") return "Quận 1";
                if (dist == "Quận 02" || dist == "Quận 2") return "Thành phố Thủ Đức";
                if (dist == "Quận 03" || dist == "Quận 3") return "Quận 3";
                if (dist == "Quận 04" || dist == "Quận 4") return "Quận 4";
                if (dist == "Quận 05" || dist == "Quận 5") return "Quận 5";
                if (dist == "Quận 06" || dist == "Quận 6") return "Quận 6";
                if (dist == "Quận 07" || dist == "Quận 7") return "Quận 7";
                if (dist == "Quận 08" || dist == "Quận 8") return "Quận 8";
                if (dist == "Quận 09" || dist == "Quận 9") return "Thành phố Thủ Đức";
                if (dist == "Quận Thủ Đức" || dist == "Thành phố Thủ Đức" || dist.Contains("Th? d?c")) return "Thành phố Thủ Đức";

                // Ward mapping fallbacks
                string distLower = dist.ToLower();
                if (distLower.Contains("linh xuân") || distLower.Contains("tam bình") || distLower.Contains("long bình") || 
                    distLower.Contains("phước long") || distLower.Contains("tăng nhơn phú") || distLower.Contains("cát lái") || 
                    distLower.Contains("hiệp bình") || distLower.Contains("long trường") || distLower.Contains("an khánh") || 
                    distLower.Contains("bình trung") || distLower.Contains("linh đông") || distLower.Contains("tam phú") || 
                    distLower.Contains("trường thạnh") || distLower.Contains("hiệp phú") || distLower.Contains("thảo điền") || 
                    distLower.Contains("long thạnh mỹ"))
                {
                    return "Thành phố Thủ Đức";
                }
                if (distLower.Contains("đông hưng thuận") || distLower.Contains("tân thới hiệp") || distLower.Contains("thới an") || 
                    distLower.Contains("an phú đông") || distLower.Contains("trung mỹ tây") || distLower.Contains("vườn lài") ||
                    distLower.Contains("thạnh lộc") || distLower.Contains("thạnh xuân") || distLower.Contains("tân chánh hiệp"))
                {
                    return "Quận 12";
                }
                if (distLower.Contains("tân tạo") || distLower.Contains("bình trị đông") || distLower.Contains("an lạc") || 
                    distLower.Contains("bình hưng hòa"))
                {
                    return "Quận Bình Tân";
                }
                if (distLower.Contains("phú thọ hòa") || distLower.Contains("tây thạnh") || distLower.Contains("tân sơn nhì") || 
                    distLower.Contains("tân quý") || distLower.Contains("tân thành"))
                {
                    return "Quận Tân Phú";
                }
                if (distLower.Contains("thông tây hội") || distLower.Contains("thạnh mỹ tây") || distLower.Contains("gò vấp") || 
                    distLower.Contains("phường 1") || distLower.Contains("phường 11"))
                {
                    return "Quận Gò Vấp";
                }
                if (distLower.Contains("chánh hưng") || distLower.Contains("phú định") || distLower.Contains("rạch ông"))
                {
                    return "Quận 8";
                }
                if (distLower.Contains("chợ quán") || distLower.Contains("cầu ông lãnh") || distLower.Contains("bến nghé"))
                {
                    return "Quận 1";
                }
                if (distLower.Contains("phú nhuận")) return "Quận Phú Nhuận";
                if (distLower.Contains("bình thạnh")) return "Quận Bình Thạnh";
                if (distLower.Contains("tân bình")) return "Quận Tân Bình";
                if (distLower.Contains("bình chánh")) return "Huyện Bình Chánh";
                if (distLower.Contains("hóc môn")) return "Huyện Hóc Môn";
                if (distLower.Contains("củ chi")) return "Huyện Củ Chi";
                if (distLower.Contains("nhà bè")) return "Huyện Nhà Bè";
                if (distLower.Contains("cần giờ")) return "Huyện Cần Giờ";
            }

            // Clean other provinces' districts
            if (province == "Bình Dương")
            {
                if (dist.Contains("Thuận An")) return "Thành phố Thuận An";
                if (dist.Contains("Dĩ An")) return "Thành phố Dĩ An";
                if (dist.Contains("Thủ Dầu Một")) return "Thành phố Thủ Dầu Một";
                if (dist.Contains("Tân Uyên")) return "Thành phố Tân Uyên";
                if (dist.Contains("Bến Cát")) return "Thành phố Bến Cát";
                if (dist.Contains("Phú Giáo")) return "Huyện Phú Giáo";
                if (dist.Contains("Dầu Tiếng")) return "Huyện Dầu Tiếng";
                if (dist.Contains("Bàu Bàng")) return "Huyện Bàu Bàng";
                if (dist.Contains("Bắc Tân Uyên")) return "Huyện Bắc Tân Uyên";
            }

            if (province == "Cần Thơ")
            {
                if (dist.Contains("Ninh Kiều")) return "Quận Ninh Kiều";
                if (dist.Contains("Bình Thủy")) return "Quận Bình Thủy";
                if (dist.Contains("Cái Răng")) return "Quận Cái Răng";
                if (dist.Contains("Ô Môn")) return "Quận Ô Môn";
                if (dist.Contains("Thốt Nốt")) return "Quận Thốt Nốt";
                if (dist.Contains("Phong Điền")) return "Huyện Phong Điền";
                if (dist.Contains("Thới Lai")) return "Huyện Thới Lai";
                if (dist.Contains("Cờ Đỏ")) return "Huyện Cờ Đỏ";
                if (dist.Contains("Vĩnh Thạnh")) return "Huyện Vĩnh Thạnh";
            }

            if (province == "Đà Nẵng")
            {
                if (dist.Contains("Hải Châu")) return "Quận Hải Châu";
                if (dist.Contains("Thanh Khê")) return "Quận Thanh Khê";
                if (dist.Contains("Sơn Trà")) return "Quận Sơn Trà";
                if (dist.Contains("Ngũ Hành Sơn")) return "Quận Ngũ Hành Sơn";
                if (dist.Contains("Liên Chiểu")) return "Quận Liên Chiểu";
                if (dist.Contains("Cẩm Lệ")) return "Quận Cẩm Lệ";
                if (dist.Contains("Hòa Vang")) return "Huyện Hòa Vang";
            }

            // General capitalization and prefixing
            if (!dist.StartsWith("Quận") && !dist.StartsWith("Huyện") && !dist.StartsWith("Thành phố") && !dist.StartsWith("Thị xã") && dist != "Khác")
            {
                if (dist.Contains("Quận")) dist = "Quận " + dist.Replace("Quận", "").Trim();
                else if (dist.Contains("Huyện")) dist = "Huyện " + dist.Replace("Huyện", "").Trim();
            }

            return dist;
        }

        private class RawBranchJson
        {
            public string Name { get; set; }
            public string Address { get; set; }
            public string Province { get; set; }
            public string District { get; set; }
        }

        private static readonly List<WardMapEntry> WardMappings = new List<WardMapEntry>
        {
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Phường Bến Nghé, một phần phường Đa Kao và Nguyễn Thái Bình", NewWard = "Phường Sài Gòn" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Phường Tân Định và một phần phường Đa Kao", NewWard = "Phường Tân Định" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Bến Thành, Phạm Ngũ Lão, một phần phường Cầu Ông Lãnh và Nguyễn Thái Bình", NewWard = "Phường Bến Thành" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Nguyễn Cư Trinh, Cầu Kho, Cô Giang, một phần phường Cầu Ông Lãnh", NewWard = "Phường Cầu Ông Lãnh" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 1, 2, 3, 5, một phần phường 4 (Quận 3)", NewWard = "Phường Bàn Cờ" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Võ Thị Sáu, một phần phường 4 (Quận 3)", NewWard = "Phường Xuân Hòa" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 9, 11, 12, 14 (Quận 3)", NewWard = "Phường Nhiêu Lộc" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 13, 16, 18, một phần phường 15 (Quận 4)", NewWard = "Phường Xóm Chiếu" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 8, 9, một phần phường 2, 4 và 15 (Quận 4)", NewWard = "Phường Khánh Hội" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 1, 3, một phần phường 2 và 4 (Quận 4)", NewWard = "Phường Vĩnh Hội" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 1, 2, 4 (Quận 5)", NewWard = "Phường Chợ Quán" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 5, 7, 9 (Quận 5)", NewWard = "Phường An Đông" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 11, 12, 13, 14 (Quận 5)", NewWard = "Phường Chợ Lớn" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 2, 9 (Quận 6)", NewWard = "Phường Bình Tây" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 1, 7, 8 (Quận 6)", NewWard = "Phường Bình Tiên" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 10, 11 (Quận 6), một phần phường 16 (Quận 8)", NewWard = "Phường Bình Phú" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 12, 13, 14 (Quận 6)", NewWard = "Phường Phú Lâm" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Bình Thuận, Tân Thuận Đông, Tân Thuận Tây", NewWard = "Phường Tân Thuận" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Phường Phú Thuận và một phần phường Phú Mỹ (Quận 7)", NewWard = "Phường Phú Thuận" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Tân Phú và một phần phường Phú Mỹ (Quận 7)", NewWard = "Phường Tân Mỹ" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Tân Phong, Tân Quy, Tân Kiểng, Tân Hưng", NewWard = "Phường Tân Hưng" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 4 (Quận 8), Rạch Ông, Hưng Phú và một phần phường 5 (Quận 8)", NewWard = "Phường Chánh Hưng" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 14, 15, Xóm Củi và một phần phường 16 (Quận 8)", NewWard = "Phường Phú Định" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Phường 6, một phần phường 5 và 7 (Quận 8), xã An Phú Tây (Huyện Bình Chánh)", NewWard = "Phường Bình Đông" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 6, 8, một phần phường 14 (Quận 10)", NewWard = "Phường Diên Hồng" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 1, 2, 4, 9, 10 (Quận 10)", NewWard = "Phường Vườn Lài" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 12, 13, 15, một phần phường 14 (Quận 10)", NewWard = "Phường Hòa Hưng" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 1, 7, 16 (Quận 11)", NewWard = "Phường Minh Phụng" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 3, 10, một phần phường 8 (Quận 11)", NewWard = "Phường Bình Thới" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 5, 14 (Quận 11)", NewWard = "Phường Hòa Bình" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 11, 15, một phần phường 8 (Quận 11)", NewWard = "Phường Phú Thọ" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Tân Thới Nhất, Tân Hưng Thuận, Đông Hưng Thuận", NewWard = "Phường Đông Hưng Thuận" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Tân Chánh Hiệp, Trung Mỹ Tây", NewWard = "Phường Trung Mỹ Tây" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Hiệp Thành (Quận 12), Tân Thới Hiệp", NewWard = "Phường Tân Thới Hiệp" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Thạnh Xuân, Thới An", NewWard = "Phường Thới An" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Thạnh Lộc, An Phú Đông", NewWard = "Phường An Phú Đông" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Bình Trị Đông B, An Lạc A, An Lạc", NewWard = "Phường An Lạc" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Bình Hưng Hòa B, một phần phường Bình Trị Đông A và Tân Tạo", NewWard = "Phường Bình Tân" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Xã Tân Kiên, một phần phường Tân Tạo A và Tân Tạo", NewWard = "Phường Tân Tạo" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Bình Trị Đông, một phần phường Bình Hưng Hòa A và Bình Trị Đông A", NewWard = "Phường Bình Trị Đông" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Bình Hưng Hòa, một phần phường Sơn Kỳ và Bình Hưng Hòa A", NewWard = "Phường Bình Hưng Hòa" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 1, 2, 7, 17 (quận Bình Thạnh)", NewWard = "Phường Gia Định" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 12, 14, 26 (quận Bình Thạnh)", NewWard = "Phường Bình Thạnh" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 5, 11, 13 (quận Bình Thạnh)", NewWard = "Phường Bình Lợi Trung" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 19, 22, 25", NewWard = "Phường Thạnh Mỹ Tây" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 27, 28", NewWard = "Phường Bình Quới" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 1, 3 (quận Gò Vấp)", NewWard = "Phường Hạnh Thông" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 5, 6 (quận Gò Vấp)", NewWard = "Phường An Nhơn" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 10, 17 (quận Gò Vấp)", NewWard = "Phường Gò Vấp" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 15, 16 (quận Gò Vấp)", NewWard = "Phường An Hội Đông" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 8, 11 (quận Gò Vấp)", NewWard = "Phường Thông Tây Hội" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 12, 14 (quận Gò Vấp)", NewWard = "Phường An Hội Tây" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 4, 5, 9 (quận Phú Nhuận)", NewWard = "Phường Đức Nhuận" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 1, 2, 7, một phần phường 15 (quận Phú Nhuận)", NewWard = "Phường Cầu Kiệu" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 8, 10, 11, 13, một phần phường 15 (quận Phú Nhuận)", NewWard = "Phường Phú Nhuận" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 1, 2, 3 (quận Tân Bình)", NewWard = "Phường Tân Sơn Hòa" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 4, 5, 7 (quận Tân Bình)", NewWard = "Phường Tân Sơn Nhất" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 6, 8, 9 (quận Tân Bình)", NewWard = "Phường Tân Hòa" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 10, 11, 12 (quận Tân Bình)", NewWard = "Phường Bảy Hiền" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường 13, 14, một phần phường 15 (quận Tân Bình)", NewWard = "Phường Tân Bình" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Phần còn lại phường 15 (quận Tân Bình)", NewWard = "Phường Tân Sơn" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Tây Thạnh, một phần phường Sơn Kỳ", NewWard = "Phường Tây Thạnh" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Tân Sơn Nhì, Sơn Kỳ, một phần phường Tân Quý và Tân Thành", NewWard = "Phường Tân Sơn Nhì" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Phú Thọ Hòa, một phần phường Tân Thành và Tân Quý", NewWard = "Phường Phú Thọ Hòa" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Phú Trung, Hòa Thạnh, một phần phường Tân Thới Hòa và Tân Thành", NewWard = "Phường Tân Phú" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Hiệp Tân, Phú Thạnh, một phần phường Tân Thới Hòa", NewWard = "Phường Phú Thạnh" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Hiệp Bình Chánh, Hiệp Bình Phước, một phần phường Linh Đông", NewWard = "Phường Hiệp Bình" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Bình Thọ, Linh Chiểu, Trường Thọ, một phần phường Linh Tây và Linh Đông", NewWard = "Phường Thủ Đức" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Bình Chiểu, Tam Phú, Tam Bình", NewWard = "Phường Tam Bình" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Linh Trung, Linh Xuân, một phần phường Linh Tây", NewWard = "Phường Linh Xuân" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Tân Phú (thành phố Thủ Đức), Hiệp Phú, Tăng Nhơn Phú A, Tăng Nhơn Phú B, một phần phường Long Thạnh Mỹ", NewWard = "Phường Tăng Nhơn Phú" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Long Bình, một phần phường Long Thạnh Mỹ", NewWard = "Phường Long Bình" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Trường Thạnh, Long Phước", NewWard = "Phường Long Phước" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Phú Hữu, Long Trường", NewWard = "Phường Long Trường" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Thạnh Mỹ Lợi, Cát Lái", NewWard = "Phường Cát Lái" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Bình Trưng Đông, Bình Trưng Tây, một phần phường An Phú (thành phố Thủ Đức)", NewWard = "Phường Bình Trưng" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Phước Bình, Phước Long A, Phước Long B", NewWard = "Phường Phước Long" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các phường Thủ Thiêm, An Lợi Đông, Thảo Điền, An Khánh, một phần phường An Phú (thành phố Thủ Đức)", NewWard = "Phường An Khánh" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các phường Bình An, Bình Thắng, Đông Hòa", NewWard = "Phường Đông Hòa" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các phường An Bình, Dĩ An, một phần phường Tân Đông Hiệp", NewWard = "Phường Dĩ An" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các phường Tân Bình, một phần phường Thái Hòa và Tân Đông Hiệp", NewWard = "Phường Tân Đông Hiệp" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các phường An Phú (thành phố Thuận An), một phần phường Bình Chuẩn", NewWard = "Phường An Phú" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Phường Bình Hòa và một phần phường Vĩnh Phú", NewWard = "Phường Bình Hòa" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các phường Bình Nhâm, Lái Thiêu, một phần phường Vĩnh Phú", NewWard = "Phường Lái Thiêu" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các phường Hưng Định, An Thạnh, Xã An Sơn", NewWard = "Phường Thuận An" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các phường Thuận Giao, Bình Chuẩn", NewWard = "Phường Thuận Giao" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các phường Phú Cường, Phú Thọ, Chánh Nghĩa, một phần phường Hiệp Thành (thành phố Thủ Dầu Một), Chánh Mỹ", NewWard = "Phường Thủ Dầu Một" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các phường Phú Hòa, Phú Lợi, một phần phường Hiệp Thành (thành phố Thủ Dầu Một)", NewWard = "Phường Phú Lợi" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các phường Định Hòa, Tương Bình Hiệp, một phần phường Hiệp An và Chánh Mỹ", NewWard = "Phường Chánh Hiệp" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các phường Phú Mỹ (thành phố Thủ Dầu Một), Hòa Phú, Phú Tân, Phú Chánh", NewWard = "Phường Bình Dương" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các phường Tân Định (thành phố Bến Cát), Hòa Lợi", NewWard = "Phường Hòa Lợi" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các phường Tân An, Xã Phú An, Hiệp An", NewWard = "Phường Phú An" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Phường An Tây, một phần xã Thanh Tuyền và xã An Lập", NewWard = "Phường Tây Nam" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Phường An Điền, xã Long Nguyên, một phần phường Mỹ Phước", NewWard = "Phường Long Nguyên" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Xã Tân Hưng (huyện Bàu Bàng), xã Lai Hưng, một phần phường Mỹ Phước", NewWard = "Phường Bến Cát" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Phường Chánh Phú Hòa, Xã Hưng Hòa", NewWard = "Phường Chánh Phú Hòa" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Phường Vĩnh Tân, Thị trấn Tân Bình", NewWard = "Phường Vĩnh Tân" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Xã Bình Mỹ (huyện Bắc Tân Uyên), Phường Hội Nghĩa", NewWard = "Phường Bình Cơ" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Phường Uyên Hưng, Xã Bạch Đằng, Xã Tân Lập, một phần xã Tân Mỹ", NewWard = "Phường Tân Uyên" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các phường Khánh Bình, Tân Hiệp", NewWard = "Phường Tân Hiệp" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các phường Thạnh Phước, Tân Phước Khánh, Tân Vĩnh Hiệp, một phần phường Thái Hòa và xã Thạnh Hội", NewWard = "Phường Tân Khánh" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Các phường 1, 2, 3, 4, 5 (thành phố Vũng Tàu), Thắng Nhì, Thắng Tam", NewWard = "Phường Vũng Tàu" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Các phường 7, 8, 9 (thành phố Vũng Tàu), Nguyễn An Ninh", NewWard = "Phường Tam Thắng" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Các phường 10 (thành phố Vũng Tàu), Thắng Nhất, Rạch Dừa", NewWard = "Phường Rạch Dừa" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Các phường 11, 12 (thành phố Vũng Tàu)", NewWard = "Phường Phước Thắng" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Xã Tân Hưng (thành phố Bà Rịa), Kim Dinh, Long Hương", NewWard = "Phường Long Hương" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Các phường Phước Trung, Phước Nguyên, Long Toàn, Phước Hưng", NewWard = "Phường Bà Rịa" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Các phường Long Tâm, Xã Hòa Long, Xã Long Phước", NewWard = "Phường Tam Long" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Các phường Tân Hòa, Tân Hải", NewWard = "Phường Tân Hải" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Các phường Phước Hòa, Tân Phước", NewWard = "Phường Tân Phước" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Các phường Phú Mỹ (thành phố Phú Mỹ), Mỹ Xuân", NewWard = "Phường Phú Mỹ" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Các phường Hắc Dịch, Xã Sông Xoài", NewWard = "Phường Tân Thành" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Xã Vĩnh Lộc A và một phần xã Phạm Văn Hai", NewWard = "Xã Vĩnh Lộc" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các xã Vĩnh Lộc B, một phần xã Phạm Văn Hai và một phần phường Tân Tạo", NewWard = "Xã Tân Vĩnh Lộc" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các xã Lê Minh Xuân, Bình Lợi", NewWard = "Xã Bình Lợi" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Thị trấn Tân Túc, Xã Tân Nhựt, một phần phường Tân Tạo A, xã Tân Kiên và phường 16 (Quận 8)", NewWard = "Xã Tân Nhựt" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các xã Tân Quý Tây, Bình Chánh, An Phú Tây", NewWard = "Xã Bình Chánh" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các xã Đa Phước, Qui Đức, Hưng Long", NewWard = "Xã Hưng Long" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các xã Phong Phú, xã Bình Hưng, một phần phường 7 (Quận 8)", NewWard = "Xã Bình Hưng" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các xã Tam Thôn Hiệp, Bình Khánh, một phần xã An Thới Đông", NewWard = "Xã Bình Khánh" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Xã Lý Nhơn và một phần xã An Thới Đông", NewWard = "Xã An Thới Đông" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Xã Long Hòa (huyện Cần Giờ), Thị trấn Cần Thạnh", NewWard = "Xã Cần Giờ" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các xã Tân Phú Trung, Tân Thông Hội, Phước Vĩnh An", NewWard = "Xã Củ Chi" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Thị trấn Củ Chi, Xã Phước Hiệp, Xã Tân An Hội", NewWard = "Xã Tân An Hội" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các xã Trung Lập Thượng, Phước Thạnh, Thái Mỹ", NewWard = "Xã Thái Mỹ" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các xã Phú Mỹ Hưng, An Phú, An Nhơn Tây", NewWard = "Xã An Nhơn Tây" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các xã Phạm Văn Cội, Trung Lập Hạ, Nhuận Đức", NewWard = "Xã Nhuận Đức" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các xã Tân Thạnh Tây, Tân Thạnh Đông, Phú Hòa Đông", NewWard = "Xã Phú Hòa Đông" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các xã Bình Mỹ (huyện Củ Chi), Hòa Phú, Trung An", NewWard = "Xã Bình Mỹ" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các xã Thới Tam Thôn, Nhị Bình, Đông Thạnh", NewWard = "Xã Đông Thạnh" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các xã Tân Hiệp (huyện Hóc Môn), Xã Tân Xuân, Thị trấn Hóc Môn", NewWard = "Xã Hóc Môn" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các xã Tân Thới Nhì, Xuân Thới Đông, Xuân Thới Sơn", NewWard = "Xã Xuân Thới Sơn" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các xã Xuân Thới Thượng, Trung Chánh, Bà Điểm", NewWard = "Xã Bà Điểm" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Thị trấn Nhà Bè, Xã Phú Xuân, Xã Phước Kiển, Xã Phước Lộc", NewWard = "Xã Nhà Bè" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Các xã Nhơn Đức, Long Thới, Hiệp Phước", NewWard = "Xã Hiệp Phước" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các xã Lạc An, Hiếu Liêm, Thường Tân, một phần xã Tân Mỹ", NewWard = "Xã Thường Tân" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Thị trấn Tân Thành, Xã Đất Cuốc, Xã Tân Định", NewWard = "Xã Bắc Tân Uyên" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Thị trấn Phước Vĩnh, xã An Bình, một phần xã Tam Lập", NewWard = "Xã Phú Giáo" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các xã Vĩnh Hòa, Phước Hòa, một phần xã Tam Lập", NewWard = "Xã Phước Hòa" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các xã Tân Hiệp (huyện Phú Giáo), An Thái, Phước Sang", NewWard = "Xã Phước Thành" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các xã An Linh, Tân Long, An Long", NewWard = "Xã An Long" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Xã Trừ Văn Thố, xã Cây Trường II, một phần thị trấn Lai Uyên", NewWard = "Xã Trừ Văn Thố" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Phần còn lại thị trấn Lai Uyên", NewWard = "Xã Bàu Bàng" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các xã Long Tân, Long Hòa (huyện Dầu Tiếng), một phần xã Minh Tân và Minh Thạnh", NewWard = "Xã Long Hòa" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Các xã Thanh An, một phần xã Định Hiệp, Thanh Tuyền và An Lập", NewWard = "Xã Thanh An" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Thị trấn Dầu Tiếng, xã Định An, xã Định Thành và một phần xã Định Hiệp", NewWard = "Xã Dầu Tiếng" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Xã Minh Hòa, một phần xã Minh Tân và Minh Thạnh", NewWard = "Xã Minh Thạnh" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Các xã Tóc Tiên và Châu Pha", NewWard = "Xã Châu Pha" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Thị trấn Long Hải, xã Phước Tỉnh và xã Phước Hưng", NewWard = "Xã Long Hải" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Thị trấn Long Điền, Xã Tam An", NewWard = "Xã Long Điền" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Thị trấn Phước Hải, Xã Phước Hội", NewWard = "Xã Phước Hải" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Thị trấn Đất Đỏ, Xã Long Tân (huyện Long Đất), Xã Láng Dài, Xã Phước Long Thọ", NewWard = "Xã Đất Đỏ" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Các xã Đá Bạc, Nghĩa Thành", NewWard = "Xã Nghĩa Thành" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Thị trấn Ngãi Giao, Xã Bình Ba, Xã Suối Nghệ", NewWard = "Xã Ngãi Giao" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Thị trấn Kim Long, Xã Bàu Chinh, Xã Láng Lớn", NewWard = "Xã Kim Long" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Các xã Cù Bị, Xà Bang", NewWard = "Xã Châu Đức" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Các xã Bình Trung, Quảng Thành, Bình Giã", NewWard = "Xã Bình Giã" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Các xã Suối Rao, Xã Sơn Bình, Xã Xuân Sơn", NewWard = "Xã Xuân Sơn" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Thị trấn Phước Bửu, Xã Phước Tân, Xã Phước Thuận", NewWard = "Xã Hồ Tràm" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Các xã Bông Trang, Xã Bưng Riềng, Xã Xuyên Mộc", NewWard = "Xã Xuyên Mộc" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Các xã Hòa Hưng, Hòa Bình, Hòa Hội", NewWard = "Xã Hòa Hội" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Các xã Tân Lâm, Bàu Lâm", NewWard = "Xã Bàu Lâm" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Huyện Côn Đảo", NewWard = "Đặc khu Côn Đảo" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Không sáp nhập", NewWard = "Xã Bình Châu" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Không sáp nhập", NewWard = "Xã Hòa Hiệp" },
            new WardMapEntry { Province = "Bà Rịa - Vũng Tàu", PriorWards = "Không sáp nhập", NewWard = "Xã Long Sơn" },
            new WardMapEntry { Province = "TP.HCM", PriorWards = "Không sáp nhập", NewWard = "Xã Thạnh An" },
            new WardMapEntry { Province = "Bình Dương", PriorWards = "Không sáp nhập", NewWard = "Phường Thới Hòa" },
        };



        private class WardMapEntry
        {
            public string Province { get; set; }
            public string PriorWards { get; set; }
            public string NewWard { get; set; }
        }

        private static string GetCleanWard(string address, string district, string province)
        {
            if (string.IsNullOrEmpty(address)) return "Khác";

            // 1. Try to extract ward from address parts (comma-separated, right to left)
            string rawWard = "";
            var parts = address.Split(',');
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                var part = parts[i].Trim();
                var asciiPart = RemoveDiacritics(part).ToLower();
                if (asciiPart.StartsWith("phuong ") || asciiPart.StartsWith("xa ") || asciiPart.StartsWith("thi tran "))
                {
                    rawWard = part;
                    break;
                }
            }

            // Fallback to district if it matches Phường/Xã/Thị trấn
            if (string.IsNullOrEmpty(rawWard) && !string.IsNullOrEmpty(district))
            {
                var asciiDist = RemoveDiacritics(district).ToLower();
                if (asciiDist.Contains("phuong") || asciiDist.Contains("xa") || asciiDist.Contains("thi tran"))
                {
                    rawWard = district.Trim();
                }
            }

            if (string.IsNullOrEmpty(rawWard))
            {
                return district ?? "Khác";
            }

            // Remove parentheses (e.g. "Phường 15 (Quận 4)" -> "Phường 15")
            string cleanWard = System.Text.RegularExpressions.Regex.Replace(rawWard, @"\s*\(.+?\)\s*", "").Trim();
            
            // For HCMC, Binh Duong, Ba Ria - Vung Tau: map to official 168 wards
            string provAscii = RemoveDiacritics(province).ToLower();
            if (provAscii.Contains("ho chi minh") || provAscii.Contains("binh duong") || provAscii.Contains("ba ria") || provAscii.Contains("vung tau"))
            {
                string cleanWardAscii = RemoveDiacritics(cleanWard).ToLower();
                
                // Strip leading prefixes for fuzzy checking
                string strippedWardAscii = System.Text.RegularExpressions.Regex.Replace(cleanWardAscii, @"^(phuong|xa|thi tran)\s+", "");
                // Standardize leading numbers (e.g., "01" -> "1")
                strippedWardAscii = System.Text.RegularExpressions.Regex.Replace(strippedWardAscii, @"^0+(\d+)", "$1");

                foreach (var m in WardMappings)
                {
                    string mNewAscii = RemoveDiacritics(m.NewWard).ToLower();
                    string mNewAsciiStripped = System.Text.RegularExpressions.Regex.Replace(mNewAscii, @"^(phuong|xa|thi tran)\s+", "");
                    mNewAsciiStripped = System.Text.RegularExpressions.Regex.Replace(mNewAsciiStripped, @"^0+(\d+)", "$1");

                    // Direct match
                    if (cleanWardAscii == mNewAscii || strippedWardAscii == mNewAsciiStripped)
                    {
                        return m.NewWard;
                    }

                    // Match on prior wards
                    string mPriorAscii = RemoveDiacritics(m.PriorWards).ToLower();
                    mPriorAscii = System.Text.RegularExpressions.Regex.Replace(mPriorAscii, @"\b0+(\d+)", "$1");

                    if (mPriorAscii.Contains(cleanWardAscii) || 
                        mPriorAscii.Contains(strippedWardAscii) || 
                        System.Text.RegularExpressions.Regex.IsMatch(mPriorAscii, @"\b" + System.Text.RegularExpressions.Regex.Escape(strippedWardAscii) + @"\b"))
                    {
                        return m.NewWard;
                    }
                }
            }

            return cleanWard;
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    if (c == 'đ') stringBuilder.Append('d');
                    else if (c == 'Đ') stringBuilder.Append('D');
                    else stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

    }
}