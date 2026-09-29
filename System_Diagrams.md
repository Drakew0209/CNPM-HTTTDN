# Tài liệu Thiết kế Hệ thống Thông tin
## Internet Cafe Management System

> **Phiên bản:** 1.0 | **Ngày:** 29/09/2026 | **Người lập:** System Analyst

---

## 1. Sơ đồ Ngữ cảnh (Context Diagram)

Sơ đồ mô tả hệ thống quản lý Internet Cafe ở mức tổng quan nhất, thể hiện các tác nhân bên ngoài tương tác với hệ thống trung tâm.

```mermaid
flowchart TD
    %% Styling
    classDef actor fill:#4A90D9,stroke:#2C5F8A,color:#fff,rx:8
    classDef system fill:#2ECC71,stroke:#1A8A4A,color:#fff,rx:8,font-size:14px

    %% External Actors
    KH([Khach hang]):::actor
    NV([Nhan vien Thu ngan]):::actor
    QL([Quan ly]):::actor
    MT([May tram - Workstation]):::actor

    %% Central System
    SYS["HE THONG QUAN LY\nINTERNET CAFE\nInternetCafeDB"]:::system

    %% Khach hang --> He thong
    KH -- "Dang ky / Dang nhap\nNap tien Combo\nDat F&B\nPhan hoi / Khao sat" --> SYS

    %% He thong --> Khach hang
    SYS -- "Thong tin tai khoan / So du\nHoa don / Phien choi\nKet qua khao sat\nThong bao khuyen mai" --> KH

    %% Nhan vien --> He thong
    NV -- "Mo / Dong phien choi\nTiep nhan xu ly don F&B\nXu ly giao dich nap tien\nCham cong" --> SYS

    %% He thong --> Nhan vien
    SYS -- "Danh sach don hang\nLich lam viec\nThong bao phien sap het gio\nKet qua phan hoi khach" --> NV

    %% Quan ly --> He thong
    QL -- "Quan ly nhan su HRM\nDuyet don nghi phep\nTao khao sat bao cao\nCau hinh gia Combo" --> SYS

    %% He thong --> Quan ly
    SYS -- "Bao cao doanh thu\nBao cao luong cham cong\nBao cao ton kho\nThong ke khach hang" --> QL

    %% May tram <-> He thong
    MT -- "Trang thai may\nThong tin phien dang chay" --> SYS
    SYS -- "Kich hoat / Khoa may\nThong tin tai khoan khach" --> MT
```

---

## 2. Sơ đồ Chức năng (Business Function Diagram - BFD)

Phân rã toàn bộ các chức năng của hệ thống thành 4 phân hệ chính.

```mermaid
flowchart TD
    ROOT["HE THONG QUAN LY INTERNET CAFE"]

    ROOT --> CRM["1. CRM - Quan ly Khach hang"]
    CRM --> CRM1["1.1 Quan ly ho so khach hang"]
    CRM --> CRM2["1.2 Quan ly cap bac thanh vien"]
    CRM --> CRM3["1.3 Quan ly giao dich nap tien va so du"]
    CRM --> CRM4["1.4 Quan ly phan hoi va khieu nai"]
    CRM --> CRM5["1.5 Quan ly khao sat va cau hoi"]

    ROOT --> HRM["2. HRM - Quan ly Nhan su"]
    HRM --> HRM1["2.1 Quan ly phong ban va chuc vu"]
    HRM --> HRM2["2.2 Quan ly ho so nhan vien"]
    HRM --> HRM3["2.3 Quan ly ca lam viec va lich phan cong"]
    HRM --> HRM4["2.4 Quan ly cham cong"]
    HRM --> HRM5["2.5 Quan ly don nghi phep"]
    HRM --> HRM6["2.6 Quan ly tinh luong va bang luong"]

    ROOT --> ORD["3. ORDER - Quan ly Ban hang F&B"]
    ORD --> ORD1["3.1 Quan ly danh muc va san pham"]
    ORD --> ORD2["3.2 Tiep nhan va xu ly don hang"]
    ORD --> ORD3["3.3 Quan ly chi tiet don hang"]
    ORD --> ORD4["3.4 Quan ly kho nhap xuat hang"]
    ORD --> ORD5["3.5 Quan ly Combo va khuyen mai"]

    ROOT --> USG["4. USAGE_SESSIONS - Quan ly Phien choi"]
    USG --> USG1["4.1 Quan ly may tram va khu vuc"]
    USG --> USG2["4.2 Mo phien choi Check-in"]
    USG --> USG3["4.3 Theo doi phien dang hoat dong"]
    USG --> USG4["4.4 Ket thuc va tinh tien phien choi Check-out"]
    USG --> USG5["4.5 Quan ly lich su phien choi"]
```

---

## 3. Sơ đồ Luồng Dữ liệu Mức Đỉnh (DFD Level 0)

Thể hiện 4 tiến trình chính, các luồng dữ liệu với tác nhân ngoài và các kho dữ liệu.

```mermaid
flowchart LR
    KH([Khach hang])
    NV([Nhan vien Thu ngan])
    QL([Quan ly])
    MT([May tram])

    DS1[("DS1: Customers, Membership_Tier")]
    DS2[("DS2: Employees, Departments, Positions")]
    DS3[("DS3: Products, Orders, Combos, Inventory")]
    DS4[("DS4: Usage_Sessions, Computers, Transactions")]
    DS5[("DS5: Work_Schedules, Attendance, Payroll, Leave")]
    DS6[("DS6: Surveys, Feedback")]

    P1["P1 - QUAN LY\nKHACH HANG\nCRM"]
    P2["P2 - QUAN LY\nNHAN SU\nHRM"]
    P3["P3 - QUAN LY\nBAN HANG\nORDER"]
    P4["P4 - QUAN LY\nPHIEN CHOI\nUSAGE_SESSIONS"]

    KH -->|"Dang ky, nap tien, dat mon, phan hoi"| P1
    P1 -->|"Xac nhan TK, so du, ket qua khao sat"| KH
    P1 <-->|"Luu/Doc ho so KH, phan hoi, khao sat"| DS1
    P1 <-->|"Luu ket qua khao sat, phan hoi"| DS6
    NV -->|"Nap tien cho KH, xu ly giao dich"| P1
    QL -->|"Tao khao sat, xem bao cao KH"| P1
    P1 -->|"Bao cao phan tich khach hang"| QL

    QL -->|"Phan cong ca, duyet nghi phep, cau hinh luong"| P2
    P2 -->|"Bang luong, bao cao cham cong"| QL
    NV -->|"Cham cong, nop don nghi phep"| P2
    P2 -->|"Lich lam viec, trang thai don nghi"| NV
    P2 <-->|"Doc/Ghi ho so NV"| DS2
    P2 <-->|"Luu cham cong, tinh luong, nghi phep"| DS5

    KH -->|"Dat mon F&B"| P3
    P3 -->|"Xac nhan don, hoa don F&B"| KH
    NV -->|"Tiep nhan, cap nhat trang thai don"| P3
    P3 -->|"Thong bao don can xu ly"| NV
    QL -->|"Nhap hang, cau hinh Combo, danh muc"| P3
    P3 -->|"Bao cao doanh thu, ton kho"| QL
    P3 <-->|"Doc/Ghi don hang, ton kho, san pham"| DS3
    P3 <-->|"Ghi giao dich FoodOrder"| DS4

    KH -->|"Yeu cau mo phien, thong tin dang nhap"| P4
    P4 -->|"Xac nhan phien, thoi gian, so du con lai"| KH
    NV -->|"Mo/Dong phien, Check-in/out"| P4
    P4 -->|"Thong bao phien sap het, hoa don phien"| NV
    MT -->|"Trang thai may, su kien ket noi"| P4
    P4 -->|"Kich hoat/Khoa may, thong tin phien"| MT
    P4 <-->|"Doc/Ghi phien choi, may, giao dich Rental"| DS4
    P4 <-->|"Doc TT KH, cap nhat so du"| DS1
    DS2 <-->|"Doc thong tin NV phu trach"| P3
    DS2 <-->|"Doc NV phu trach phien choi"| P4
```

---

*Tài liệu được tạo dựa trên schema `InternetCafe Final.sql` - InternetCafeDB*
