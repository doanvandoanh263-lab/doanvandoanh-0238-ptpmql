CREATE TABLE Khoa (
    Code TEXT NOT NULL PRIMARY KEY,
    Name TEXT NOT NULL
);

CREATE TABLE SinhVien (
    StudentCode TEXT NOT NULL PRIMARY KEY,
    FullName TEXT NOT NULL,
    EntryYear INTEGER NOT NULL,
    TrainingSystemCode TEXT NOT NULL,
    MajorCode TEXT NOT NULL,
    Address TEXT NULL,
    KhoaCode TEXT NOT NULL,
    CONSTRAINT FK_SinhVien_Khoa FOREIGN KEY (KhoaCode) REFERENCES Khoa(Code)
);

CREATE TABLE StudentCodeCounter (
    Prefix TEXT NOT NULL PRIMARY KEY,
    LastNumber INTEGER NOT NULL
);

INSERT INTO Khoa (Code, Name) VALUES
('CNTT', 'Khoa Công nghệ Thông tin'),
('DC', 'Khoa Địa chất'),
('MO', 'Khoa Kỹ thuật Mỏ'),
('KT', 'Khoa Kinh tế'),
('DCK', 'Khoa Cơ - Điện'),
('DCKT', 'Khoa Dầu khí'),
('XD', 'Khoa Xây dựng'),
('MT', 'Khoa Môi trường'),
('TDBD', 'Khoa Trắc địa - Bản đồ và Quản lý đất đai'),
('KHCB', 'Khoa Khoa học cơ bản'),
('LLCT', 'Khoa Lý luận chính trị'),
('GDQP', 'Khoa Giáo dục quốc phòng');

INSERT INTO StudentCodeCounter (Prefix, LastNumber) VALUES ('212105', 238);
