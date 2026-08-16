let map = null;
let marker = null;
let selectedLatLng = null;
let selectedAddress = "";
let lastSearchQuery = "";
let searchLatLng = null; // Store the latlng of the searched location
let isStockVerified = false;

// Store branches list - Representative branches for each major province/city BHX operates in
// Sources: OSM Nominatim geocoding + verified coordinates
const storeBranches = [
    // === HỒ CHÍ MINH ===
    { name: "Bách Hóa Xanh - Nguyễn Giản Thanh (Trụ sở chính, Quận 10)", latlng: L.latLng(10.780110, 106.663520) },
    { name: "Bách Hóa Xanh - Trương Phước Phan (Bình Tân)", latlng: L.latLng(10.761502, 106.602525) },
    { name: "Bách Hóa Xanh - Dương Văn Dương (Tân Phú)", latlng: L.latLng(10.791550, 106.618950) },
    { name: "Bách Hóa Xanh - Điện Biên Phủ (Bình Thạnh)", latlng: L.latLng(10.800000, 106.710000) },
    { name: "Bách Hóa Xanh - Quang Trung (Gò Vấp)", latlng: L.latLng(10.825000, 106.660000) },
    { name: "Bách Hóa Xanh - Lũy Bán Bích (Tân Phú)", latlng: L.latLng(10.778000, 106.625000) },
    { name: "Bách Hóa Xanh - Nguyễn Thị Thập (Quận 7)", latlng: L.latLng(10.742000, 106.705000) },
    { name: "Bách Hóa Xanh - Võ Văn Ngân (Thủ Đức)", latlng: L.latLng(10.845000, 106.772000) },
    { name: "Bách Hóa Xanh - Nguyễn Ảnh Thủ (Quận 12)", latlng: L.latLng(10.875000, 106.615000) },
    { name: "Bách Hóa Xanh - Linh Xuân (Thủ Đức)", latlng: L.latLng(10.862000, 106.756000) },
    { name: "Bách Hóa Xanh - Bình Chánh", latlng: L.latLng(10.687000, 106.600000) },
    { name: "Bách Hóa Xanh - Hóc Môn", latlng: L.latLng(10.884000, 106.596000) },
    { name: "Bách Hóa Xanh - Củ Chi", latlng: L.latLng(11.002000, 106.498000) },
    { name: "Bách Hóa Xanh - Nhà Bè", latlng: L.latLng(10.696000, 106.742000) },
    { name: "Bách Hóa Xanh - Cần Giờ", latlng: L.latLng(10.422000, 106.952000) },

    // === BÌNH DƯƠNG ===
    { name: "Bách Hóa Xanh - Thủ Dầu Một (Bình Dương)", latlng: L.latLng(10.980000, 106.650000) },
    { name: "Bách Hóa Xanh - Dĩ An (Bình Dương)", latlng: L.latLng(10.900000, 106.762000) },
    { name: "Bách Hóa Xanh - Thuận An (Bình Dương)", latlng: L.latLng(10.870000, 106.700000) },
    { name: "Bách Hóa Xanh - Tân Uyên (Bình Dương)", latlng: L.latLng(11.060000, 106.810000) },
    { name: "Bách Hóa Xanh - Bến Cát (Bình Dương)", latlng: L.latLng(11.100000, 106.590000) },

    // === ĐỒNG NAI ===
    { name: "Bách Hóa Xanh - Biên Hòa (Đồng Nai)", latlng: L.latLng(10.861819, 106.755520) },
    { name: "Bách Hóa Xanh - Long Thành (Đồng Nai)", latlng: L.latLng(10.782000, 106.939000) },
    { name: "Bách Hóa Xanh - Nhơn Trạch (Đồng Nai)", latlng: L.latLng(10.733000, 106.888000) },
    { name: "Bách Hóa Xanh - Trảng Bom (Đồng Nai)", latlng: L.latLng(10.966000, 107.006000) },

    // === BÀ RỊA - VŨNG TÀU ===
    { name: "Bách Hóa Xanh - Vũng Tàu", latlng: L.latLng(10.344539, 107.081224) },
    { name: "Bách Hóa Xanh - Bà Rịa", latlng: L.latLng(10.499000, 107.167000) },
    { name: "Bách Hóa Xanh - Phú Mỹ (BRVT)", latlng: L.latLng(10.598000, 107.070000) },

    // === TÂY NINH ===
    { name: "Bách Hóa Xanh - Tây Ninh", latlng: L.latLng(11.351000, 106.099000) },
    { name: "Bách Hóa Xanh - Gò Dầu (Tây Ninh)", latlng: L.latLng(11.131000, 106.266000) },

    // === BÌNH PHƯỚC ===
    { name: "Bách Hóa Xanh - Đồng Xoài (Bình Phước)", latlng: L.latLng(11.535000, 106.885000) },
    { name: "Bách Hóa Xanh - Bình Long (Bình Phước)", latlng: L.latLng(11.640000, 106.610000) },

    // === LONG AN ===
    { name: "Bách Hóa Xanh - Tân An (Long An)", latlng: L.latLng(10.779685, 106.620350) },
    { name: "Bách Hóa Xanh - Bến Lức (Long An)", latlng: L.latLng(10.641000, 106.494000) },
    { name: "Bách Hóa Xanh - Đức Hòa (Long An)", latlng: L.latLng(10.823000, 106.406000) },
    { name: "Bách Hóa Xanh - Cần Đước (Long An)", latlng: L.latLng(10.575000, 106.613000) },

    // === TIỀN GIANG ===
    { name: "Bách Hóa Xanh - Mỹ Tho (Tiền Giang)", latlng: L.latLng(10.360000, 106.358000) },
    { name: "Bách Hóa Xanh - Cai Lậy (Tiền Giang)", latlng: L.latLng(10.477000, 106.125000) },
    { name: "Bách Hóa Xanh - Gò Công (Tiền Giang)", latlng: L.latLng(10.363000, 106.673000) },

    // === BẾN TRE ===
    { name: "Bách Hóa Xanh - Bến Tre", latlng: L.latLng(10.243000, 106.375000) },
    { name: "Bách Hóa Xanh - Châu Thành (Bến Tre)", latlng: L.latLng(10.204000, 106.413000) },

    // === VĨNH LONG ===
    { name: "Bách Hóa Xanh - Vĩnh Long", latlng: L.latLng(10.259393, 106.404800) },
    { name: "Bách Hóa Xanh - Bình Minh (Vĩnh Long)", latlng: L.latLng(10.055000, 106.002000) },

    // === ĐỒNG THÁP ===
    { name: "Bách Hóa Xanh - Cao Lãnh (Đồng Tháp)", latlng: L.latLng(10.458000, 105.632000) },
    { name: "Bách Hóa Xanh - Sa Đéc (Đồng Tháp)", latlng: L.latLng(10.259000, 105.593000) },
    { name: "Bách Hóa Xanh - Hồng Ngự (Đồng Tháp)", latlng: L.latLng(10.806000, 105.341000) },
    { name: "Bách Hóa Xanh - Lai Vung (Đồng Tháp)", latlng: L.latLng(10.258758, 105.592634) },

    // === AN GIANG ===
    { name: "Bách Hóa Xanh - Long Xuyên (An Giang)", latlng: L.latLng(10.383000, 105.435000) },
    { name: "Bách Hóa Xanh - Châu Đốc (An Giang)", latlng: L.latLng(10.697000, 105.118000) },

    // === KIÊN GIANG ===
    { name: "Bách Hóa Xanh - Rạch Giá (Kiên Giang)", latlng: L.latLng(10.013000, 105.087000) },
    { name: "Bách Hóa Xanh - Hà Tiên (Kiên Giang)", latlng: L.latLng(10.382000, 104.490000) },
    { name: "Bách Hóa Xanh - Phú Quốc (Kiên Giang)", latlng: L.latLng(10.289000, 103.984000) },

    // === CẦN THƠ ===
    { name: "Bách Hóa Xanh - Ninh Kiều (Cần Thơ)", latlng: L.latLng(10.031000, 105.768000) },
    { name: "Bách Hóa Xanh - Bình Thủy (Cần Thơ)", latlng: L.latLng(10.047000, 105.726000) },
    { name: "Bách Hóa Xanh - Cái Răng (Cần Thơ)", latlng: L.latLng(10.006757, 105.785853) },
    { name: "Bách Hóa Xanh - Ô Môn (Cần Thơ)", latlng: L.latLng(10.086000, 105.657000) },

    // === HẬU GIANG ===
    { name: "Bách Hóa Xanh - Vị Thanh (Hậu Giang)", latlng: L.latLng(9.790000, 105.471000) },

    // === SÓC TRĂNG ===
    { name: "Bách Hóa Xanh - Sóc Trăng", latlng: L.latLng(9.603000, 105.974000) },
    { name: "Bách Hóa Xanh - Ngã Năm (Sóc Trăng)", latlng: L.latLng(9.508000, 105.742000) },

    // === TRÀ VINH ===
    { name: "Bách Hóa Xanh - Trà Vinh", latlng: L.latLng(9.934000, 106.345000) },
    { name: "Bách Hóa Xanh - Càng Long (Trà Vinh)", latlng: L.latLng(9.994000, 106.220000) },

    // === BẠC LIÊU ===
    { name: "Bách Hóa Xanh - Bạc Liêu", latlng: L.latLng(9.285000, 105.726000) },
    { name: "Bách Hóa Xanh - Giá Rai (Bạc Liêu)", latlng: L.latLng(9.144000, 105.412000) },

    // === CÀ MAU ===
    { name: "Bách Hóa Xanh - Cà Mau", latlng: L.latLng(9.177000, 105.150000) },

    // === KHÁNH HÒA ===
    { name: "Bách Hóa Xanh - Lý Tự Trọng (Diên Khánh, Khánh Hòa)", latlng: L.latLng(12.261200, 109.096500) },
    { name: "Bách Hóa Xanh - Bắc Nha Trang (Khánh Hòa)", latlng: L.latLng(12.288260, 109.203486) },
    { name: "Bách Hóa Xanh - Cam Ranh (Khánh Hòa)", latlng: L.latLng(11.921000, 109.158000) },
    { name: "Bách Hóa Xanh - Trần Quý Cáp (Ninh Hòa, Khánh Hòa)", latlng: L.latLng(12.493264, 109.127525) },
    { name: "Bách Hóa Xanh - Vạn Ninh (Khánh Hòa)", latlng: L.latLng(12.692000, 109.222000) },

    // === PHÚ YÊN ===
    { name: "Bách Hóa Xanh - Tuy Hòa (Phú Yên)", latlng: L.latLng(13.096000, 109.296000) },
    { name: "Bách Hóa Xanh - Đông Hòa (Phú Yên)", latlng: L.latLng(13.007000, 109.323000) },

    // === NINH THUẬN ===
    { name: "Bách Hóa Xanh - Phan Rang (Ninh Thuận)", latlng: L.latLng(11.565000, 108.988000) },
    { name: "Bách Hóa Xanh - Ninh Phước (Ninh Thuận)", latlng: L.latLng(11.464000, 108.923000) },

    // === BÌNH THUẬN ===
    { name: "Bách Hóa Xanh - Phan Thiết (Bình Thuận)", latlng: L.latLng(10.928000, 108.102000) },
    { name: "Bách Hóa Xanh - La Gi (Bình Thuận)", latlng: L.latLng(10.659000, 107.769000) },

    // === ĐẮK LẮK ===
    { name: "Bách Hóa Xanh - Buôn Ma Thuột (Đắk Lắk)", latlng: L.latLng(12.666000, 108.037000) },
    { name: "Bách Hóa Xanh - Krông Búk (Đắk Lắk)", latlng: L.latLng(12.732000, 108.099000) },
    { name: "Bách Hóa Xanh - Ea Kar (Đắk Lắk)", latlng: L.latLng(12.798000, 108.440000) },
    { name: "Bách Hóa Xanh - Buôn Hồ (Đắk Lắk)", latlng: L.latLng(12.906000, 108.262000) },

    // === ĐẮK NÔNG ===
    { name: "Bách Hóa Xanh - Gia Nghĩa (Đắk Nông)", latlng: L.latLng(11.977000, 107.695000) },
    { name: "Bách Hóa Xanh - Đắk Mil (Đắk Nông)", latlng: L.latLng(12.454000, 107.633000) },

    // === LÂM ĐỒNG ===
    { name: "Bách Hóa Xanh - Đà Lạt (Lâm Đồng)", latlng: L.latLng(11.954089, 108.429891) },
    { name: "Bách Hóa Xanh - Bảo Lộc (Lâm Đồng)", latlng: L.latLng(11.548000, 107.808000) },
    { name: "Bách Hóa Xanh - Di Linh (Lâm Đồng)", latlng: L.latLng(11.579000, 108.072000) },

    // === GIA LAI ===
    { name: "Bách Hóa Xanh - Pleiku (Gia Lai)", latlng: L.latLng(13.983000, 108.000000) },
    { name: "Bách Hóa Xanh - An Khê (Gia Lai)", latlng: L.latLng(13.959000, 108.643000) },
    { name: "Bách Hóa Xanh - Chư Prông (Gia Lai)", latlng: L.latLng(13.764000, 107.968000) },

    // === KON TUM ===
    { name: "Bách Hóa Xanh - Kon Tum", latlng: L.latLng(14.363125, 107.999265) },

    // === BÌNH ĐỊNH ===
    { name: "Bách Hóa Xanh - Quy Nhơn (Bình Định)", latlng: L.latLng(13.776000, 109.223000) },
    { name: "Bách Hóa Xanh - Hoài Nhơn (Bình Định)", latlng: L.latLng(14.535000, 109.016000) },
    { name: "Bách Hóa Xanh - An Nhơn (Bình Định)", latlng: L.latLng(13.878000, 109.093000) },

    // === QUẢNG NGÃI ===
    { name: "Bách Hóa Xanh - Quảng Ngãi", latlng: L.latLng(15.121000, 108.804000) },
    { name: "Bách Hóa Xanh - Đức Phổ (Quảng Ngãi)", latlng: L.latLng(14.838000, 108.968000) },

    // === QUẢNG NAM ===
    { name: "Bách Hóa Xanh - Tam Kỳ (Quảng Nam)", latlng: L.latLng(15.574000, 108.474000) },
    { name: "Bách Hóa Xanh - Hội An (Quảng Nam)", latlng: L.latLng(15.884538, 108.348626) },
    { name: "Bách Hóa Xanh - Điện Bàn (Quảng Nam)", latlng: L.latLng(15.898000, 108.222000) },

    // === ĐÀ NẴNG ===
    { name: "Bách Hóa Xanh - Lê Thanh Nghị (Hải Châu, Đà Nẵng)", latlng: L.latLng(16.038400, 108.221200) },
    { name: "Bách Hóa Xanh - Thanh Khê (Đà Nẵng)", latlng: L.latLng(16.066000, 108.182000) },
    { name: "Bách Hóa Xanh - Liên Chiểu (Đà Nẵng)", latlng: L.latLng(16.091000, 108.122000) },
    { name: "Bách Hóa Xanh - Ngũ Hành Sơn (Đà Nẵng)", latlng: L.latLng(16.015000, 108.264000) },
    { name: "Bách Hóa Xanh - Cẩm Lệ (Đà Nẵng)", latlng: L.latLng(16.018000, 108.208000) },

    // === THỪA THIÊN HUẾ ===
    { name: "Bách Hóa Xanh - Huế", latlng: L.latLng(16.462000, 107.590000) },
    { name: "Bách Hóa Xanh - Phú Bài (Huế)", latlng: L.latLng(16.400000, 107.706000) },
    { name: "Bách Hóa Xanh - Hương Thủy (Huế)", latlng: L.latLng(16.350000, 107.630000) },

    // === QUẢNG TRỊ ===
    { name: "Bách Hóa Xanh - Đông Hà (Quảng Trị)", latlng: L.latLng(16.818000, 107.100000) },
    { name: "Bách Hóa Xanh - Quảng Trị", latlng: L.latLng(16.748000, 107.187000) },

    // === QUẢNG BÌNH ===
    { name: "Bách Hóa Xanh - Đồng Hới (Quảng Bình)", latlng: L.latLng(17.482000, 106.600000) },

    // === THANH HÓA ===
    { name: "Bách Hóa Xanh - TP Thanh Hóa", latlng: L.latLng(19.807000, 105.776000) },
    { name: "Bách Hóa Xanh - Sầm Sơn (Thanh Hóa)", latlng: L.latLng(19.745000, 105.901000) },
    { name: "Bách Hóa Xanh - Bỉm Sơn (Thanh Hóa)", latlng: L.latLng(20.089000, 105.859000) },

    // === NGHỆ AN ===
    { name: "Bách Hóa Xanh - Vinh (Nghệ An)", latlng: L.latLng(18.680000, 105.681000) },
    { name: "Bách Hóa Xanh - Cửa Lò (Nghệ An)", latlng: L.latLng(18.816000, 105.728000) },

    // === HÀ TĨNH ===
    { name: "Bách Hóa Xanh - TP Hà Tĩnh", latlng: L.latLng(18.343000, 105.909000) },
    { name: "Bách Hóa Xanh - Kỳ Anh (Hà Tĩnh)", latlng: L.latLng(18.068000, 106.278000) },

    // === NINH BÌNH (Khai trương 11/2025 - tỉnh đầu tiên BHX Bắc tiến) ===
    { name: "Bách Hóa Xanh - TP Ninh Bình", latlng: L.latLng(20.251000, 105.975000) },
    { name: "Bách Hóa Xanh - Hoa Lư (Ninh Bình)", latlng: L.latLng(20.283000, 105.900000) },
    { name: "Bách Hóa Xanh - Tam Điệp (Ninh Bình)", latlng: L.latLng(20.149000, 105.909000) },
    { name: "Bách Hóa Xanh - Yên Khánh (Ninh Bình)", latlng: L.latLng(20.203000, 106.050000) },
    { name: "Bách Hóa Xanh - Kim Sơn (Ninh Bình)", latlng: L.latLng(20.083000, 106.083000) },

    // === HƯNG YÊN ===
    { name: "Bách Hóa Xanh - TP Hưng Yên", latlng: L.latLng(20.646000, 106.051000) },
    { name: "Bách Hóa Xanh - Văn Lâm (Hưng Yên)", latlng: L.latLng(20.959000, 106.022000) },
    { name: "Bách Hóa Xanh - Mỹ Hào (Hưng Yên)", latlng: L.latLng(20.950000, 106.043000) },
    { name: "Bách Hóa Xanh - Yên Mỹ (Hưng Yên)", latlng: L.latLng(20.910000, 106.013000) },

    // === HẢI PHÒNG ===
    { name: "Bách Hóa Xanh - Lê Chân (Hải Phòng)", latlng: L.latLng(20.851000, 106.677000) },
    { name: "Bách Hóa Xanh - Ngô Quyền (Hải Phòng)", latlng: L.latLng(20.861000, 106.695000) },
    { name: "Bách Hóa Xanh - Hải An (Hải Phòng)", latlng: L.latLng(20.840000, 106.741000) },
    { name: "Bách Hóa Xanh - An Dương (Hải Phòng)", latlng: L.latLng(20.874000, 106.630000) },

    // === BẮC NINH ===
    { name: "Bách Hóa Xanh - TP Bắc Ninh", latlng: L.latLng(21.186000, 106.076000) },
    { name: "Bách Hóa Xanh - Từ Sơn (Bắc Ninh)", latlng: L.latLng(21.115000, 105.996000) },
    { name: "Bách Hóa Xanh - Tiên Du (Bắc Ninh)", latlng: L.latLng(21.141000, 106.007000) },

    // === QUẢNG NINH ===
    { name: "Bách Hóa Xanh - Hạ Long (Quảng Ninh)", latlng: L.latLng(20.951000, 107.072000) },
    { name: "Bách Hóa Xanh - Uông Bí (Quảng Ninh)", latlng: L.latLng(21.034000, 106.762000) },
    { name: "Bách Hóa Xanh - Đông Triều (Quảng Ninh)", latlng: L.latLng(21.031000, 106.544000) },

    // === HÀ NỘI (Khai trương 15/05/2026) ===
    { name: "Bách Hóa Xanh - Phú Diễn (Bắc Từ Liêm, Hà Nội)", latlng: L.latLng(21.049000, 105.763000) },
    { name: "Bách Hóa Xanh - Phúc Lợi (Long Biên, Hà Nội)", latlng: L.latLng(21.037000, 105.898000) },
    { name: "Bách Hóa Xanh - Gia Lâm (Hà Nội)", latlng: L.latLng(21.052000, 105.879000) },
    { name: "Bách Hóa Xanh - Hoài Đức (Hà Nội)", latlng: L.latLng(21.023000, 105.704000) },
    { name: "Bách Hóa Xanh - Đông Anh (Hà Nội)", latlng: L.latLng(21.135000, 105.845000) },
    { name: "Bách Hóa Xanh - Gia Lâm 2 (Hà Nội)", latlng: L.latLng(21.040000, 105.920000) },
    { name: "Bách Hóa Xanh - Doãn Kế Thiện (Bắc Từ Liêm, Hà Nội)", latlng: L.latLng(21.036600, 105.776600) },
    { name: "Bách Hóa Xanh - Vũ Xuân Thiều (Long Biên, Hà Nội)", latlng: L.latLng(21.037500, 105.908000) },
];


function fetchNearestRealBranch(latlng, addressText, callback) {
    // If distance to all hardcoded branches is <= 25km, just use the hardcoded ones!
    let nearestHardcoded = storeBranches[0];
    let minHardcodedDist = latlng.distanceTo(nearestHardcoded.latlng) / 1000.0;
    for (let i = 1; i < storeBranches.length; i++) {
        const d = latlng.distanceTo(storeBranches[i].latlng) / 1000.0;
        if (d < minHardcodedDist) {
            minHardcodedDist = d;
            nearestHardcoded = storeBranches[i];
        }
    }

    if (minHardcodedDist <= 50) {
        callback(nearestHardcoded);
        return;
    }

    // Otherwise, search for real Bách Hóa Xanh stores near the user's location via OSM Nominatim
    const delta = 0.3; // Bounding box of ~30km around customer
    const left = latlng.lng - delta;
    const right = latlng.lng + delta;
    const top = latlng.lat + delta;
    const bottom = latlng.lat - delta;
    
    // Also extract province name if available to refine search
    let provinceName = "";
    if (addressText) {
        const parts = addressText.split(',').map(p => p.trim());
        if (parts.length >= 1) {
            provinceName = parts[parts.length - 1];
        }
    }
    
    let query = "Bách Hóa Xanh";
    if (provinceName && !provinceName.includes("Việt Nam")) {
        query = "Bách Hóa Xanh, " + provinceName;
    }
    let searchUrl = `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&viewbox=${left},${top},${right},${bottom}&bounded=1&limit=15&addressdetails=1`;
    
    fetch(searchUrl, {
        headers: { 'Accept-Language': 'vi,en;q=0.9' }
    })
    .then(res => res.json())
    .then(results => {
        if (results && results.length > 0) {
            let nearestReal = null;
            let minRealDist = Infinity;
            
            results.forEach(item => {
                const storeLoc = L.latLng(parseFloat(item.lat), parseFloat(item.lon));
                const dist = latlng.distanceTo(storeLoc) / 1000.0;
                if (dist < minRealDist) {
                    minRealDist = dist;
                    // Format store name, e.g. "Bách Hóa Xanh - Trần Quý Cáp"
                    let displayName = item.display_name.split(',')[0].trim();
                    if (!displayName.toLowerCase().startsWith("bách hóa xanh")) {
                        displayName = "Bách Hóa Xanh - " + displayName;
                    }
                    nearestReal = {
                        name: displayName,
                        latlng: storeLoc,
                        address: item.display_name
                    };
                }
            });
            
            if (nearestReal) {
                console.log("Found nearest real store branch:", nearestReal);
                callback(nearestReal);
                return;
            }
        }
        
        // Fallback: if no real Bách Hóa Xanh store is found on OSM in that area, generate a virtual local one
        console.log("No real Bách Hóa Xanh found on OSM in this area. Generating virtual branch...");
        let fallbackProvince = provinceName || "Khánh Hòa";
        let districtName = "";
        if (addressText) {
            const parts = addressText.split(',').map(p => p.trim());
            if (parts.length >= 2) districtName = parts[parts.length - 2];
        }
        let branchName = "Bách Hóa Xanh - Chi nhánh " + fallbackProvince;
        if (districtName && !districtName.toLowerCase().startsWith("đường") && !districtName.toLowerCase().startsWith("phố") && !districtName.toLowerCase().startsWith("quốc lộ")) {
            branchName = `Bách Hóa Xanh - ${districtName} (${fallbackProvince})`;
        }
        const virtualLatLng = L.latLng(latlng.lat + 0.012, latlng.lng + 0.012);
        callback({ name: branchName, latlng: virtualLatLng, isVirtual: true });
    })
    .catch(err => {
        console.error("Error fetching real branches:", err);
        // Fallback on error
        const virtualLatLng = L.latLng(latlng.lat + 0.012, latlng.lng + 0.012);
        callback({ name: "Bách Hóa Xanh - Chi nhánh " + (provinceName || "Khánh Hòa"), latlng: virtualLatLng, isVirtual: true });
    });
}

let storeLatLng = storeBranches[0].latlng; // default fallback
let activeStoreLatLng = storeBranches[0].latlng;
let activeStoreName = storeBranches[0].name;

let storeMarker = null;
let deliveryRouteLine = null;
let lastShippingCalc = null;

// Setup modal event listener
const mapModalEl = document.getElementById('mapModal');
if (mapModalEl) {
    mapModalEl.addEventListener('shown.bs.modal', function () {
        if (!map) {
            const initLatLng = selectedLatLng || storeLatLng;
            
            // Initialize map centered on initLatLng with rotation enabled
            map = L.map('map', { 
                zoomControl: false, 
                rotate: true,
                rotateControl: false,
                bearing: 0,
                maxZoom: 21
            }).setView(initLatLng, 15);
            
            // Add zoom control at bottomright to avoid overlapping search bar
            L.control.zoom({ position: 'bottomright' }).addTo(map);

            // Layer switcher layers definition
            const roadmapLayer = L.tileLayer('https://mt1.google.com/vt/lyrs=m&hl=vi&gl=vn&x={x}&y={y}&z={z}', { 
                attribution: '&copy; Google Maps',
                maxZoom: 21,
                maxNativeZoom: 21
            });
            const satelliteLayer = L.tileLayer('https://mt1.google.com/vt/lyrs=y&hl=vi&gl=vn&x={x}&y={y}&z={z}', { 
                attribution: '&copy; Google Maps',
                maxZoom: 21,
                maxNativeZoom: 21
            });
            const terrainLayer = L.tileLayer('https://mt1.google.com/vt/lyrs=p&hl=vi&gl=vn&x={x}&y={y}&z={z}', { 
                attribution: '&copy; Google Maps',
                maxZoom: 21,
                maxNativeZoom: 21
            });
            const trafficLayer = L.tileLayer('https://mt1.google.com/vt?lyrs=h,traffic&hl=vi&gl=vn&x={x}&y={y}&z={z}', { 
                attribution: '&copy; Google Maps', 
                zIndex: 10,
                maxZoom: 21,
                maxNativeZoom: 21
            });
            const transitLayer = L.tileLayer('https://mt1.google.com/vt?lyrs=h,transit&hl=vi&gl=vn&x={x}&y={y}&z={z}', { 
                attribution: '&copy; Google Maps', 
                zIndex: 11,
                maxZoom: 21,
                maxNativeZoom: 21
            });

            let activeBaseLayer = roadmapLayer;
            roadmapLayer.addTo(map);

            // Add custom compass button overlay (placed above zoom control at bottomright)
            const mapContainer = map.getContainer();
            const compassHtml = `
                <div id="custom-compass-btn" style="position: absolute; bottom: 105px; right: 10px; width: 40px; height: 40px; background: #222222; border: 2px solid #ffffff; border-radius: 50%; box-shadow: 0 3px 8px rgba(0,0,0,0.4); z-index: 1000; cursor: pointer; display: flex; align-items: center; justify-content: center; transition: all 0.2s;">
                    <svg width="32" height="32" viewBox="0 0 34 34" fill="none" xmlns="http://www.w3.org/2000/svg">
                        <!-- Left curved arrow -->
                        <path d="M 9 11 A 10 10 0 0 0 9 23" stroke="#ffffff" stroke-width="2" stroke-linecap="round" fill="none" />
                        <path d="M 6 20 L 9 23 L 12 21" stroke="#ffffff" stroke-width="2" stroke-linejoin="round" fill="none" />
                        <!-- Right curved arrow -->
                        <path d="M 25 11 A 10 10 0 0 1 25 23" stroke="#ffffff" stroke-width="2" stroke-linecap="round" fill="none" />
                        <path d="M 28 20 L 25 23 L 22 21" stroke="#ffffff" stroke-width="2" stroke-linejoin="round" fill="none" />
                        <!-- The center needle group (which we rotate dynamically to match the map bearing) -->
                        <g id="custom-compass-needle" style="transform-origin: 17px 17px; transition: transform 0.1s ease;">
                            <!-- Red North pointer -->
                            <path d="M 17 6 L 21 17 L 17 14 L 13 17 Z" fill="#dc3545" />
                            <!-- White South pointer -->
                            <path d="M 17 28 L 21 17 L 17 20 L 13 17 Z" fill="#e0e0e0" />
                        </g>
                    </svg>
                </div>
            `;
            mapContainer.insertAdjacentHTML('beforeend', compassHtml);

            const compassBtn = document.getElementById('custom-compass-btn');
            if (compassBtn) {
                L.DomEvent.disableClickPropagation(compassBtn);
                L.DomEvent.disableScrollPropagation(compassBtn);
            }

            // Add click listener to reset rotation
            document.getElementById('custom-compass-btn').addEventListener('click', function() {
                map.setBearing(0);
            });

            // Update compass needle rotation based on map bearing
            map.on('rotate', function() {
                const bearing = map.getBearing();
                const needle = document.getElementById('custom-compass-needle');
                if (needle) {
                    needle.style.transform = `rotate(${-bearing}deg)`;
                }
            });

            // Support rotating map by Ctrl + Mouse Drag (Rotate mouse)
            let isRotatingMap = false;
            let startMouseAngle = 0;
            let startMapBearing = 0;

            mapContainer.addEventListener('mousedown', function(e) {
                if (e.ctrlKey && e.button === 0) { // Ctrl + Left Click
                    e.preventDefault();
                    e.stopPropagation();
                    isRotatingMap = true;
                    map.dragging.disable();
                    
                    const rect = mapContainer.getBoundingClientRect();
                    const cx = rect.left + rect.width / 2;
                    const cy = rect.top + rect.height / 2;
                    
                    const dx = e.clientX - cx;
                    const dy = e.clientY - cy;
                    startMouseAngle = Math.atan2(dy, dx) * 180 / Math.PI;
                    startMapBearing = map.getBearing();
                    
                    document.addEventListener('mousemove', onMapRotateMove);
                    document.addEventListener('mouseup', onMapRotateUp);
                }
            }, true); // Capture phase to intercept before Leaflet starts panning

            function onMapRotateMove(e) {
                if (!isRotatingMap) return;
                e.preventDefault();
                
                const rect = mapContainer.getBoundingClientRect();
                const cx = rect.left + rect.width / 2;
                const cy = rect.top + rect.height / 2;
                
                const dx = e.clientX - cx;
                const dy = e.clientY - cy;
                const currentMouseAngle = Math.atan2(dy, dx) * 180 / Math.PI;
                
                const angleDiff = currentMouseAngle - startMouseAngle;
                let newBearing = startMapBearing + angleDiff;
                map.setBearing(newBearing);
            }

            function onMapRotateUp(e) {
                if (isRotatingMap) {
                    isRotatingMap = false;
                    map.dragging.enable();
                    document.removeEventListener('mousemove', onMapRotateMove);
                    document.removeEventListener('mouseup', onMapRotateUp);
                }
            }

            // Add custom Layer Switcher Control at bottomleft
            const layerSwitcherHtml = `
                <div id="map-layers-control" style="position: absolute; bottom: 20px; left: 10px; z-index: 1000;">
                    <!-- Toggle Button -->
                    <button type="button" id="map-layers-toggle-btn" style="width: 44px; height: 44px; background: #ffffff; border: 2px solid #ffffff; border-radius: 8px; box-shadow: 0 2px 6px rgba(0,0,0,0.3); cursor: pointer; display: flex; flex-direction: column; align-items: center; justify-content: center; font-size: 11px; font-weight: bold; color: #555555; transition: all 0.2s;">
                        <i class="bi bi-layers-half" style="font-size: 18px; color: #1a7a2e;"></i>
                        <span style="font-size: 8px; margin-top: 1px; font-weight: 700;">Lớp</span>
                    </button>
                    
                    <!-- Expanded Menu Panel -->
                    <div id="map-layers-panel" style="position: absolute; bottom: 50px; left: 0; background: #ffffff; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.15); border: 1px solid #e0e0e0; padding: 12px; display: none; flex-direction: row; gap: 10px; align-items: flex-start; min-width: 320px;">
                        <!-- Mặc định (Default Map) -->
                        <div class="map-type-card active" data-type="roadmap" style="display: flex; flex-direction: column; align-items: center; cursor: pointer; width: 60px;">
                            <div class="map-layer-thumb" style="width: 56px; height: 42px; border-radius: 8px; border: 2px solid #1a73e8; overflow: hidden; box-sizing: border-box; transition: all 0.2s;">
                                <svg width="100%" height="100%" viewBox="0 0 60 45" style="display: block;">
                                    <rect width="60" height="45" fill="#e4f1fb" />
                                    <path d="M 0,0 L 45,0 L 25,45 L 0,45 Z" fill="#f4f3f0" />
                                    <path d="M 0,0 L 25,0 L 10,25 L 0,20 Z" fill="#d0eed4" />
                                    <path d="M 0,10 L 60,10 M 15,0 L 15,45 M 40,0 L 40,45 M 0,35 L 60,35" stroke="#e9e9e9" stroke-width="1" />
                                    <path d="M 0,22 L 60,22" stroke="#ffd54f" stroke-width="3" />
                                    <path d="M 30,0 L 30,45" stroke="#ffd54f" stroke-width="3" />
                                </svg>
                            </div>
                            <span style="font-size: 8.5px; font-weight: 700; color: #1a73e8; margin-top: 4px; text-align: center; white-space: nowrap;">Mặc định</span>
                        </div>

                        <!-- Vệ tinh (Satellite Map) -->
                        <div class="map-type-card" data-type="satellite" style="display: flex; flex-direction: column; align-items: center; cursor: pointer; width: 60px;">
                            <div class="map-layer-thumb" style="width: 56px; height: 42px; border-radius: 8px; border: 2px solid transparent; overflow: hidden; box-sizing: border-box; transition: all 0.2s; background: #c8c8c8;">
                                <svg width="100%" height="100%" viewBox="0 0 60 45" style="display: block;">
                                    <rect width="60" height="45" fill="#1b3d22" />
                                    <path d="M 40,0 Q 45,20 60,30 L 60,0 Z" fill="#0c2340" />
                                    <rect x="5" y="5" width="10" height="8" fill="#5c5446" opacity="0.8" />
                                    <rect x="20" y="8" width="12" height="10" fill="#6d4c41" opacity="0.8" />
                                    <rect x="8" y="25" width="14" height="12" fill="#546e7a" opacity="0.8" />
                                    <path d="M 0,20 Q 30,22 60,20" stroke="#ffffff" stroke-width="1.5" opacity="0.6" fill="none" />
                                    <path d="M 35,0 Q 33,20 30,45" stroke="#ffffff" stroke-width="1.5" opacity="0.6" fill="none" />
                                </svg>
                            </div>
                            <span style="font-size: 8.5px; font-weight: 700; color: #5f6368; margin-top: 4px; text-align: center; white-space: nowrap;">Vệ tinh</span>
                        </div>

                        <!-- Địa hình (Terrain Map) -->
                        <div class="map-type-card" data-type="terrain" style="display: flex; flex-direction: column; align-items: center; cursor: pointer; width: 60px;">
                            <div class="map-layer-thumb" style="width: 56px; height: 42px; border-radius: 8px; border: 2px solid transparent; overflow: hidden; box-sizing: border-box; transition: all 0.2s;">
                                <svg width="100%" height="100%" viewBox="0 0 60 45" style="display: block;">
                                    <rect width="60" height="45" fill="#e0d4be" />
                                    <path d="M 0,45 Q 20,20 40,25 Q 50,28 60,10 L 60,45 Z" fill="#c3d7a4" />
                                    <path d="M -10,35 Q 15,15 35,18 Q 45,20 60,5" stroke="#b0a080" stroke-width="0.8" fill="none" />
                                    <path d="M -10,40 Q 18,22 38,25 Q 48,27 60,12" stroke="#b0a080" stroke-width="0.8" fill="none" />
                                    <path d="M -10,45 Q 20,28 40,31 Q 50,33 60,20" stroke="#b0a080" stroke-width="0.8" fill="none" />
                                    <path d="M 5,0 Q 10,20 25,28 T 55,45" stroke="#ffffff" stroke-width="1.5" fill="none" />
                                </svg>
                            </div>
                            <span style="font-size: 8.5px; font-weight: 700; color: #5f6368; margin-top: 4px; text-align: center; white-space: nowrap;">Địa hình</span>
                        </div>

                        <!-- Vertical Divider -->
                        <div style="width: 1px; height: 50px; background: #e0e0e0; align-self: center; margin: 0 2px;"></div>

                        <!-- Giao thông (Traffic Overlay) -->
                        <div class="map-detail-card" id="traffic-toggle-card" style="display: flex; flex-direction: column; align-items: center; cursor: pointer; width: 60px;">
                            <div class="map-layer-thumb" style="width: 56px; height: 42px; border-radius: 8px; border: 2px solid transparent; overflow: hidden; box-sizing: border-box; transition: all 0.2s;">
                                <svg width="100%" height="100%" viewBox="0 0 60 45" style="display: block;">
                                    <rect width="60" height="45" fill="#f4f3f0" />
                                    <path d="M 12,0 L 12,45 M 48,0 L 48,45 M 0,15 L 60,15 M 0,30 L 60,30" stroke="#e0e0e0" stroke-width="3" fill="none" />
                                    <path d="M 12,0 L 12,20 M 0,15 L 30,15" stroke="#4caf50" stroke-width="2" fill="none" />
                                    <path d="M 12,20 L 12,45 M 30,15 L 48,15" stroke="#ffc107" stroke-width="2" fill="none" />
                                    <path d="M 48,15 L 60,15 M 48,0 L 48,45" stroke="#f44336" stroke-width="2" fill="none" />
                                </svg>
                            </div>
                            <span style="font-size: 8.5px; font-weight: 700; color: #5f6368; margin-top: 4px; text-align: center; line-height: 1.1;">Giao thông</span>
                        </div>

                        <!-- Công cộng (Transit Overlay) -->
                        <div class="map-detail-card" id="transit-toggle-card" style="display: flex; flex-direction: column; align-items: center; cursor: pointer; width: 60px;">
                            <div class="map-layer-thumb" style="width: 56px; height: 42px; border-radius: 8px; border: 2px solid transparent; overflow: hidden; box-sizing: border-box; transition: all 0.2s;">
                                <svg width="100%" height="100%" viewBox="0 0 60 45" style="display: block;">
                                    <rect width="60" height="45" fill="#f4f3f0" />
                                    <path d="M 0,12 L 40,12 Q 50,12 50,28 L 50,45" stroke="#00bcd4" stroke-width="3" fill="none" />
                                    <path d="M 22,0 L 22,45" stroke="#9c27b0" stroke-width="2.5" fill="none" />
                                    <circle cx="22" cy="12" r="3.5" fill="#ffffff" stroke="#00bcd4" stroke-width="1.5" />
                                    <circle cx="50" cy="28" r="3.5" fill="#ffffff" stroke="#9c27b0" stroke-width="1.5" />
                                </svg>
                            </div>
                            <span style="font-size: 8.5px; font-weight: 700; color: #5f6368; margin-top: 4px; text-align: center; line-height: 1.1;">Công cộng</span>
                        </div>
                    </div>
                </div>
            `;
            mapContainer.insertAdjacentHTML('beforeend', layerSwitcherHtml);

            const layersControl = document.getElementById('map-layers-control');
            if (layersControl) {
                L.DomEvent.disableClickPropagation(layersControl);
                L.DomEvent.disableScrollPropagation(layersControl);
            }

            // Layer Switcher JS logic
            const layersBtn = document.getElementById('map-layers-toggle-btn');
            const layersPanel = document.getElementById('map-layers-panel');
            
            layersBtn.addEventListener('click', function(e) {
                e.stopPropagation();
                layersPanel.style.display = layersPanel.style.display === 'none' ? 'flex' : 'none';
            });
            
            // Close layers panel when clicking on map
            map.on('click', function() {
                layersPanel.style.display = 'none';
            });
            
            // Base map switcher
            document.querySelectorAll('.map-type-card').forEach(card => {
                card.addEventListener('click', function() {
                    document.querySelectorAll('.map-type-card').forEach(c => {
                        const thumb = c.querySelector('.map-layer-thumb');
                        const label = c.querySelector('span');
                        if (thumb) thumb.style.borderColor = 'transparent';
                        if (label) label.style.color = '#5f6368';
                    });
                    const activeThumb = this.querySelector('.map-layer-thumb');
                    const activeLabel = this.querySelector('span');
                    if (activeThumb) activeThumb.style.borderColor = '#1a73e8';
                    if (activeLabel) activeLabel.style.color = '#1a73e8';
                    
                    const type = this.dataset.type;
                    map.removeLayer(activeBaseLayer);
                    if (type === 'roadmap') activeBaseLayer = roadmapLayer;
                    else if (type === 'satellite') activeBaseLayer = satelliteLayer;
                    else if (type === 'terrain') activeBaseLayer = terrainLayer;
                    
                    activeBaseLayer.addTo(map);
                    
                    // Re-apply overlays if they are currently active so they draw on top of new base
                    if (map.hasLayer(trafficLayer)) {
                        map.removeLayer(trafficLayer);
                        trafficLayer.addTo(map);
                    }
                    if (map.hasLayer(transitLayer)) {
                        map.removeLayer(transitLayer);
                        transitLayer.addTo(map);
                    }
                });
            });
            
            // Traffic toggle
            const trafficCard = document.getElementById('traffic-toggle-card');
            trafficCard.addEventListener('click', function() {
                const thumb = this.querySelector('.map-layer-thumb');
                const label = this.querySelector('span');
                if (map.hasLayer(trafficLayer)) {
                    map.removeLayer(trafficLayer);
                    if (thumb) thumb.style.borderColor = 'transparent';
                    if (label) label.style.color = '#5f6368';
                } else {
                    trafficLayer.addTo(map);
                    if (thumb) thumb.style.borderColor = '#1a73e8';
                    if (label) label.style.color = '#1a73e8';
                }
            });
            
            // Transit toggle
            const transitCard = document.getElementById('transit-toggle-card');
            transitCard.addEventListener('click', function() {
                const thumb = this.querySelector('.map-layer-thumb');
                const label = this.querySelector('span');
                if (map.hasLayer(transitLayer)) {
                    map.removeLayer(transitLayer);
                    if (thumb) thumb.style.borderColor = 'transparent';
                    if (label) label.style.color = '#5f6368';
                } else {
                    transitLayer.addTo(map);
                    if (thumb) thumb.style.borderColor = '#1a73e8';
                    if (label) label.style.color = '#1a73e8';
                }
            });

            // Map click handler to place/move marker
            map.on('click', function(e) {
                updateMarker(e.latlng);
            });

            // Immediately place marker at initLatLng
            updateMarker(initLatLng, selectedLatLng ? true : false);

            // Only perform geolocation if there is no pre-selected location
            if (!selectedLatLng && navigator.geolocation) {
                navigator.geolocation.getCurrentPosition(function(position) {
                    const latlng = L.latLng(position.coords.latitude, position.coords.longitude);
                    map.setView(latlng, 16);
                    updateMarker(latlng);
                }, function(err) {
                    console.log("Geolocation error or permission denied:", err);
                }, {
                    timeout: 5000
                });
            }
        } else {
            map.invalidateSize();
            if (selectedLatLng) {
                map.setView(selectedLatLng);
                updateMarker(selectedLatLng, true);
            }
            // Khi modal mở lại (lần 2+): ẩn dropdown, không tự động tìm kiếm lại
            const listEl = document.getElementById('branchSearchResults');
            if (listEl) listEl.style.display = "none";
        }
    });
}

function updateMarker(latlng, skipReverseGeocode = false) {
    selectedLatLng = latlng;
    if (marker) {
        marker.setLatLng(latlng);
    } else {
        marker = L.marker(latlng, { draggable: true }).addTo(map);

        marker.on('dragend', function(e) {
            selectedLatLng = marker.getLatLng();
            reverseGeocode(selectedLatLng);
            updateShippingInfo(selectedLatLng);
        });
    }
    if (!skipReverseGeocode) {
        reverseGeocode(latlng);
    }
    updateShippingInfo(latlng);
}

function buildCleanAddress(data) {
    if (!data || !data.address) return data.display_name || "";
    
    const addr = data.address;
    
    // 1. Part 1: [Số nhà Tên đường/ Thôn] (Separated by space, no comma)
    let houseNum = (addr.house_number || "").trim();
    let roadName = (addr.road || "").trim();
    
    // Add neighbourhood/khu phố/thôn to roadName if present
    let neighbourhood = addr.neighbourhood;
    if (!neighbourhood && addr.suburb && addr.suburb.includes("Khu phố")) {
        neighbourhood = addr.suburb;
    }
    if (neighbourhood) {
        roadName = roadName ? (roadName + ", " + neighbourhood) : neighbourhood;
    }
    
    let part1Text = "";
    if (houseNum && roadName) {
        part1Text = houseNum + " " + roadName; // Separated by space
    } else {
        part1Text = houseNum || roadName || "";
    }
    
    // 2. Part 2: [Phường/Xã]
    let suburb = addr.suburb;
    if (suburb && suburb.includes("Khu phố")) {
        suburb = ""; // Already handled in Part 1
    }
    let part2Text = suburb || addr.village || addr.commune || addr.hamlet || "";
    if (!part2Text && addr.town && (addr.town.startsWith("Phường") || addr.town.startsWith("Xã") || addr.town.startsWith("Thị trấn"))) {
        part2Text = addr.town;
    }
    part2Text = part2Text.trim();
    
    // 3. Part 3: [Tỉnh/Thành phố trực thuộc Trung ương] (District/Quận/Huyện is omitted)
    let city = addr.city || addr.town || addr.municipality;
    let state = addr.state || addr.province;
    
    let isHCMC = false;
    let isHanoi = false;
    let stateLower = (state || "").toLowerCase();
    let cityLower = (city || "").toLowerCase();
    let dispLower = (data.display_name || "").toLowerCase();
    
    if (stateLower.includes("hồ chí minh") || cityLower.includes("hồ chí minh") || dispLower.includes("hồ chí minh") || 
        (data.lat && parseFloat(data.lat) >= 10.70 && parseFloat(data.lat) <= 10.85 && 
         data.lon && parseFloat(data.lon) >= 106.60 && parseFloat(data.lon) <= 106.71) ||
        (cityLower === "thủ đức" || cityLower === "thành phố thủ đức")) {
        isHCMC = true;
    }
    
    if (stateLower.includes("hà nội") || cityLower.includes("hà nội") || dispLower.includes("hà nội")) {
        isHanoi = true;
    }
    
    if (isHCMC) {
        state = "Thành phố Hồ Chí Minh";
        city = "Thành phố Hồ Chí Minh";
        if (addr) {
            addr.province = "Thành phố Hồ Chí Minh";
        }
    } else if (isHanoi) {
        state = "Thành phố Hà Nội";
        city = "Thành phố Hà Nội";
        if (addr) {
            addr.province = "Thành phố Hà Nội";
        }
    }
    
    let provName = state || addr.province || city || "";
    provName = provName.trim();
    
    let part3Text = "";
    let provLower = provName.toLowerCase();
    if (provLower.includes("hồ chí minh")) {
        part3Text = "Thành phố Hồ Chí Minh";
    } else if (provLower.includes("hà nội")) {
        part3Text = "Thành phố Hà Nội";
    } else if (provLower.includes("đà nẵng")) {
        part3Text = "Thành phố Đà Nẵng";
    } else if (provLower.includes("hải phòng")) {
        part3Text = "Thành phố Hải Phòng";
    } else if (provLower.includes("cần thơ")) {
        part3Text = "Thành phố Cần Thơ";
    } else if (provName) {
        // Prepend "Tỉnh " if not already present
        if (!provName.startsWith("Tỉnh") && !provName.startsWith("Thành phố")) {
            part3Text = "Tỉnh " + provName;
        } else {
            part3Text = provName;
        }
    }
    
    // Combine the 3 parts with ", "
    const finalParts = [];
    if (part1Text) finalParts.push(part1Text);
    if (part2Text) finalParts.push(part2Text);
    if (part3Text) finalParts.push(part3Text);
    
    let addrStr = finalParts.join(", ");
    
    // Mismatch correction for Sư Vạn Hạnh alleys (OSM / Google Maps tiles discrepancy)
    if (addrStr.includes("Hẻm 814 Sư Vạn Hạnh") && data && data.lat && data.lon) {
        const clickLat = parseFloat(data.lat);
        const clickLon = parseFloat(data.lon);
        const d824 = Math.pow(clickLat - 10.7757433, 2) + Math.pow(clickLon - 106.6678552, 2);
        const d814 = Math.pow(clickLat - 10.7748976, 2) + Math.pow(clickLon - 106.6689445, 2);
        if (d824 < d814) {
            addrStr = addrStr.replace("Hẻm 814 Sư Vạn Hạnh", "Hẻm 824 Sư Vạn Hạnh");
        }
    }
    
    return addrStr;
}

function capitalizeWord(word) {
    if (!word) return "";
    return word.charAt(0).toUpperCase() + word.slice(1);
}

function parseSearchQueryFirstPart(firstPart) {
    let temp = firstPart.trim();
    
    // Strip leading "Số" or "Số nhà" if followed by a number
    let soMatch = temp.match(/^(số\s+nhà|số)\s+(\d)/i);
    if (soMatch) {
        temp = temp.substring(soMatch[0].length - 1).trim();
    }
    
    // Extract house number if it starts with digits/letters and slashes
    let houseNum = "";
    let houseMatch = temp.match(/^([0-9a-zA-Z-\/]+)\s+/);
    if (houseMatch) {
        houseNum = houseMatch[1];
        temp = temp.substring(houseMatch[0].length).trim();
    }
    
    // Now extract prefix word
    let prefix = "";
    let prefixMatch = temp.match(/^(thôn|ấp|xã|phường|p\.|đường|phố|ngõ|ngách|hẻm|kiệt)\s+/i);
    if (prefixMatch) {
        prefix = prefixMatch[1];
        temp = temp.substring(prefixMatch[0].length).trim();
    }
    
    let name = temp;
    return { houseNum, prefix, name };
}

function cleanSearchQuery(query) {
    if (!query) return "";
    
    // Check if query contains commas
    if (query.includes(',')) {
        let parts = query.split(',').map(p => p.trim()).filter(p => p);
        let cleanedParts = parts.map(part => {
            let clean = part;
            // Strip middle prefixes (e.g. "814/29 đường Sư Vạn Hạnh" -> "814/29 Sư Vạn Hạnh")
            clean = clean.replace(/^([0-9a-zA-Z-\/]+)\s+(đường|phố|ngõ|ngách|hẻm|kiệt)\s+/i, "$1 ");
            
            // Strip leading prefixes
            const prefixRegex = /^(thôn|ấp|xã|phường|p\.|quận|q\.|huyện|h\.|thị\s+xã|tx\.|tx|tỉnh|thành\s+phố|tp\.|tp|đường|phố|ngõ|ngách|hẻm|kiệt|số\s+nhà|số)\s+/i;
            let lastClean = "";
            while (clean !== lastClean) {
                lastClean = clean;
                clean = clean.replace(prefixRegex, "");
            }
            return clean.trim();
        }).filter(p => p);
        
        return cleanedParts.join(", ");
    } else {
        // No commas, do a global replacement of word-bounded prefixes
        let clean = query;
        clean = clean.replace(/^([0-9a-zA-Z-\/]+)\s+(đường|phố|ngõ|ngách|hẻm|kiệt)\s+/i, "$1 ");
        
        const globalPrefixRegex = /\b(thôn|ấp|xã|phường|p\.|quận|q\.|huyện|h\.|thị\s+xã|tx\.|tx|tỉnh|thành\s+phố|tp\.|tp|đường|phố|ngõ|ngách|hẻm|kiệt|số\s+nhà|số)\b/gi;
        clean = clean.replace(globalPrefixRegex, "");
        
        // Collapse spaces
        clean = clean.replace(/\s+/g, " ").trim();
        return clean;
    }
}

function mergeHouseNumber(searchQuery, resolvedAddress) {
    if (!searchQuery || !resolvedAddress) return resolvedAddress;
    
    // Split search query and parse the first part
    const searchParts = searchQuery.split(',').map(p => p.trim()).filter(p => p);
    if (searchParts.length === 0) return resolvedAddress;
    
    const parsedQuery = parseSearchQueryFirstPart(searchParts[0]);
    
    // Split resolved address into parts
    const resParts = resolvedAddress.split(',').map(p => p.trim()).filter(p => p);
    if (resParts.length === 0) return resolvedAddress;

    // Check if the query house number is a slash address (e.g. 814/29)
    if (parsedQuery.houseNum && parsedQuery.houseNum.includes('/')) {
        const queryAlley = parsedQuery.houseNum.split('/')[0];
        // If the resolved first part starts with "Hẻm [queryAlley]" or "Ngõ [queryAlley]"
        const regex = new RegExp(`^(hẻm|ngõ|ngách|kiệt)\\s+${queryAlley}\\b`, 'i');
        if (regex.test(resParts[0])) {
            // Replace "Hẻm 814 Sư Vạn Hạnh" with "814/29 Sư Vạn Hạnh"
            resParts[0] = resParts[0].replace(regex, parsedQuery.houseNum);
            return resParts.join(", ");
        }
    }
    
    const parsedRes = parseSearchQueryFirstPart(resParts[0]);
    
    // Compare the names (case-insensitive)
    if (parsedQuery.name && parsedRes.name && 
        parsedQuery.name.toLowerCase() === parsedRes.name.toLowerCase()) {
        
        // Reconstruct the first part using query's house number and prefix,
        // but preserving the resolved name's capitalization/spelling
        let houseStr = parsedQuery.houseNum ? (parsedQuery.houseNum + " ") : "";
        let prefixStr = parsedQuery.prefix ? (capitalizeWord(parsedQuery.prefix) + " ") : "";
        
        resParts[0] = houseStr + prefixStr + parsedRes.name;
        return resParts.join(", ");
    }
    
    // Fallback: If names don't match, apply the original house number merging logic
    if (parsedQuery.houseNum && /\d/.test(parsedQuery.houseNum)) {
        const houseNum = parsedQuery.houseNum;
        const firstPart = resParts[0];
        
        // Check if the resolved first part starts with a house number
        const resolvedHouseMatch = firstPart.match(/^([0-9a-zA-Z-\/]+)\s+/);
        if (resolvedHouseMatch) {
            const resolvedHouseNum = resolvedHouseMatch[1];
            if (/^\d/.test(resolvedHouseNum) && resolvedHouseNum !== houseNum) {
                // Replace the house number in the first part
                resParts[0] = firstPart.replace(resolvedHouseNum, houseNum);
                return resParts.join(", ");
            }
        }
        
        // If not matching, prepend the house number to the first part
        let startsWithHouseNum = firstPart.startsWith(houseNum + ",") || 
                                 firstPart.startsWith(houseNum + " ") || 
                                 firstPart === houseNum;
        if (!startsWithHouseNum) {
            resParts[0] = houseNum + " " + firstPart;
            return resParts.join(", ");
        }
    }
    
    return resolvedAddress;
}

function reverseGeocode(latlng) {
    const selectedTextEl = document.getElementById('selectedAddressText');
    const confirmBtnEl = document.getElementById('confirmAddressBtn');
    if (selectedTextEl) selectedTextEl.innerText = "Đang xác định địa chỉ...";
    if (confirmBtnEl) confirmBtnEl.disabled = true;

    const url = `https://nominatim.openstreetmap.org/reverse?format=json&lat=${latlng.lat}&lon=${latlng.lng}&zoom=18&addressdetails=1`;
    
    fetch(url, {
        headers: {
            'Accept-Language': 'vi,en;q=0.9'
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data) {
            let addr = buildCleanAddress(data);
            
            // Retrieve search query from input, or last search query (DO NOT fallback to old textarea address as it causes incorrect merge)
            const searchInputEl = document.getElementById('mapSearchInput');
            let searchQuery = (searchInputEl ? searchInputEl.value.trim() : "") || 
                             lastSearchQuery;
            
            if (searchQuery && searchLatLng) {
                let dist = L.latLng(latlng).distanceTo(searchLatLng);
                if (dist <= 2) {
                    addr = mergeHouseNumber(searchQuery, addr);
                } else {
                    searchLatLng = null; // Clear search reference if clicked/dragged far away
                }
            }
            
            selectedAddress = addr;
            if (selectedTextEl) selectedTextEl.innerText = selectedAddress;
            if (confirmBtnEl) confirmBtnEl.disabled = false;
            updateShippingInfo(latlng);
        } else {
            if (selectedTextEl) selectedTextEl.innerText = "Không thể xác định địa chỉ cụ thể.";
        }
    })
    .catch(err => {
        console.error("Reverse geocoding error:", err);
        if (selectedTextEl) selectedTextEl.innerText = "Không thể kết nối để lấy địa chỉ.";
    });
}

const mapSearchBtnEl = document.getElementById('mapSearchBtn');
if (mapSearchBtnEl) {
    mapSearchBtnEl.addEventListener('click', performSearch);
}
const mapSearchInputEl = document.getElementById('mapSearchInput');
if (mapSearchInputEl) {
    mapSearchInputEl.addEventListener('keypress', function(e) {
        if (e.key === 'Enter') {
            performSearch();
        }
    });
}

const huflitCampuses = [
    {
        display_name: "Trường Đại học Ngoại ngữ - Tin học Thành phố Hồ Chí Minh (Cơ sở Sư Vạn Hạnh)",
        lat: "10.775900",
        lon: "106.667511",
        address: {
            house_number: "828",
            road: "Sư Vạn Hạnh",
            suburb: "Phường Hòa Hưng",
            city: "Thành phố Hồ Chí Minh",
            province: "Thành phố Hồ Chí Minh"
        }
    },
    {
        display_name: "Trường Đại học Ngoại ngữ - Tin học (Cơ sở Hóc Môn)",
        lat: "10.865466",
        lon: "106.601012",
        address: {
            house_number: "806",
            road: "Lê Quang Đạo",
            suburb: "Xã Hóc Môn",
            city: "Thành phố Hồ Chí Minh",
            province: "Thành phố Hồ Chí Minh"
        }
    },
    {
        display_name: "Trường Đại học Ngoại ngữ - Tin học (Cơ sở Ba Gia)",
        lat: "10.785047",
        lon: "106.654629",
        address: {
            house_number: "52 - 70",
            road: "Ba Gia",
            suburb: "Phường Tân Sơn Nhất",
            city: "Thành phố Hồ Chí Minh",
            province: "Thành phố Hồ Chí Minh"
        }
    },
    {
        display_name: "Trường Đại học Ngoại ngữ - Tin học (Cơ sở Trường Sơn)",
        lat: "10.808955",
        lon: "106.664940",
        address: {
            house_number: "32",
            road: "Trường Sơn",
            suburb: "Phường Tân Sơn Hòa",
            city: "Thành phố Hồ Chí Minh",
            province: "Thành phố Hồ Chí Minh"
        }
    }
];

const bachkhoaCampuses = [
    {
        display_name: "Trường Đại học Bách Khoa TP.HCM (Cơ sở 1 - Lý Thường Kiệt)",
        lat: "10.772075",
        lon: "106.657902",
        address: {
            house_number: "268",
            road: "Lý Thường Kiệt",
            suburb: "Phường 14",
            city: "Thành phố Hồ Chí Minh",
            province: "Thành phố Hồ Chí Minh"
        }
    },
    {
        display_name: "Trường Đại học Bách khoa - ĐHQG TP.HCM (Cơ sở 2 - Khu phố Tân Lập, Đông Hòa)",
        lat: "10.88070777240946",
        lon: "106.80617814207669",
        address: {
            road: "Khu phố Tân Lập",
            suburb: "Phường Đông Hòa",
            city: "Thành phố Hồ Chí Minh",
            province: "Thành phố Hồ Chí Minh"
        }
    },
    {
        display_name: "Đại học Bách khoa Hà Nội (HUST)",
        lat: "21.006910",
        lon: "105.843397",
        address: {
            house_number: "1",
            road: "Đại Cồ Việt",
            suburb: "Bách Khoa",
            city: "Thành phố Hà Nội",
            province: "Thành phố Hà Nội"
        }
    }
];

const abbreviationMap = {
    'academy of cryptography techniques': 'Học viện Kỹ thuật mật mã',
    'academy of finance': 'Học viện Tài chính',
    'academy of journalism and communication': 'Học viện Báo chí và Tuyên truyền',
    'act': 'Học viện Kỹ thuật mật mã',
    'ajc': 'Học viện Báo chí và Tuyên truyền',
    'anh': 'Học viện An ninh Nhân dân',
    'ans': 'Trường Đại học An ninh nhân dân',
    'aof': 'Học viện Tài chính',
    'ba': 'Học viện Ngân hàng',
    'banking academy': 'Học viện Ngân hàng',
    'bka': 'Đại học Bách khoa Hà Nội',
    'bmu': 'Trường Đại học Y dược Buôn Ma Thuột',
    'bph': 'Học viện Biên phòng',
    'bvh': 'Học viện Công nghệ Bưu chính Viễn thông',
    'bvs': 'Học viện Công nghệ Bưu chính Viễn thông – Cơ sở TP.HCM',
    'bvu': 'Trường Đại học Bà Rịa - Vũng Tàu',
    'ccm': 'Trường Đại học Công nghiệp Dệt may Hà Nội',
    'cea': 'Trường Đại học Kinh tế Nghệ An',
    'cmc': 'Trường Đại học CMC',
    'csh': 'Học viện Cảnh sát Nhân dân',
    'css': 'Trường Đại học Cảnh sát nhân dân',
    'dad': 'Trường Đại học Đông Á',
    'dai nam university': 'Đại học Đại Nam',
    'dav': 'Học viện Ngoại giao',
    'dbd': 'Trường Đại học Bình Dương',
    'dbg': 'Trường Đại học Nông Lâm Bắc Giang',
    'dbh': 'Trường Đại học Quốc tế Bắc Hà',
    'dbl': 'Trường Đại học Bạc Liêu',
    'dca': 'Trường Đại học Chu Văn An',
    'dcd': 'Trường Đại học Công nghệ Đồng Nai',
    'dch': 'Trường Sĩ quan Đặc công',
    'dcl': 'Trường Đại học Cửu Long',
    'dcn': 'Trường Đại học Công nghiệp Hà Nội',
    'dcq': 'Trường Đại học Công nghệ và Quản lý Hữu nghị',
    'dct': 'Trường Đại học Công thương TP.HCM',
    'dcv': 'Trường Đại học Công nghiệp Vinh',
    'dda': 'Trường Đại học Công nghệ Đông Á',
    'ddb': 'Trường Đại học Thành Đông',
    'ddf': 'Trường Đại học Ngoại ngữ - Đại học Đà Nẵng',
    'ddg': 'Khoa Giáo dục Thể chất – Đại học Đà Nẵng',
    'ddk': 'Trường Đại học Bách Khoa - Đại học Đà Nẵng',
    'ddl': 'Trường Đại học Điện lực',
    'ddm': 'Trường Đại học Công nghiệp Quảng Ninh',
    'ddn': 'Trường Đại học Đại Nam',
    'ddp': 'Phân hiệu Đại học Đà Nẵng tại Kon Tum',
    'ddq': 'Trường Đại học Kinh tế - Đại học Đà Nẵng',
    'dds': 'Trường Đại học Sư phạm - Đại học Đà nẵng',
    'ddt': 'Đại học Duy Tân',
    'ddu': 'Trường Đại học Đông Đô',
    'ddv': 'Viện nghiên cứu đào tạo Việt - Anh - Đại học Đà Nẵng',
    'ddy': 'Khoa Y Dược – Đại học Đà Nẵng',
    'dfa': 'Trường Đại học Tài chính - Quản trị kinh doanh',
    'dha': 'Trường Đại học Luật - Đại học Huế',
    'dhc': 'Khoa Giáo dục Thể chất - Đại học Huế',
    'dhd': 'Trường Du lịch - Đại học Huế',
    'dhe': 'Khoa Kỹ thuật và công nghệ - Đại học Huế',
    'dhf': 'Trường Đại học Ngoại ngữ - Đại học Huế',
    'dhi': 'Khoa Quốc tế - Đại học Huế',
    'dhk': 'Trường Đại học Kinh tế - Đại học Huế',
    'dhl': 'Trường Đại học Nông Lâm - Đại học Huế',
    'dhn': 'Trường Đại học Nghệ thuật - Đại học Huế',
    'dhp': 'Trường Đại học Quản lý và công nghệ Hải Phòng',
    'dhq': 'Phân hiệu Đại học Huế tại quảng Trị',
    'dhs': 'Trường Đại học Sư phạm - Đại học Huế',
    'dht': 'Trường Đại học Khoa học - Đại học Huế',
    'dhv': 'Trường Đại học Hùng Vương TPHCM',
    'dhy': 'Trường Đại học Y Dược - Đại học Huế',
    'diplomatic academy of vietnam': 'Học viện Ngoại giao',
    'dkb': 'Trường Đại học Kinh tế Kỹ thuật Bình Dương',
    'dkc': 'Trường Đại học Công nghệ TP.HCM',
    'dkh': 'Trường Đại học Dược Hà Nội',
    'dkk': 'Trường Đại học Kinh tế Kỹ thuật Công nghiệp',
    'dks': 'Trường Đại học Kiểm sát Hà Nội',
    'dkt': 'Trường Đại học Hải Dương',
    'dky': 'Trường Đại học Kỹ thuật Y tế Hải Dương',
    'dla': 'Trường Đại học Kinh tế Công nghiệp Long An',
    'dlh': 'Trường Đại học Lạc Hồng',
    'dls': 'Trường Đại học Lao động Xã hội – Cơ sở TP.HCM',
    'dlx': 'Trường Đại học Lao động Xã hội',
    'dms': 'Trường Đại học Tài chính - Marketing',
    'dmt': 'Trường Đại học Tài nguyên và Môi trường Hà Nội',
    'dnb': 'Trường Đại học Hoa Lư',
    'dnc': 'Trường Đại học Nam Cần Thơ',
    'dnt': 'Trường Đại học Ngoại ngữ - Tin học TP.HCM',
    'dnu': 'Trường Đại học Đồng Nai',
    'dpc': 'Trường Đại học Phan Châu Trinh',
    'dpd': 'Trường Đại học Phương Đông',
    'dpq': 'Trường Đại học Phạm Văn Đồng',
    'dpt': 'Trường Đại học Phan Thiết',
    'dpx': 'Trường Đại học Dân lập Phú Xuân',
    'dpy': 'Trường Đại học Phú Yên',
    'dqb': 'Trường Đại học Quảng Bình',
    'dqk': 'Trường Đại học Kinh doanh và Công nghệ Hà Nội',
    'dqn': 'Trường Đại học Quy Nhơn',
    'dqt': 'Trường Đại học Quang Trung',
    'dqu': 'Trường Đại học Quảng Nam',
    'dsd': 'Trường Đại học Sân khấu Điện ảnh TP.HCM',
    'dsg': 'Trường Đại học Công nghệ Sài Gòn',
    'dsk': 'Trường Đại học Sư phạm Kỹ thuật – Đại học Đà Nẵng',
    'dtb': 'Trường Đại học Thái Bình',
    'dtc': 'Trường Đại học Công nghệ TT và Truyền thông (Đại học Thái Nguyên)',
    'dtd': 'Trường Đại học Tây Đô',
    'dte': 'Trường Đại học Kinh tế - Quản trị kinh doanh (Đại học Thái Nguyên)',
    'dtf': 'Trường Ngoại ngữ (Đại học Thái Nguyên)',
    'dtk': 'Trường Đại học Kỹ thuật Công nghiệp (Đại học Thái Nguyên)',
    'dtl': 'Trường Đại học Thăng Long',
    'dtm': 'Trường Đại học Tài nguyên và Môi trường TP.HCM',
    'dtn': 'Trường Đại học Nông lâm (Đại học Thái Nguyên)',
    'dtp': 'Phân hiệu Đại học Thái Nguyên tại Lào Cai',
    'dtq': 'Khoa Quốc tế (Đại học Thái Nguyên)',
    'dts': 'Trường Đại học Sư phạm (Đại học Thái Nguyên)',
    'dtt': 'Trường Đại học Tôn Đức Thắng',
    'dtv': 'Trường Đại học Lương Thế Vinh',
    'dty': 'Trường Đại học Y Dược (Đại học Thái Nguyên)',
    'dtz': 'Trường Đại học Khoa học (Đại học Thái Nguyên)',
    'dvb': 'Trường Đại học Việt Bắc',
    'dvd': 'Trường Đại học Văn hóa Thể thao và Du lịch Thanh Hóa',
    'dvh': 'Trường Đại học Văn Hiến',
    'dvl': 'Trường Đại học Văn Lang',
    'dvp': 'Trường Đại học Trưng Vương',
    'dvt': 'Trường Đại học Trà Vinh',
    'dvx': 'Trường Đại học Công nghệ Vạn Xuân',
    'dyd': 'Trường Đại học Yersin Đà Lạt',
    'eiu': 'Trường Đại học Quốc tế Miền Đông',
    'electric power university': 'Đại học Điện lực',
    'epu': 'Đại học Điện lực',
    'etu': 'Trường Đại học Hòa Bình',
    'fbu': 'Trường Đại học Tài chính Ngân hàng Hà Nội',
    'financial & banking university': 'Đại học Tài chính Ngân hàng',
    'financial and banking university': 'Đại học Tài chính Ngân hàng',
    'foreign trade university': 'Đại học Ngoại thương',
    'fpt': 'Trường Đại học FPT',
    'fpt university': 'Đại học FPT',
    'ftu': 'Đại học Ngoại thương',
    'gdu': 'Trường Đại học Gia Định',
    'gha': 'Trường Đại học Giao thông vận tải',
    'gnt': 'Trường Đại học Sư phạm Nghệ thuật Trung ương Hà Nội',
    'gsa': 'Trường Đại học Giao thông vận tải - Cơ sở 2',
    'gta': 'Trường Đại học Công nghệ Giao thông vận tải',
    'gts': 'Trường Đại học Giao thông vận tải TP.HCM',
    'hanoi academy of theatre and cinema': 'Đại học Sân khấu Điện ảnh',
    'hanoi architectural university': 'Đại học Kiến trúc Hà Nội',
    'hanoi law university': 'Đại học Luật Hà Nội',
    'hanoi medical university': 'Đại học Y Hà Nội',
    'hanoi national university of education': 'Đại học Sư phạm Hà Nội',
    'hanoi open university': 'Đại học Mở Hà Nội',
    'hanoi university': 'Đại học Hà Nội',
    'hanoi university of business and technology': 'Đại học Kinh doanh và Công nghệ',
    'hanoi university of culture': 'Đại học Văn hóa Hà Nội',
    'hanoi university of industry': 'Đại học Công nghiệp Hà Nội',
    'hanoi university of mining & geology': 'Đại học Mỏ Địa chất',
    'hanoi university of mining and geology': 'Đại học Mỏ Địa chất',
    'hanoi university of pharmacy': 'Đại học Dược Hà Nội',
    'hanoi university of public health': 'Đại học Y tế Công cộng',
    'hanoi university of science & technology': 'Đại học Bách khoa Hà Nội',
    'hanoi university of science and technology': 'Đại học Bách khoa Hà Nội',
    'hanoi university of transport & communications': 'Đại học Giao thông vận tải',
    'hanoi university of transport and communications': 'Đại học Giao thông vận tải',
    'hanu': 'Đại học Hà Nội',
    'hau': 'Đại học Kiến trúc Hà Nội',
    'haui': 'Đại học Công nghiệp Hà Nội',
    'hbt': 'Học viện Báo chí Tuyên truyền',
    'hca': 'Học viện Chính trị Công an Nhân dân',
    'hcb': 'Trường Đại học Kỹ thuật - Hậu cần Công an nhân dân phía Bắc',
    'hch': 'Học viện Hành chính Quốc gia',
    'hcmc university of technology': 'Trường Đại học Công nghệ',
    'hcn': 'Trường Đại học Kỹ thuật - Hậu cần Công an nhân dân phía Nam',
    'hcp': 'Học viện Chính sách và Phát triển',
    'hdt': 'Trường Đại học Hồng Đức',
    'heh': 'Học viện Hậu cần',
    'hgh': 'Trường Sĩ quan Phòng Hóa',
    'hha': 'Trường Đại học Hàng hải',
    'hhk': 'Học viện Hàng không Việt Nam',
    'hht': 'Trường Đại học Hà Tĩnh',
    'hiu': 'Trường Đại học Quốc tế Hồng Bàng',
    'hlu': 'Trường Đại học Hạ Long',
    'hmu': 'Đại học Y Hà Nội',
    'hnm': 'Trường Đại học Thủ đô Hà Nội',
    'hnue': 'Đại học Sư phạm Hà Nội',
    'ho chi minh city university of industry and trade': 'Trường Đại học Công thương',
    'hong bang international university': 'Trường Đại học Quốc tế Hồng Bàng',
    'hou': 'Đại học Mở Hà Nội',
    'hpn': 'Học viện Phụ nữ Việt Nam',
    'hqh': 'Học viện Hải Quân',
    'hqt': 'Học viện Ngoại giao',
    'hsu': 'Trường Đại học Hoa Sen',
    'hta': 'Học viện Tòa án',
    'htc': 'Học viện Tài chính',
    'htn': 'Học viện Thanh Thiếu niên Việt Nam',
    'hubt': 'Đại học Kinh doanh và Công nghệ',
    'huc': 'Đại học Văn hóa Hà Nội',
    'hueuni': 'Đại học HUẾ',
    'hufi': 'Trường Đại học Công thương',
    'huflit': 'Trường Ngoại Ngữ Tin Học',
    'huha': 'Đại học Nội vụ',
    'huit': 'Trường Đại học Công thương',
    'humg': 'Đại học Mỏ Địa chất',
    'hup': 'Đại học Dược Hà Nội',
    'huph': 'Đại học Y tế Công cộng',
    'hust': 'Đại học Bách khoa Hà Nội',
    'hutech': 'Trường Đại học Công nghệ',
    'hva': 'Học viện Âm nhạc Huế',
    'hvc': 'Học viện Cán bộ TP.HCM',
    'hvn': 'Học viện Nông nghiệp Việt Nam',
    'hvq': 'Học viện Quản lý Giáo dục',
    'hyd': 'Học viện Y Dược học cổ truyền Việt Nam',
    'industrial university': 'Trường Đại học Công nghiệp',
    'iuh': 'Trường Đại học Công nghiệp TP.HCM',
    'kcc': 'Trường Đại học Kỹ thuật Công nghệ Cần Thơ',
    'kcn': 'Trường Đại học Khoa học và Công nghệ Hà Nội',
    'kgh': 'Trường Sĩ quan không quân',
    'kha': 'Đại học Kinh tế Quốc dân',
    'kma': 'Học viện Kỹ thuật Mật mã',
    'kqh': 'Học viện Kỹ thuật Quân sự',
    'ksa': 'Đại học Kinh tế TP.HCM',
    'kta': 'Trường Đại học Kiến trúc Hà Nội',
    'ktd': 'Trường Đại học Kiến trúc Đà Nẵng',
    'kts': 'Trường Đại học Kiến trúc TP.HCM',
    'lah': 'Trường Sĩ quan Lục quân 1 (Đại học Trần Quốc Tuấn)',
    'lbh': 'Trường Sĩ quan Lục quân 2 (Đại học Nguyễn Huệ)',
    'lcdf': 'Học viện Thiết kế và Thời trang London',
    'lch': 'Trường Đại học Chính trị (Trường Sĩ quan Chính trị)',
    'lda': 'Trường Đại học Công đoàn',
    'lnh': 'Trường Đại học Lâm nghiệp',
    'lns': 'Trường Đại học Lâm nghiệp – Cơ sở 2',
    'london college of design & fashion': 'Học viện Thiết kế và Thời trang London',
    'london college of design and fashion': 'Học viện Thiết kế và Thời trang London',
    'lph': 'Trường Đại học Luật Hà Nội',
    'lps': 'Trường Đại học Luật TP.HCM',
    'mbs': 'Trường Đại học Mở TP.HCM',
    'mda': 'Trường Đại học Mỏ Địa chất Hà Nội',
    'mhn': 'Trường Đại học Mở Hà Nội',
    'military science academy': 'Học viện Khoa học Quân sự',
    'mit': 'Trường Đại học Công nghệ Miền Đông',
    'msa': 'Học viện Khoa học Quân sự',
    'mtc': 'Trường Đại học Mỹ thuật Công nghiệp',
    'mth': 'Trường Đại học Mỹ thuật Việt Nam',
    'mts': 'Trường Đại học Mỹ thuật TP.HCM',
    'mtu': 'Trường Đại học Xây dựng Miền Tây',
    'national economics university': 'Đại học Kinh tế Quốc dân',
    'national university of civil engineering': 'Đại học Xây dựng',
    'neu': 'Đại học Kinh tế Quốc dân',
    'nguyen tat thanh university': 'Trường Đại học Nguyễn Tất Thành',
    'nhf': 'Trường Đại học Hà Nội',
    'nhh': 'Học viện Ngân hàng',
    'nhs': 'Trường Đại học Ngân hàng TP.HCM',
    'nls': 'Trường Đại học Nông Lâm TP.HCM',
    'nlu': 'Trường Đại học Nông Lâm',
    'nong lam university': 'Trường Đại học Nông Lâm',
    'nqh': 'Học viện Khoa học Quân sự',
    'nth': 'Trường Đại học Ngoại thương',
    'nts': 'Trường Đại học Ngoại thương – Cơ sở phía Nam',
    'ntt': 'Trường Đại học Nguyễn Tất Thành',
    'nttu': 'Trường Đại học Nguyễn Tất Thành',
    'ntu': 'Trường Đại học Nguyễn Trãi',
    'nuce': 'Đại học Xây dựng',
    'nvh': 'Học viện Âm nhạc Quốc gia Việt Nam',
    'nvs': 'Nhạc viện TP.HCM',
    'pbh': 'Trường Sĩ quan Pháo binh',
    'pch': 'Trường Đại học Phòng cháy chữa cháy phía Bắc',
    'pcs': 'Trường Đại học Phòng cháy chữa cháy phía Nam',
    'pdu': 'Đại học Phương Đông',
    'pham ngoc thach university': 'Trường Đại học Y khoa Phạm Ngọc Thạch',
    'phuong dong university': 'Đại học Phương Đông',
    'pka': 'Đại học Phenikaa',
    'pkh': 'Học viện Phòng không - Không quân',
    'pntu': 'Trường Đại học Y khoa Phạm Ngọc Thạch',
    'posts and telecommunications institute of technology': 'Học viện Công nghệ Bưu chính Viễn thông',
    'ptit': 'Học viện Công nghệ Bưu chính Viễn thông',
    'pvu': 'Trường Đại học Dầu khí Việt Nam',
    'qhd': 'Trường Quản trị và kinh doanh (ĐHQG Hà Nội)',
    'qhe': 'Trường Đại học Kinh tế (ĐHQG Hà Nội)',
    'qhf': 'Trường Đại học Ngoại ngữ (ĐHQG Hà Nội)',
    'qhi': 'Trường Đại học Công nghệ (ĐHQG Hà Nội)',
    'qhl': 'Khoa Luật (ĐHQG Hà Nội)',
    'qhq': 'Khoa Quốc tế (ĐHQG Hà Nội)',
    'qhs': 'Trường Đại học Giáo dục (ĐHQG Hà Nội)',
    'qht': 'Trường Đại học Khoa học Tự nhiên (ĐHQG Hà Nội)',
    'qhx': 'Trường Đại học Khoa học Xã hội và Nhân văn (ĐHQG Hà Nội)',
    'qhy': 'Trường Đại học Y Dược (ĐHQG Hà Nội)',
    'qsb': 'Trường Đại học Bách Khoa - ĐHQG TP.HCM',
    'qsc': 'Trường Đại học Công nghệ Thông tin - Đại học Quốc gia TP.HCM',
    'qsk': 'Trường Đại học Kinh tế - Luật (Đại học Quốc gia TP.HCM)',
    'qsq': 'Trường Đại học Quốc tế - Đại học Quốc gia TP.HCM',
    'qst': 'Trường Đại học Khoa học Tự nhiên - ĐHQG TPHCM',
    'qsx': 'Trường Đại học Khoa học xã hội và Nhân văn - ĐHQG TP.HCM',
    'qsy': 'Trường Đại học Sức khỏe  - ĐHQG TP.HCM',
    'rmit': 'Trường Đại học RMIT',
    'royal melbourne institute of technology': 'Trường Đại học RMIT',
    'saigon technology university': 'Trường Đại học Công nghệ Sài Gòn',
    'saigon university': 'Trường Đại học Sài Gòn',
    'sdu': 'Trường Đại học Sao Đỏ',
    'sgd': 'Trường Đại học Sài Gòn',
    'sgu': 'Trường Đại học Sài Gòn',
    'siu': 'Trường Đại học Quốc tế Sài Gòn',
    'skd': 'Trường Đại học Sân khấu Điện ảnh',
    'skda': 'Đại học Sân khấu Điện ảnh',
    'skh': 'Trường Đại học Sư phạm Kỹ thuật Hưng Yên',
    'skn': 'Trường Đại học Sư phạm Kỹ thuật Nam Định',
    'skv': 'Trường Đại học Sư phạm Kỹ thuật Vinh',
    'snh': 'Trường Sĩ quan Công binh',
    'sp2': 'Trường Đại học Sư phạm Hà Nội 2',
    'spd': 'Trường Đại học Đồng Tháp',
    'sph': 'Trường Đại học Sư phạm Hà Nội',
    'spk': 'Trường Đại học Sư phạm Kỹ thuật TP.HCM',
    'sps': 'Trường Đại học Sư phạm TP.HCM',
    'sts': 'Trường Đại học Sư phạm TDTT TP.HCM',
    'stu': 'Trường Đại học Công nghệ Sài Gòn',
    'tag': 'Đại học Cần Thơ',
    'tbd': 'Trường Đại học Thái Bình Dương',
    'tct': 'Trường Đại học An Giang (ĐHQG HCM)',
    'tdb': 'Trường Đại học Thể dục Thể thao Bắc Ninh',
    'tdd': 'Trường Đại học Thành Đô',
    'tdh': 'Trường Đại học Sư phạm Thể dục thể thao Hà nội',
    'tdl': 'Trường Đại học Đà Lạt',
    'tdm': 'Trường Đại học Thủ Dầu Một',
    'tds': 'Trường Đại học Thể dục Thể thao TP.HCM',
    'tdtu': 'Trường Đại học Tôn Đức Thắng',
    'tdv': 'Trường Đại học Vinh',
    'tgh': 'Trường Sĩ quan Tăng - Thiết giáp',
    'thang long university': 'Đại học Thăng Long',
    'thp': 'Trường Đại học Hải Phòng',
    'thu': 'Trường Đại học Y khoa Tokyo Việt Nam',
    'thv': 'Trường Đại học Hùng Vương',
    'tkg': 'Trường Đại học Kiên Giang',
    'tla': 'Trường Đại học Thủy lợi',
    'tls': 'Trường Đại học Thủy lợi – Cơ sở 2',
    'tlu': 'Đại học Thăng Long',
    'tmu': 'Trường Đại học Thương mại',
    'tnu': 'Đại học Thái Nguyên',
    'ton duc thang university': 'Trường Đại học Tôn Đức Thắng',
    'tqu': 'Trường Đại học Tân Trào',
    'tsn': 'Trường Đại học Nha Trang',
    'ttb': 'Trường Đại học Tây Bắc',
    'ttd': 'Trường Đại học Thể dục Thể thao Đà Nẵng',
    'ttg': 'Trường Đại học Tiền Giang',
    'tth': 'Trường Sĩ quan Thông tin',
    'ttn': 'Trường Đại học Tây Nguyên',
    'ttu': 'Trường Đại học Tân Tạo',
    'tuu': 'Đại học Công đoàn',
    'tys': 'Trường Đại học Y khoa Phạm Ngọc Thạch',
    'uaf': 'Đại học Nông Lâm',
    'uah': 'Trường Đại học Kiến trúc',
    'udn': 'Đại học ĐÀ NẴNG',
    'uef': 'Trường Đại học Kinh tế - Tài chính TP.HCM',
    'ueh': 'Trường Đại học Kinh tế',
    'uet': 'Đại học Công nghệ',
    'ufa': 'Trường Đại học Tài chính Kế toán',
    'uif': 'Đại học Mỹ thuật Công nghiệp',
    'uit': 'Trường Đại học Công nghệ thông tin',
    'ukb': 'Trường Đại học Kinh Bắc',
    'ukh': 'Trường Đại học Khánh Hòa',
    'ulis': 'Đại học Ngoại ngữ',
    'ulsa': 'Đại học Lao động Xã hội',
    'ump': 'Đại học Y Dược',
    'umt': 'Trường Đại học Quản lý và công nghệ TPHCM',
    'university of agriculture & forestry': 'Đại học Nông Lâm',
    'university of agriculture and forestry': 'Đại học Nông Lâm',
    'university of economics': 'Trường Đại học Kinh tế',
    'university of economics ho chi minh city': 'Trường Đại học Kinh tế',
    'university of education': 'Đại học Giáo dục',
    'university of engineering and technology': 'Đại học Công nghệ',
    'university of home affairs': 'Đại học Nội vụ',
    'university of industrial fine art': 'Đại học Mỹ thuật Công nghiệp',
    'university of information technology': 'Trường Đại học Công nghệ thông tin',
    'university of labour & social affairs': 'Đại học Lao động Xã hội',
    'university of labour and social affairs': 'Đại học Lao động Xã hội',
    'university of languages & international studies': 'Đại học Ngoại ngữ',
    'university of languages and international studies': 'Đại học Ngoại ngữ',
    'university of medicine and pharmacy': 'Đại học Y Dược',
    'university of natural resources and environment': 'Đại học Tài nguyên và Môi trường',
    'university of science': 'Đại học Khoa học Tự nhiên',
    'university of social sciences & humanities': 'Đại học Khoa học Xã hội và Nhân văn',
    'university of social sciences and humanities': 'Đại học Khoa học Xã hội và Nhân văn',
    'university of technology': 'Trường Đại học Công nghệ',
    'unre': 'Đại học Tài nguyên và Môi trường',
    'utc': 'Đại học Giao thông vận tải',
    'van lang university': 'Trường Đại học Văn Lang',
    'vca': 'Học viện Tòa án',
    'vgu': 'Trường Đại học Việt – Đức',
    'vhd': 'Trường Đại học Công nghiệp Việt Hung',
    'vhh': 'Trường Đại học Văn hóa Hà Nội',
    'vhs': 'Trường Đại học Văn hóa TP.HCM',
    'vietnam court academy': 'Học viện Tòa án',
    'vietnam japan university': 'Đại học Việt Nhật',
    'vietnam maritime university': 'Đại học Hàng hải',
    'vietnam national academy of music': 'Học viện Âm nhạc Quốc gia',
    'vietnam national forestry university': 'Đại học Lâm nghiệp',
    'vietnam national university': 'Đại học Quốc gia',
    'vietnam national university of agriculture': 'Học viện Nông nghiệp Việt Nam',
    'vietnam trade union university': 'Đại học Công đoàn',
    'vietnam university of commerce': 'Đại học Thương mại',
    'vimaru': 'Đại học Hàng hải',
    'vinuni': 'Đại học VinUni',
    'vinuniversity': 'Đại học VinUni',
    'vju': 'Trường Đại học Việt Nhật (ĐHQG Hà Nội)',
    'vku': 'Khoa Công nghệ thông tin và Truyền thông – Đại học Đà Nẵng',
    'vlu': 'Trường Đại học Sư phạm Kỹ thuật Vĩnh Long',
    'vmu': 'Đại học Hàng hải',
    'vnam': 'Học viện Âm nhạc Quốc gia',
    'vnu': 'Đại học Quốc gia Hà Nội',
    'vnu – ued': 'Đại học Giáo dục',
    'vnu – us': 'Đại học Khoa học Tự nhiên',
    'vnu – ussh': 'Đại học Khoa học Xã hội và Nhân văn',
    'vnua': 'Học viện Nông nghiệp Việt Nam',
    'vnuf': 'Đại học Lâm nghiệp',
    'vnu-hcm': 'Đại học QUỐC GIA TP.HCM',
    'vnu-ued': 'Đại học Giáo dục',
    'vnu-us': 'Đại học Khoa học Tự nhiên',
    'vnu-ussh': 'Đại học Khoa học Xã hội và Nhân văn',
    'vph': 'Trường Sĩ quan Kỹ thuật QS Vinhempich (Đại học Trần Đại Nghĩa)',
    'vtt': 'Trường Đại học Võ Trường Toản',
    'vuc': 'Đại học Thương mại',
    'vui': 'Trường Đại học Công nghiệp Việt Trì',
    'xda': 'Trường Đại học Xây dựng Hà Nội',
    'xdt': 'Trường Đại học Xây dựng Miền Trung',
    'yct': 'Trường Đại học Y Dược Cần Thơ',
    'ydd': 'Trường Đại học Điều dưỡng Nam Định',
    'ydn': 'Trường Đại học Kỹ thuật Y Dược Đà Nẵng',
    'yds': 'Trường Đại học Y Dược TP.HCM',
    'yhb': 'Trường Đại học Y Hà Nội',
    'ykv': 'Trường Đại học Y khoa Vinh',
    'ypb': 'Trường Đại học Y Dược Hải Phòng',
    'yqh': 'Học viện Quân Y',
    'ytb': 'Trường Đại học Y Dược Thái Bình',
    'ytc': 'Trường Đại học Y tế Công cộng',
    'znh': 'Trường Đại học Văn hóa - Nghệ thuật Quân đội'
};


function geocodeAddress(query) {
    let cleaned = cleanSearchQuery(query);
    
    // Expand common abbreviations and English names to match OSM database entries
    for (const [abbr, fullName] of Object.entries(abbreviationMap)) {
        const escapedAbbr = abbr.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        // Use custom Unicode word boundary regex supporting Vietnamese accents (À-ỹ)
        const regex = new RegExp(`(^|[^a-zA-Z0-9_À-ỹ])(${escapedAbbr})($|[^a-zA-Z0-9_À-ỹ])`, 'gi');
        if (regex.test(cleaned)) {
            cleaned = cleaned.replace(regex, (match, p1, p2, p3) => p1 + fullName + p3);
            console.log(`Expanded abbreviation "${abbr}" to "${fullName}"`);
        }
    }

    const cleanedQuery = cleaned;
    let alleyQuery = null;
    const slashMatch = cleanedQuery.match(/^([0-9]+)\/[0-9/a-zA-Z-]*\s+(.*)$/);
    if (slashMatch) {
        const alleyNum = slashMatch[1];
        const restOfQuery = slashMatch[2];
        alleyQuery = `Hẻm ${alleyNum} ${restOfQuery}`;
    }

    function isGenericRoad(result) {
        if (!result) return true;
        const type = result.type;
        const classification = result.class;
        // Nominatim returns class=highway and type=tertiary/secondary/primary/trunk/motorway for main roads
        if (classification === 'highway' && ['tertiary', 'secondary', 'primary', 'trunk', 'motorway'].includes(type)) {
            return true;
        }
        return false;
    }

    function searchApi(searchStr, fallbackIfGeneric = false) {
        const url = `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(searchStr)}&limit=10&addressdetails=1`;
        return fetch(url, {
            headers: {
                'Accept-Language': 'vi,en;q=0.9'
            }
        })
        .then(response => response.json())
        .then(results => {
            if (results && results.length > 0) {
                let chosenResult = results[0];
                
                // If selectedLatLng is available, sort results by distance to selectedLatLng and choose the closest one
                if (selectedLatLng) {
                    let minDistance = Infinity;
                    let closestResult = null;
                    
                    for (const res of results) {
                        const resLatLng = L.latLng(parseFloat(res.lat), parseFloat(res.lon));
                        const distance = selectedLatLng.distanceTo(resLatLng);
                        if (distance < minDistance) {
                            minDistance = distance;
                            closestResult = res;
                        }
                    }
                    if (closestResult) {
                        chosenResult = closestResult;
                        console.log(`Prioritizing closest search result to selected location (dist: ${Math.round(minDistance)}m):`, chosenResult.display_name);
                    }
                }
                
                if (fallbackIfGeneric && isGenericRoad(chosenResult) && alleyQuery) {
                    console.log("Matched generic road. Falling back to alley search:", alleyQuery);
                    return null;
                }
                return chosenResult;
            }
            return null;
        });
    }

    return searchApi(cleanedQuery, true)
    .then(result => {
        if (!result && alleyQuery) {
            console.log("Trying alley search query:", alleyQuery);
            return searchApi(alleyQuery);
        }
        return result;
    })
    .then(result => {
        if (!result) {
            console.log("Cleaned search returned 0 results. Trying raw query:", query);
            return searchApi(query);
        }
        return result;
    })
    .then(result => {
        if (!result) {
            let cityFallback = query;
            if (!query.toLowerCase().includes("hồ chí minh") && !query.toLowerCase().includes("tphcm")) {
                cityFallback = query + ", Hồ Chí Minh";
                console.log("Raw search returned 0 results. Trying with city:", cityFallback);
                return searchApi(cityFallback);
            }
        }
        return result;
    })
    .then(result => {
        if (!result) {
            const parts = query.split(',').map(p => p.trim()).filter(p => p);
            if (parts.length >= 3) {
                const firstPartClean = cleanSearchQuery(parts[0]);
                const lastPartClean = cleanSearchQuery(parts[parts.length - 1]);
                const fallbackQuery = firstPartClean + ", " + lastPartClean;
                console.log("Trying parts split fallback:", fallbackQuery);
                return searchApi(fallbackQuery);
            }
        }
        return result;
    });
}

function isLandmarkQuery(query) {
    if (!query) return false;
    const q = query.toLowerCase().trim();
    
    // Check if it matches any abbreviation in abbreviationMap (with word boundaries)
    for (const abbr of Object.keys(abbreviationMap)) {
        const escapedAbbr = abbr.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        const regex = new RegExp(`(^|[^a-zA-Z0-9_À-ỹ])(${escapedAbbr})($|[^a-zA-Z0-9_À-ỹ])`, 'i');
        if (regex.test(q)) {
            return true;
        }
    }
    
    // Landmark keywords (tên địa điểm/địa danh có chi nhánh)
    const landmarkKeywords = [
        "circle k", "circlek", "gs25", "gs 25", "familymart", "family mart", "7-eleven", "7 eleven", "7eleven", "ministop",
        "winmart", "coopmart", "co.opmart", "lottemart", "aeon", "big c", "bigc", "go!", "go !",
        "trường", "đại học", "học viện", "thpt", "thcs", "tiểu học", "mầm non", "colleg", "univers", "school",
        "bệnh viện", "clinic", "hospital", "phòng khám",
        "chợ", "siêu thị", "tttm", "plaza", "mall",
        "công viên", "park",
        "nhà thờ", "chùa", "temple", "church",
        "chung cư", "tòa nhà", "building", "apartment", "condo", "residence",
        "khách sạn", "hotel", "resort",
        "nhà hàng", "quán", "cafe", "cà phê", "circle-k"
    ];
    
    for (const kw of landmarkKeywords) {
        if (q.includes(kw)) {
            return true;
        }
    }
    
    // If it doesn't look like a standard address (e.g. no numbers and very short)
    const hasNumber = /\d/.test(q);
    const words = q.split(/\s+/);
    if (!hasNumber && words.length <= 4) {
        return true;
    }
    
    return false;
}

function extractProvince(address) {
    if (!address) return "";
    const addr = address.toLowerCase();
    
    // List of standard Vietnamese provinces/cities and their common variants
    const provinces = [
        { name: "Hồ Chí Minh", keywords: ["hồ chí minh", "hcm", "tp.hcm", "tphcm", "sài gòn", "sai gon"] },
        { name: "Hà Nội", keywords: ["hà nội", "ha noi", "hn"] },
        { name: "Bình Dương", keywords: ["bình dương", "binh duong"] },
        { name: "Đồng Nai", keywords: ["đồng nai", "dong nai"] },
        { name: "Bà Rịa - Vũng Tàu", keywords: ["bà rịa", "vũng tàu", "brvt", "ba ria", "vung tau"] },
        { name: "Tây Ninh", keywords: ["tây ninh", "tay ninh"] },
        { name: "Bình Phước", keywords: ["bình phước", "binh phuoc"] },
        { name: "Long An", keywords: ["long an"] },
        { name: "Tiền Giang", keywords: ["tiền giang", "tien giang"] },
        { name: "Bến Tre", keywords: ["bến tre", "ben tre"] },
        { name: "Vĩnh Long", keywords: ["vĩnh long", "vinh long"] },
        { name: "Đồng Tháp", keywords: ["đồng tháp", "dong thap"] },
        { name: "An Giang", keywords: ["an giang"] },
        { name: "Kiên Giang", keywords: ["kiên giang", "kien giang"] },
        { name: "Cần Thơ", keywords: ["cần thơ", "can tho"] },
        { name: "Hậu Giang", keywords: ["hậu giang", "hau giang"] },
        { name: "Sóc Trăng", keywords: ["sóc trăng", "soc trang"] },
        { name: "Trà Vinh", keywords: ["trà vinh", "tra vinh"] },
        { name: "Bạc Liêu", keywords: ["bạc liêu", "bac lieu"] },
        { name: "Cà Mau", keywords: ["cà mau", "ca mau"] },
        { name: "Khánh Hòa", keywords: ["khánh hòa", "khanh hoa", "nha trang"] },
        { name: "Phú Yên", keywords: ["phú yên", "phu yen", "tuy hòa", "tuy hoa"] },
        { name: "Ninh Thuận", keywords: ["ninh thuận", "ninh thuan", "phan rang"] },
        { name: "Bình Thuận", keywords: ["bình thuận", "binh thuan", "phan thiết", "phan thiet"] },
        { name: "Đắk Lắk", keywords: ["đắk lắk", "dak lak", "đắc lắc", "buôn ma thuột", "bmt"] },
        { name: "Đắk Nông", keywords: ["đắk nông", "dak nong", "đắc nông"] },
        { name: "Lâm Đồng", keywords: ["lâm đồng", "lam dong", "đà lạt", "da lat"] },
        { name: "Gia Lai", keywords: ["gia lai", "pleiku"] },
        { name: "Kon Tum", keywords: ["kon tum", "kontum"] },
        { name: "Bình Định", keywords: ["bình định", "binh dinh", "quy nhơn", "quy nhon"] },
        { name: "Quảng Ngãi", keywords: ["quảng ngãi", "quang ngai"] },
        { name: "Quảng Nam", keywords: ["quảng nam", "quang nam", "hội an", "hoi an"] },
        { name: "Đà Nẵng", keywords: ["đà nẵng", "da nang"] },
        { name: "Thừa Thiên Huế", keywords: ["thừa thiên huế", "thừa thiên", "huế", "hue"] },
        { name: "Quảng Trị", keywords: ["quảng trị", "quang tri"] },
        { name: "Quảng Bình", keywords: ["quảng bình", "quang binh"] },
        { name: "Thanh Hóa", keywords: ["thanh hóa", "thanh hoa"] },
        { name: "Nghệ An", keywords: ["nghệ an", "nghe an", "vinh"] },
        { name: "Hà Tĩnh", keywords: ["hà tĩnh", "ha tinh"] },
        { name: "Ninh Bình", keywords: ["ninh bình", "ninh binh"] },
        { name: "Hưng Yên", keywords: ["hưng yên", "hung yen"] },
        { name: "Hải Phòng", keywords: ["hải phòng", "hai phong"] },
        { name: "Bắc Ninh", keywords: ["bắc ninh", "bac ninh"] },
        { name: "Quảng Ninh", keywords: ["quảng ninh", "quang ninh", "hạ long", "ha long"] }
    ];
    
    for (const p of provinces) {
        for (const kw of p.keywords) {
            if (addr.includes(kw)) {
                return p.name;
            }
        }
    }
    
    const parts = address.split(',');
    if (parts.length > 0) {
        return parts[parts.length - 1].trim();
    }
    
    return "";
}

function searchLandmarkBranches(query, province) {
    const qLower = query.toLowerCase().trim();
    if (qLower === "huflit" || qLower.includes("ngoại ngữ tin học") || qLower.includes("đại học ngoại ngữ tin học")) {
        console.log("Intercepting HUFLIT query, returning correct campuses.");
        if (province && !province.toLowerCase().includes("hồ chí minh") && !province.toLowerCase().includes("hcm")) {
            return Promise.resolve([]);
        }
        return Promise.resolve(JSON.parse(JSON.stringify(huflitCampuses)));
    }
    
    // Interceptor cho Bách Khoa - trả về danh sách cơ sở chính xác
    if (qLower === "bách khoa" || qLower === "bach khoa" || qLower === "bk" ||
        qLower.includes("đại học bách khoa") || qLower.includes("dai hoc bach khoa")) {
        console.log("Intercepting Bách Khoa query, returning correct campuses.");
        // Nếu có địa chỉ hồ sơ thuộc HCM thì chỉ trả về 2 cơ sở HCM
        if (province && (province.toLowerCase().includes("hồ chí minh") || province.toLowerCase().includes("hcm"))) {
            return Promise.resolve(JSON.parse(JSON.stringify(bachkhoaCampuses.slice(0, 2))));
        }
        // Nếu có địa chỉ hồ sơ thuộc Hà Nội thì chỉ trả về cơ sở HN
        if (province && (province.toLowerCase().includes("hà nội") || province.toLowerCase().includes("ha noi"))) {
            return Promise.resolve([JSON.parse(JSON.stringify(bachkhoaCampuses[2]))]);
        }
        // Không có địa chỉ hồ sơ: trả về cả ba cơ sở
        return Promise.resolve(JSON.parse(JSON.stringify(bachkhoaCampuses)));
    }
    
    let cleaned = cleanSearchQuery(query);
    
    // Expand abbreviations
    for (const [abbr, fullName] of Object.entries(abbreviationMap)) {
        const escapedAbbr = abbr.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        const regex = new RegExp(`(^|[^a-zA-Z0-9_À-ỹ])(${escapedAbbr})($|[^a-zA-Z0-9_À-ỹ])`, 'gi');
        if (regex.test(cleaned)) {
            cleaned = cleaned.replace(regex, (match, p1, p2, p3) => p1 + fullName + p3);
        }
    }
    
    let searchStr = cleaned;
    if (province && !cleaned.toLowerCase().includes(province.toLowerCase())) {
        searchStr = `${cleaned}, ${province}`;
    }
    
    const url = `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(searchStr)}&limit=15&addressdetails=1`;
    return fetch(url, {
        headers: {
            'Accept-Language': 'vi,en;q=0.9'
        }
    })
    .then(res => res.json());
}

function deduplicateSearchResults(results) {
    if (!results) return [];
    const seenAddresses = new Set();
    const seenCoords = new Set();
    const uniqueResults = [];
    
    results.forEach(res => {
        // Chuẩn hóa địa chỉ được định dạng sạch bằng buildCleanAddress(res) để so sánh
        const address = buildCleanAddress(res);
        const normalizedAddr = address.toLowerCase().replace(/\s+/g, "");
        
        // Chuẩn hóa tọa độ để so sánh (làm tròn 4 chữ số thập phân ~11m)
        const lat = parseFloat(res.lat).toFixed(4);
        const lon = parseFloat(res.lon).toFixed(4);
        const coordSig = `${lat}_${lon}`;
        
        if (!seenAddresses.has(normalizedAddr) && !seenCoords.has(coordSig)) {
            seenAddresses.add(normalizedAddr);
            seenCoords.add(coordSig);
            uniqueResults.push(res);
        } else {
            console.log("Duplicate result filtered out:", res.display_name);
        }
    });
    return uniqueResults;
}

function showBranchSearchResults(results, query) {
    const listEl = document.getElementById('branchSearchResults');
    if (!listEl) return;
    
    listEl.innerHTML = "";
    
    if (!results || results.length === 0) {
        listEl.innerHTML = `<div class="p-3 text-muted text-center small">Không tìm thấy chi nhánh nào cho "${query}"</div>`;
        listEl.style.display = "block";
        return;
    }
    
    results.forEach(res => {
        const itemEl = document.createElement('div');
        itemEl.className = "branch-search-item";
        
        let name = res.display_name.split(',')[0].trim();
        let address = buildCleanAddress(res);
        
        itemEl.innerHTML = `
            <div>
                <i class="bi bi-geo-alt-fill"></i>
            </div>
            <div class="text-start">
                <div class="branch-name-display">${name}</div>
                <div class="text-muted branch-address-display" style="font-size: 12px;">${address}</div>
            </div>
        `;
        
        itemEl.addEventListener('click', () => {
            const latlng = L.latLng(parseFloat(res.lat), parseFloat(res.lon));
            searchLatLng = latlng;
            if (map) map.setView(latlng, 16);
            
            let addr = buildCleanAddress(res);
            addr = mergeHouseNumber(query, addr);
            
            selectedAddress = addr;
            const selectedTextEl = document.getElementById('selectedAddressText');
            if (selectedTextEl) selectedTextEl.innerText = selectedAddress;
            
            const confirmBtnEl = document.getElementById('confirmAddressBtn');
            if (confirmBtnEl) confirmBtnEl.disabled = false;
            
            updateMarker(latlng, true);
            
            listEl.style.display = "none";
        });
        
        listEl.appendChild(itemEl);
    });
    
    listEl.style.display = "block";
}

function performSearch() {
    const searchInputEl = document.getElementById('mapSearchInput');
    const selectedTextEl = document.getElementById('selectedAddressText');
    const confirmBtnEl = document.getElementById('confirmAddressBtn');
    const listEl = document.getElementById('branchSearchResults');
    
    const query = searchInputEl ? searchInputEl.value.trim() : "";
    if (!query) return;

    lastSearchQuery = query; // Save search query
    if (selectedTextEl) selectedTextEl.innerText = "Đang tìm kiếm...";
    if (listEl) listEl.style.display = "none";
    
    const savedAddress = window.shippingConfig && window.shippingConfig.savedProfileAddress;
    const isLandmark = isLandmarkQuery(query);
    
    if (isLandmark) {
        const province = savedAddress ? extractProvince(savedAddress) : "";
        if (province) {
            console.log(`Landmark search detected for "${query}" restricted to province "${province}".`);
            searchLandmarkBranches(query, province)
            .then(results => {
                const uniqueResults = deduplicateSearchResults(results);
                if (uniqueResults && uniqueResults.length > 0) {
                    showBranchSearchResults(uniqueResults, query);
                    if (selectedTextEl) selectedTextEl.innerText = `Tìm thấy ${uniqueResults.length} địa điểm tại ${province}. Vui lòng chọn bên dưới:`;
                } else {
                    // Fallback to global landmark search and show dropdown
                    searchLandmarkBranches(query, "")
                    .then(globalResults => {
                        const uniqueGlobal = deduplicateSearchResults(globalResults);
                        if (uniqueGlobal && uniqueGlobal.length > 0) {
                            showBranchSearchResults(uniqueGlobal, query);
                            if (selectedTextEl) selectedTextEl.innerText = `Không tìm thấy chi nhánh tại ${province}. Gợi ý các địa điểm khác:`;
                        } else {
                            if (selectedTextEl) selectedTextEl.innerText = "Không tìm thấy địa điểm này. Vui lòng thử lại.";
                            showBranchSearchResults([], query);
                        }
                    })
                    .catch(err => {
                        console.error("Global fallback search error:", err);
                        if (selectedTextEl) selectedTextEl.innerText = "Không tìm thấy địa điểm này. Vui lòng thử lại.";
                    });
                }
            })
            .catch(err => {
                console.error("Landmark province search error:", err);
                if (selectedTextEl) selectedTextEl.innerText = "Lỗi kết nối khi tìm kiếm chi nhánh.";
            });
        } else {
            console.log(`Landmark search detected for "${query}" with no saved address. Searching globally...`);
            searchLandmarkBranches(query, "")
            .then(results => {
                const uniqueResults = deduplicateSearchResults(results);
                if (uniqueResults && uniqueResults.length > 0) {
                    showBranchSearchResults(uniqueResults, query);
                    if (selectedTextEl) selectedTextEl.innerText = `Tìm thấy ${uniqueResults.length} địa điểm. Vui lòng chọn bên dưới:`;
                } else {
                    if (selectedTextEl) selectedTextEl.innerText = "Không tìm thấy địa điểm này. Vui lòng thử lại.";
                    showBranchSearchResults([], query);
                }
            })
            .catch(err => {
                console.error("Global landmark search error:", err);
                if (selectedTextEl) selectedTextEl.innerText = "Lỗi kết nối khi tìm kiếm.";
            });
        }
        return;
    }
    
    // Normal query (specific address)
    geocodeAddress(query)
    .then(result => {
        if (result) {
            const latlng = L.latLng(parseFloat(result.lat), parseFloat(result.lon));
            searchLatLng = latlng; // Save the searched latlng reference
            if (map) map.setView(latlng, 16);
            
            let addr = buildCleanAddress(result);
            addr = mergeHouseNumber(query, addr);
            
            selectedAddress = addr;
            if (selectedTextEl) selectedTextEl.innerText = selectedAddress;
            if (confirmBtnEl) confirmBtnEl.disabled = false;

            // Place marker but skip reverse geocoding to preserve user searched text
            updateMarker(latlng, true);
        } else {
            if (selectedTextEl) selectedTextEl.innerText = "Không tìm thấy địa điểm này. Vui lòng thử lại.";
        }
    })
    .catch(err => {
        console.error("Search error:", err);
        if (selectedTextEl) selectedTextEl.innerText = "Lỗi kết nối khi tìm kiếm.";
    });
}

function formatCurrency(amount) {
    if (amount === 0) return "Miễn phí";
    return amount.toLocaleString('vi-VN') + " đ";
}

function calculateShipping(distance) {
    const config = window.shippingConfig || { hasFreshFood: false, subtotalAmount: 0, discountAmount: 0, baseFinalTotal: 0 };
    
    // Check eligibility for Freeship within 3km
    let isEligibleForFreeship = false;
    if (distance <= 3) {
        if (config.hasFreshFood && config.subtotalAmount >= 150000) {
            isEligibleForFreeship = true;
        } else if (config.subtotalAmount >= 300000) {
            isEligibleForFreeship = true;
        }
    }
    
    // Calculate surcharge for distance > 3km
    let surcharge = 0;
    if (distance > 3) {
        surcharge = Math.ceil(distance - 3) * 4000;
    }
    
    // Base fees for each method
    const baseFees = {
        "Instant": 15000,
        "Hourly": 12000,
        "4Hours": 9000,
        "Economy": 5000
    };
    
    const results = {};
    for (let method in baseFees) {
        let base = isEligibleForFreeship ? 0 : baseFees[method];
        results[method] = base + surcharge;
    }
    
    return {
        results: results,
        isEligibleForFreeship: isEligibleForFreeship,
        surcharge: surcharge
    };
}

const restrictionZones = [
    {
        name: "Đường Nam Kỳ Khởi Nghĩa (Cấm xe máy/phương tiện theo khung giờ 9h-16h)",
        center: [10.7850, 106.6830], // Nam Kỳ Khởi Nghĩa / Nguyễn Văn Trỗi, Quận 3, TP.HCM
        radius: 400, // meters
        startHour: 9,
        endHour: 16,
        type: "ban"
    },
    {
        name: "Phố đi bộ Hồ Gươm (Cấm phương tiện vào cuối tuần)",
        center: [21.0280, 105.8520], // Phố đi bộ Hồ Gươm, Hà Nội
        radius: 400, // meters
        isWeekendOnly: true,
        type: "ban"
    },
    {
        name: "Điểm kẹt xe giờ cao điểm Cộng Hòa (Hồ Chí Minh)",
        center: [10.8020, 106.6520], // Đường Cộng Hòa, Tân Bình
        radius: 500,
        rushHourOnly: true,
        type: "traffic"
    },
    {
        name: "Điểm kẹt xe giờ cao điểm Vòng xoay Hàng Xanh (Hồ Chí Minh)",
        center: [10.8016, 106.7118], // Hàng Xanh, Bình Thạnh
        radius: 500,
        rushHourOnly: true,
        type: "traffic"
    },
    {
        name: "Điểm kẹt xe giờ cao điểm Trường Chinh (Hồ Chí Minh)",
        center: [10.8140, 106.6370], // Trường Chinh, Tân Bình
        radius: 500,
        rushHourOnly: true,
        type: "traffic"
    },
    {
        name: "Điểm kẹt xe giờ cao điểm Nguyễn Trãi (Hà Nội)",
        center: [20.9980, 105.8120], // Nguyễn Trãi, Thanh Xuân
        radius: 600,
        rushHourOnly: true,
        type: "traffic"
    },
    {
        name: "Điểm kẹt xe giờ cao điểm Cầu Giấy (Hà Nội)",
        center: [21.0330, 105.7950], // Cầu Giấy
        radius: 500,
        rushHourOnly: true,
        type: "traffic"
    }
];

function evaluateRoutes(routes) {
    const now = new Date();
    const currentHour = now.getHours();
    const currentDay = now.getDay();
    const isWeekend = (currentDay === 0 || currentDay === 6);
    
    // Giờ cao điểm tại Việt Nam: 7h-9h và 17h-19h
    const isRushHour = (currentHour >= 7 && currentHour <= 9) || (currentHour >= 17 && currentHour <= 19);
    
    let bestRoute = routes[0];
    let minScore = Infinity;
    
    routes.forEach((route, index) => {
        let score = route.distance; // Điểm phạt cơ sở là chiều dài đường bộ tính bằng mét
        let hasBan = false;
        let trafficCount = 0;
        
        const coords = route.geometry.coordinates; // OSRM trả về mảng các [lng, lat]
        
        for (let i = 0; i < coords.length; i++) {
            const pt = L.latLng(coords[i][1], coords[i][0]); // [lat, lng]
            
            for (let j = 0; j < restrictionZones.length; j++) {
                const zone = restrictionZones[j];
                const dist = pt.distanceTo(L.latLng(zone.center));
                
                if (dist <= zone.radius) {
                    if (zone.type === "ban") {
                        if (zone.isWeekendOnly && isWeekend) {
                            hasBan = true;
                        } else if (zone.startHour !== undefined && zone.endHour !== undefined) {
                            if (currentHour >= zone.startHour && currentHour <= zone.endHour) {
                                hasBan = true;
                            }
                        }
                    } else if (zone.type === "traffic") {
                        if (zone.rushHourOnly && isRushHour) {
                            trafficCount++;
                        }
                    }
                }
            }
        }
        
        if (hasBan) {
            score += 100000; // Phạt nặng nếu đi vào đường cấm giờ
        }
        score += trafficCount * 500; // Phạt nhẹ cho mỗi đoạn đi qua điểm kẹt xe giờ cao điểm
        
        route.penaltyScore = score;
        route.hasBan = hasBan;
        route.trafficCount = trafficCount;
        
        console.log(`Tuyến đường ${index + 1}: dài ${route.distance}m, Phạt cấm: ${hasBan}, Số điểm kẹt xe: ${trafficCount}, Tổng điểm phạt: ${score}`);
        
        if (score < minScore) {
            minScore = score;
            bestRoute = route;
        }
    });
    
    return bestRoute;
}

function updateShippingInfo(latlng) {
    if (!latlng) return;
    
    const config = window.shippingConfig || { hasFreshFood: false, subtotalAmount: 0, discountAmount: 0, baseFinalTotal: 0, isBranchExplicitlyChosen: false };
    
    if (config.isBranchExplicitlyChosen && config.activeBranchAddress) {
        // Geocode chosen branch address
        const query = config.activeBranchAddress;
        const geocodeUrl = `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&limit=1`;
        
        fetch(geocodeUrl)
            .then(res => res.json())
            .then(results => {
                let storeLatLng = null;
                if (results && results.length > 0) {
                    storeLatLng = L.latLng(parseFloat(results[0].lat), parseFloat(results[0].lon));
                } else {
                    storeLatLng = L.latLng(10.031000, 105.768000);
                }
                
                activeStoreLatLng = storeLatLng;
                activeStoreName = config.activeBranchName || "Bách Hóa Xanh (Đã chọn)";
                
                // Truyền địa chỉ thực từ DB để hiển thị đúng, không cần reverse-geocode
                calculateAndProceed(latlng, activeStoreLatLng, activeStoreName, { address: config.activeBranchAddress });
            })
            .catch(err => {
                console.error("Geocoding failed, falling back:", err);
                activeStoreLatLng = L.latLng(10.031000, 105.768000);
                activeStoreName = config.activeBranchName || "Bách Hóa Xanh (Đã chọn)";
                calculateAndProceed(latlng, activeStoreLatLng, activeStoreName, { address: config.activeBranchAddress });
            });
    } else {
        // Find nearest branch
        fetchNearestRealBranch(latlng, selectedAddress, (nearestBranch) => {
            activeStoreLatLng = nearestBranch.latlng;
            activeStoreName = nearestBranch.name;
            
            // Resolve nearest branch on server side to auto-update active store
            resolveNearestBranchOnServer(selectedAddress, nearestBranch.name);
            
            calculateAndProceed(latlng, activeStoreLatLng, activeStoreName, nearestBranch);
        });
    }
}

function resolveNearestBranchOnServer(addressText, nearestBranchName) {
    if (!addressText) return;
    const formData = new FormData();
    formData.append("addressText", addressText);
    if (nearestBranchName) {
        formData.append("nearestBranchName", nearestBranchName);
    }
    
    fetch("/Home/ResolveNearestBranch", {
        method: "POST",
        body: formData
    })
    .then(res => res.json())
    .then(data => {
        if (data.success) {
            console.log("Automatically resolved and updated active branch to nearest:", data.branchName);
            const activeText = document.getElementById("activeBranchText");
            if (activeText) activeText.textContent = data.branchName;
            
            // Lưu thông tin chi nhánh thực từ database
            window._resolvedBranch = { name: data.branchName, address: data.address };
            isStockVerified = false; // Reset stock verification when branch changes
            
            // Cập nhật ngay lập tức hiển thị chi nhánh giao hàng gần nhất
            const branchNameEl = document.getElementById('nearestBranchName');
            const branchAddrEl = document.getElementById('nearestBranchAddress');
            const branchAddrTextEl = document.getElementById('nearestBranchAddressText');
            if (branchNameEl) branchNameEl.innerText = data.branchName;
            if (branchAddrEl && branchAddrTextEl && data.address) {
                branchAddrTextEl.innerText = data.address;
                branchAddrEl.style.display = "block";
            }
        }
    })
    .catch(err => console.error("Error resolving nearest branch on server:", err));
}

function calculateAndProceed(latlng, storeLatLng, storeName, nearestBranch) {
    const config = window.shippingConfig || { hasFreshFood: false, subtotalAmount: 0, discountAmount: 0, baseFinalTotal: 0 };
    const straightLineDistance = latlng.distanceTo(storeLatLng) / 1000.0;
    
    const osrmUrl = `https://router.project-osrm.org/route/v1/driving/${latlng.lng},${latlng.lat};${storeLatLng.lng},${storeLatLng.lat}?overview=full&geometries=geojson&alternatives=true`;
    
    fetch(osrmUrl)
        .then(res => res.json())
        .then(data => {
            let chosenRoute = null;
            let distance = straightLineDistance;
            let routeCoords = null;
            
            if (data && data.code === "Ok" && data.routes && data.routes.length > 0) {
                chosenRoute = evaluateRoutes(data.routes);
                distance = chosenRoute.distance / 1000.0;
                routeCoords = chosenRoute.geometry.coordinates.map(coord => [coord[1], coord[0]]);
                console.log(`Đã chọn tuyến đường bộ tốt nhất. Khoảng cách: ${distance.toFixed(2)} km`);
            } else {
                console.log("OSRM routing failed. Fallback to straight-line distance.");
            }
            
            proceedWithShippingDetails(distance, routeCoords);
        })
        .catch(err => {
            console.error("Error fetching OSRM routing:", err);
            proceedWithShippingDetails(straightLineDistance, null);
        });
        
    function proceedWithShippingDetails(distance, routeCoords) {
        // Set hidden fields
        const latInput = document.getElementById('latitudeInput');
        const lngInput = document.getElementById('longitudeInput');
        const distInput = document.getElementById('shippingDistanceInput');
        if (latInput) latInput.value = latlng.lat;
        if (lngInput) lngInput.value = latlng.lng;
        if (distInput) distInput.value = distance.toFixed(2);
        
        // Update UI text
        const distText = document.getElementById('shippingDistanceText');
        if (distText) distText.innerText = distance.toFixed(2) + " km";
        
        const branchInfoEl = document.getElementById('nearestBranchInfo');
        const branchNameEl = document.getElementById('nearestBranchName');
        const branchAddrEl = document.getElementById('nearestBranchAddress');
        const branchAddrTextEl = document.getElementById('nearestBranchAddressText');
        if (branchInfoEl && branchNameEl) {
            branchInfoEl.style.display = "block";
            
            // Ưu tiên hiển thị tên và địa chỉ thực từ database (đã phân giải từ server)
            if (window._resolvedBranch) {
                branchNameEl.innerText = window._resolvedBranch.name;
                if (branchAddrEl && branchAddrTextEl && window._resolvedBranch.address) {
                    branchAddrTextEl.innerText = window._resolvedBranch.address;
                    branchAddrEl.style.display = "block";
                }
            } else {
                branchNameEl.innerText = storeName;
                if (nearestBranch && nearestBranch.address && branchAddrEl && branchAddrTextEl) {
                    const addrParts = nearestBranch.address.split(',').map(p => p.trim()).filter(p => p);
                    const shortAddr = addrParts.slice(0, 4).join(', ');
                    branchAddrTextEl.innerText = shortAddr;
                    branchAddrEl.style.display = "block";
                } else if (branchAddrEl && branchAddrTextEl) {
                const storeLat = storeLatLng.lat;
                const storeLng = storeLatLng.lng;
                const cacheKey = `addr_${storeLat.toFixed(4)}_${storeLng.toFixed(4)}`;
                
                if (window._branchAddrCache && window._branchAddrCache[cacheKey]) {
                    branchAddrTextEl.innerText = window._branchAddrCache[cacheKey];
                    branchAddrEl.style.display = "block";
                } else {
                    branchAddrTextEl.innerText = "Đang tải địa chỉ...";
                    branchAddrEl.style.display = "block";
                    
                    const reverseUrl = `https://nominatim.openstreetmap.org/reverse?format=json&lat=${storeLat}&lon=${storeLng}&zoom=17&addressdetails=1`;
                    fetch(reverseUrl, { headers: { 'Accept-Language': 'vi,en;q=0.9', 'User-Agent': 'BachHoaXanhApp/1.0' } })
                        .then(r => r.json())
                        .then(data => {
                            if (data && data.display_name) {
                                const parts = data.display_name.split(',').map(p => p.trim()).filter(p => p);
                                const addr = parts.slice(0, 5).join(', ');
                                if (!window._branchAddrCache) window._branchAddrCache = {};
                                window._branchAddrCache[cacheKey] = addr;
                                if (branchAddrTextEl && document.getElementById('nearestBranchName') && 
                                    document.getElementById('nearestBranchName').innerText === storeName) {
                                    branchAddrTextEl.innerText = addr;
                                }
                            } else {
                                branchAddrEl.style.display = "none";
                            }
                        })
                        .catch(() => { branchAddrEl.style.display = "none"; });
                }
            }
        }
    }
        
        if (map) {
            const storeIcon = L.divIcon({
                html: '<div style="background-color: #1a7a2e; border: 2px solid #fff; border-radius: 50%; width: 36px; height: 36px; display: flex; align-items: center; justify-content: center; box-shadow: 0 2px 6px rgba(0,0,0,0.3);"><i class="bi bi-shop text-white" style="font-size: 18px; line-height: 1;"></i></div>',
                className: 'custom-store-icon',
                iconSize: [36, 36],
                iconAnchor: [18, 18]
            });

            if (storeMarker) {
                storeMarker.setLatLng(storeLatLng);
            } else {
                storeMarker = L.marker(storeLatLng, { icon: storeIcon }).addTo(map);
            }
            storeMarker.bindPopup(`<b>${storeName}</b><br>Cơ sở giao hàng.`, { autoPan: false }).openPopup();

            if (deliveryRouteLine) {
                if (routeCoords) {
                    deliveryRouteLine.setLatLngs(routeCoords);
                    deliveryRouteLine.setStyle({ dashArray: null });
                } else {
                    deliveryRouteLine.setLatLngs([latlng, storeLatLng]);
                    deliveryRouteLine.setStyle({ dashArray: '6, 8' });
                }
            } else {
                const lineCoords = routeCoords || [latlng, storeLatLng];
                deliveryRouteLine = L.polyline(lineCoords, {
                    color: '#1a7a2e',
                    weight: 3,
                    dashArray: routeCoords ? null : '6, 8',
                    opacity: 0.8
                }).addTo(map);
            }
        }
        
        const shSection = document.getElementById('shippingSection');
        const shAlert = document.getElementById('shippingAlert');
        if (shSection) shSection.style.display = "block";
        if (shAlert) shAlert.style.display = "none";
        
        const calc = calculateShipping(distance);
        lastShippingCalc = calc;
        
        const freeshipBadge = document.getElementById('shippingFreeshipBadge');
        if (freeshipBadge) {
            if (calc.isEligibleForFreeship) {
                freeshipBadge.style.display = "inline-block";
            } else {
                freeshipBadge.style.display = "none";
            }
        }
        
        if (!document.querySelector('.shipping-option-card.active')) {
            const defaultMethod = config.prefilledShippingMethod || "Instant";
            const targetCard = document.querySelector(`.shipping-option-card[data-method="${defaultMethod}"]`);
            if (targetCard) {
                targetCard.classList.add('active');
                const shMethodInput = document.getElementById('shippingMethodInput');
                if (shMethodInput) shMethodInput.value = defaultMethod;
            }
        }
        
        applyShippingFees();
    }
}

// Add event listeners to shipping cards
document.querySelectorAll('.shipping-option-card').forEach(card => {
    card.addEventListener('click', function() {
        document.querySelectorAll('.shipping-option-card').forEach(c => c.classList.remove('active'));
        this.classList.add('active');
        
        const method = this.dataset.method;
        const shMethodInput = document.getElementById('shippingMethodInput');
        if (shMethodInput) shMethodInput.value = method;
        
        if (lastShippingCalc) {
            applyShippingFees();
        } else if (selectedLatLng) {
            updateShippingInfo(selectedLatLng);
        }
    });
});

function applyShippingFees() {
    if (!lastShippingCalc) return;
    
    const config = window.shippingConfig || { hasFreshFood: false, subtotalAmount: 0, discountAmount: 0, baseFinalTotal: 0 };
    const isFreeShipVoucher = config.appliedDiscountCode && 
        (config.appliedDiscountCode.toUpperCase().includes("FREESHIP") || 
         config.appliedDiscountCode.toUpperCase().includes("FREE_SHIP") || 
         config.appliedDiscountCode.toUpperCase().includes("FREE-SHIP"));

    const feeInstantEl = document.getElementById('fee-Instant');
    const feeHourlyEl = document.getElementById('fee-Hourly');
    const fee4HoursEl = document.getElementById('fee-4Hours');
    const feeEconomyEl = document.getElementById('fee-Economy');
    
    let feeInstant = isFreeShipVoucher ? 0 : lastShippingCalc.results.Instant;
    let feeHourly = isFreeShipVoucher ? 0 : lastShippingCalc.results.Hourly;
    let fee4Hours = isFreeShipVoucher ? 0 : lastShippingCalc.results["4Hours"];
    let feeEconomy = isFreeShipVoucher ? 0 : lastShippingCalc.results.Economy;

    if (feeInstantEl) feeInstantEl.innerHTML = formatCurrency(feeInstant);
    if (feeHourlyEl) feeHourlyEl.innerHTML = formatCurrency(feeHourly);
    if (fee4HoursEl) fee4HoursEl.innerHTML = formatCurrency(fee4Hours);
    if (feeEconomyEl) feeEconomyEl.innerHTML = formatCurrency(feeEconomy);
    
    const activeCard = document.querySelector('.shipping-option-card.active');
    if (activeCard) {
        const method = activeCard.dataset.method;
        const originalFee = lastShippingCalc.results[method];
        const fee = isFreeShipVoucher ? 0 : originalFee;
        
        const shFeeInput = document.getElementById('shippingFeeInput');
        if (shFeeInput) shFeeInput.value = fee;
        
        const shFeeSummaryRow = document.getElementById('shippingFeeSummaryRow');
        const shFeeSummaryDisplay = document.getElementById('shippingFeeSummaryDisplay');
        const finalTotalDisplay = document.getElementById('finalTotalDisplay');
        
        if (shFeeSummaryRow) shFeeSummaryRow.style.setProperty('display', 'flex', 'important');
        if (shFeeSummaryDisplay) {
            shFeeSummaryDisplay.innerHTML = formatCurrency(fee);
        }
        if (finalTotalDisplay) finalTotalDisplay.innerText = formatCurrency(config.baseFinalTotal + fee);
    }
}

// Background geocode function when user manually types address and focuses out
function geocodeAddressAndSetShipping(addr) {
    geocodeAddress(addr)
    .then(result => {
        if (result) {
            const latlng = L.latLng(parseFloat(result.lat), parseFloat(result.lon));
            selectedLatLng = latlng;
            
            let resolvedAddr = buildCleanAddress(result);
            resolvedAddr = mergeHouseNumber(addr, resolvedAddr);
            selectedAddress = resolvedAddr;
            
            updateShippingInfo(latlng);
            
            if (map) {
                map.setView(latlng, 16);
                updateMarker(latlng, true);
            }
        }
    })
    .catch(err => console.error("Geocoding error:", err));
}

const addressTextarea = document.querySelector('textarea[name="address"]');
if (addressTextarea) {
    addressTextarea.addEventListener('blur', function() {
        const typedAddress = this.value.trim();
        if (typedAddress && typedAddress !== selectedAddress) {
            if (isLandmarkQuery(typedAddress)) {
                // Open modal and perform search
                const mapModalEl = document.getElementById('mapModal');
                if (mapModalEl) {
                    const searchInputEl = document.getElementById('mapSearchInput');
                    if (searchInputEl) searchInputEl.value = typedAddress;
                    
                    const bootstrapModal = bootstrap.Modal.getOrCreateInstance(mapModalEl);
                    bootstrapModal.show();
                    
                    // We wait for modal to be shown to trigger search and center map properly
                    mapModalEl.addEventListener('shown.bs.modal', function onShown() {
                        performSearch();
                        mapModalEl.removeEventListener('shown.bs.modal', onShown);
                    });
                }
            } else {
                geocodeAddressAndSetShipping(typedAddress);
            }
        }
    });
}

// On load check for prefilled address
function initPrefilledAddress() {
    if (addressTextarea) {
        const addrVal = addressTextarea.value.trim();
        if (addrVal) {
            console.log("Initializing prefilled address on load:", addrVal);
            geocodeAddressAndSetShipping(addrVal);
        }
    }
}

if (document.readyState === 'loading') {
    window.addEventListener('DOMContentLoaded', initPrefilledAddress);
} else {
    initPrefilledAddress();
}

// Form submission check
const formEl = document.querySelector('form');
if (formEl) {
    formEl.addEventListener('submit', function(e) {
        const methodInputEl = document.getElementById('shippingMethodInput');
        const methodInput = methodInputEl ? methodInputEl.value : "";
        
        const addressTextareaEl = document.querySelector('textarea[name="address"]');
        const addressVal = addressTextareaEl ? addressTextareaEl.value.trim() : "";
        
        if (addressVal && !methodInput) {
            e.preventDefault();
            alert("Vui lòng chọn phương thức vận chuyển trước khi thanh toán.");
            return;
        }

        // Branch verification logic when not explicitly chosen
        const config = window.shippingConfig || {};
        if (config.isBranchExplicitlyChosen === false && !isStockVerified) {
            e.preventDefault(); // Stop submission to verify stock first
            
            const submitBtn = formEl.querySelector('button[type="submit"]');
            const originalHtml = submitBtn.innerHTML;
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang xác minh tồn kho...';
            
            fetch('/Cart/CheckBranchStock')
                .then(res => res.json())
                .then(data => {
                    submitBtn.disabled = false;
                    submitBtn.innerHTML = originalHtml;
                    
                    if (data.success) {
                        if (data.hasEnoughStock) {
                            isStockVerified = true;
                            formEl.submit(); // Re-trigger submit
                        } else {
                            // Show the stock warning modal banner
                            const modalBranchEl = document.getElementById('stockWarningBranchName');
                            const modalProductsEl = document.getElementById('stockWarningProductsList');
                            
                            if (modalBranchEl) modalBranchEl.textContent = `"${data.branchName}"`;
                            if (modalProductsEl) {
                                modalProductsEl.innerHTML = data.outOfStockProducts.map(p => 
                                    `<div class="py-1 text-danger fw-bold"><i class="bi bi-x-circle-fill me-2"></i>${p}</div>`
                                ).join('');
                            }
                            
                            const modalEl = document.getElementById('stockWarningModal');
                            if (modalEl) {
                                const bootstrapModal = bootstrap.Modal.getOrCreateInstance(modalEl);
                                bootstrapModal.show();
                            }
                        }
                    } else {
                        alert(data.message || "Lỗi kiểm tra tồn kho tại chi nhánh.");
                    }
                })
                .catch(err => {
                    submitBtn.disabled = false;
                    submitBtn.innerHTML = originalHtml;
                    console.error("Stock check failed:", err);
                    alert("Lỗi kết nối khi kiểm tra tồn kho.");
                });
        }
    });
}

const confirmAddressBtnEl = document.getElementById('confirmAddressBtn');
if (confirmAddressBtnEl) {
    confirmAddressBtnEl.addEventListener('click', function() {
        if (selectedAddress) {
            const addressTextareaEl = document.querySelector('textarea[name="address"]');
            if (addressTextareaEl) {
                addressTextareaEl.value = selectedAddress;
            }
            
            const mapModalDiv = document.getElementById('mapModal');
            if (mapModalDiv) {
                const bootstrapModal = bootstrap.Modal.getInstance(mapModalDiv);
                if (bootstrapModal) bootstrapModal.hide();
            }
            
            if (selectedLatLng) {
                updateShippingInfo(selectedLatLng);
            }
        }
    });
}

// Click outside to close branch search results list
document.addEventListener('click', function(e) {
    const listEl = document.getElementById('branchSearchResults');
    const searchInputEl = document.getElementById('mapSearchInput');
    const searchBtnEl = document.getElementById('mapSearchBtn');
    
    // Dùng .closest() để bắt cả click vào phần tử con bên trong nút/input
    const clickedInsideList = listEl && listEl.contains(e.target);
    const clickedInsideInput = searchInputEl && (e.target === searchInputEl || searchInputEl.contains(e.target));
    const clickedInsideBtn = searchBtnEl && (e.target === searchBtnEl || searchBtnEl.contains(e.target));
    
    if (listEl && !clickedInsideList && !clickedInsideInput && !clickedInsideBtn) {
        listEl.style.display = "none";
    }
});

