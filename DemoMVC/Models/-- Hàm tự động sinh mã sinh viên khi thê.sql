-- Tạo bảng Khoa
CREATE TABLE Khoa (
    ma_khoa VARCHAR(10) PRIMARY KEY,
    ten_khoa NVARCHAR(100) NOT NULL
);

-- Tạo bảng SinhVien
CREATE TABLE SinhVien (
    ma_sinh_vien VARCHAR(10) PRIMARY KEY,
    ho_ten NVARCHAR(100) NOT NULL,
    nam_nhap_hoc INT NOT NULL,
    ma_he_dao_tao VARCHAR(2) NOT NULL,
    ma_nganh VARCHAR(2) NOT NULL,
    ma_khoa VARCHAR(10) NOT NULL,
    CONSTRAINT FK_SinhVien_Khoa 
        FOREIGN KEY (ma_khoa) REFERENCES Khoa(ma_khoa)
);

-- Nhập dữ liệu các khoa của Trường Đại học Mỏ - Địa chất
INSERT INTO Khoa (ma_khoa, ten_khoa) VALUES
('CNTT', N'Khoa Công nghệ Thông tin'),
('DC',   N'Khoa Địa chất'),
('Mỏ',   N'Khoa Kỹ thuật Mỏ'),
('KT',   N'Khoa Kinh tế');
