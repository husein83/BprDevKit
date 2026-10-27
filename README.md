# BprDevKit (Bpr Developer Toolkit) 🛠️🚀

<div align="center">

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)
![C#](https://img.shields.io/badge/C%23-13.0-239120?logo=csharp&logoColor=white)
![Database](https://img.shields.io/badge/SQL%20Server-2016+-CC292B?logo=microsoftsqlserver&logoColor=white)
![UI](https://img.shields.io/badge/UI-Bootstrap%205%20%7C%20RTL-7952B3?logo=bootstrap&logoColor=white)
![Languages](https://img.shields.io/badge/Languages-Persian%20%7C%20English-blue)

**ابزار جامع، مدرن و قدرتمند توسعه‌دهندگان پایگاه‌داده و تولید خودکار مدل‌های نرم‌افزاری**  
*A modern, comprehensive developer toolkit for database administration, dynamic scripting, and multi-language data model generation.*

</div>

---

## 📖 فهرست مطالب / Table of Contents
- [معرفی پروژه / Overview](#-معرفی-پروژه--overview)
- [امکانات کلیدی / Key Features](#-امکانات-کلیدی--key-features)
  - [۱. مدیریت دیتابیس (Database Management)](#۱-مدیریت-دیتابیس-database-management)
  - [۲. تولید دیتامدل (Data Model Generator)](#۲-تولید-دیتامدل-data-model-generator)
  - [۳. تولید اسکریپت پایگاه‌داده (Script Generator)](#۳-تولید-اسکریپت-پایگاهداده-script-generator)
  - [۴. پشتیبان‌گیری و بازگردانی تنظیمات (Config Backup & Restore)](#۴-پشتیبانگیری-و-بازگردانی-تنظیمات-config-backup--restore)
  - [۵. لاگ و مانیتورینگ (Logging & Audit)](#۵-لاگ-و-مانیتورینگ-logging--audit)
  - [۶. چندزبانه و طراحی مدرن (i18n & Modern UI)](#۶-چندزبانه-و-طراحی-مدرن-i18n--modern-ui)
- [معماری و ساختار پروژه / Architecture & Structure](#-معماری-و-ساختار-پروژه--architecture--structure)
- [پیش‌نیازها / Prerequisites](#-پیشنیازها--prerequisites)
- [راه‌اندازی و اجرا / Getting Started](#-راهاندازی-و-اجرا--getting-started)
- [ساخت نسخه ریلیز / Build & Release](#-ساخت-نسخه-ریلیز--build--release)
- [راهنمای انتشار در گیت‌هاب / GitHub Release Guide](#-راهنمای-انتشار-در-گیتهاب--github-release-guide)
- [امنیت / Security](#-امنیت--security)

---

## 🌟 معرفی پروژه / Overview

**BprDevKit** یک محیط کاری مدرن تحت وب (توسعه‌یافته بر پایه‌ی **ASP.NET Core Blazor Server** و **.NET 10**) است که برای تسریع، خودکارسازی و مدیریت عملیات‌های روزمره توسعه‌دهندگان بک‌اند و مدیران پایگاه‌داده (DBA) طراحی شده است.

این ابزار با حذف کارهای تکراری، امکان مهندسی معکوس ساختار دیتابیس به کلاس‌های مدل با تایپ قوی، تولید اسکریپت‌های DDL/DML، مدیریت جامع آبجکت‌های SQL، و بکاپ/ریستور دیتابیس و تنظیمات را با بالاترین کیفیت و رابط کاربری فارسی و انگلیسی فراهم می‌آورد.

---

## ⚡ امکانات کلیدی / Key Features

### ۱. مدیریت دیتابیس (Database Management)
- **مدیریت کامل اشیاء دیتابیس:** ایجاد، ویرایش و حذف جداول (Tables)، ستون‌ها، کلیدهای اصلی و خارجی (FK)، ایندکس‌ها، قیود چک (Check Constraints)، ویوها (Views)، توابع (Functions)، پروسیجرها (Stored Procedures)، تریگرها (Triggers) و اسکیماها (Schemas).
- **پشتیبان‌گیری (Backup):**
  - امکان تعیین نام دلخواه فایل با زمان‌بندی دقیق و خوانا (`YYYY-MM-DD_HH-mm-ss`).
  - عدم تحمیل مسیر پیش‌فرض سرور؛ کاربر آزادی کامل در تعیین پوشه و فایل دارد.
  - دارای مرورگر فایل و درایو اختصاصی برای انتخاب پوشه مقصد از روی دیسک‌های سرور.
  - پشتیبانی از فشرده‌سازی (`WITH COMPRESSION`) و پشتیبان مستقل (`COPY_ONLY`).
- **بازگردانی (Restore):**
  - مرورگر سرور برای پیدا کردن و انتخاب مستقیم فایل‌های `.bak`.
  - تنظیمات پیشرفته شامل `WITH REPLACE` و قطع خودکار اتصالات فعال (`ROLLBACK IMMEDIATE`).

### ۲. تولید دیتامدل (Data Model Generator)
مهندسی معکوس و تبدیل ساختار جداول پایگاه‌داده به کلاس‌های تمیز در زبان‌های مختلف:
- **پشتیبانی از ۴ زبان پرکاربرد:**
  - 🔷 **C# (.NET / EF Core / Dapper / Clean Architecture)**
  - 🐍 **Python (Dataclasses / Pydantic / SQLAlchemy)**
  - 🎯 **Dart (Flutter / Serializable)**
  - ☕ **Java (POJO / JPA / Lombok)**
- **شخصی‌سازی پیشرفته مدل‌های C#:**
  - تولید فیلدهای `private` و `public` به همراه Propertyهای سفارشی یا استاندارد.
  - تولید صفت‌های سیستمی (Attributes) نظیر `[Required]`، `[StringLength]`، `[Column]`، `[Key]` و ...
  - مفردسازی هوشمند نام کلاس‌ها (Singularization: مثلاً تبدیل `Users` به `User`).
  - تشخیص کلیدهای خارجی و ارجاعات ناوبری بدون ایجاد ارجاع اشتباه به خود جدول.
  - الگوی نام‌گذاری فضاهای نام (Namespaces) با استفاده از توکن‌های استاندارد (`{Schema}`, `{Table}`, `{DbName}`).
  - **کنترل دسترسی اسکیماها (Schema Access Control):** تعریف روابط مجاز بین اسکیماها جهت ایزوله‌سازی کدهای معماری ماژولار و DDD.
- **بسته‌بندی و دانلود چندحالته (Zip Packaging):**
  - بسته شدن همه مدل‌ها در یک فایل زیپ واحد.
  - بسته شدن بر اساس اسکیما (هر اسکیما در پوشه اختصاصی خود).
  - بسته شدن بر اساس نام Namespace.
  - نام‌گذاری تمیز و استاندارد فایل‌های خروجی همراه با تایم‌استمپ خوانا.

### ۳. تولید اسکریپت پایگاه‌داده (Script Generator)
- استخراج اسکریپت DDL/DML برای تمام یا بخشی از جداول، ویوها، پروسیجرها و توابع.
- مرتب‌سازی وابستگی‌ها (Dependency Resolution) برای اطمینان از اجرای بدون خطای اسکریپت‌ها.
- گزینه‌های تولید اسکریپت ساختار (`CREATE`)، حذف و ساخت مجدد (`DROP AND CREATE`)، یا درج داده‌ها (`DATA / INSERT`).

### ۴. پشتیبان‌گیری و بازگردانی تنظیمات (Config Backup & Restore)
- خروجی گرفتن از تمام سرورهای ذخیره‌شده، پروفایل‌های تولید دیتامدل و تنظیمات سیستم در قالب یک فایل رمزگذاری‌شده‌ی امن JSON.
- بازگردانی ساده تنظیمات در محیط‌ها یا دستگاه‌های دیگر با قابلیت پیش‌نمایش محتوا قبل از اعمال.

### ۵. لاگ و مانیتورینگ (Logging & Audit)
- ثبت چرخشی لاگ‌ها (Rolling File Logger) بدون افت کارایی سیستم.
- رهگیری رخدادها، تراکنش‌ها و خطاهای دیتابیس در صفحه‌ی اختصاصی مشاهده‌ی لاگ‌ها با فیلتر سطح اهمیت (Info, Warning, Error).

### ۶. چندزبانه و طراحی مدرن (i18n & Modern UI)
- رابط کاربری دوزبانه کامل: **فارسی (با پشتیبانی کامل RTL)** و **English (LTR)**.
- تغییر آنی زبان بدون نیاز به بارگذاری مجدد صفحه.
- طراحی بر پایه پالت رنگی حرفه‌ای، بدون ابهام و با بالاترین سطح تجربه کاربری (UX).

---

## 🏛 معماری و ساختار پروژه / Architecture & Structure

پروژه با ساختار دامنه‌محور (Domain-Driven) و تفکیک شفاف ماژول‌ها سازمان‌دهی شده است:

```text
BprDevKit/
├── Release/                         # خروجی بیلد و انتشار (Publish)
├── SourceCode/
│   ├── Components/                  # کامپوننت‌های Blazor
│   │   ├── Common/                  # کامپوننت‌های مشترک (Modal, Toast, ...)
│   │   ├── Layout/                  # قالب اصلی و سایدبار ناوبری (NavMenu, ...)
│   │   └── Pages/                   # صفحات ماژولار برنامه
│   │       ├── ConfigurationSetting/ # تنظیمات سرور، پروفایل‌ها و بکاپ کانفیگ
│   │       ├── DataModelGenerator/  # صفحه تولید کلاس‌ها و مدل‌های داده‌ای
│   │       ├── DatabaseManagement/  # مدیریت جامع دیتابیس، جداول، بکاپ/ریستور
│   │       ├── Logs/                # مشاهده و فیلتر لاگ‌های سیستم
│   │       └── ScriptGenerator/     # اسکریپت‌ساز داینامیک دیتابیس
│   ├── Config/                      # مدیریت کانفیگ‌ها و پروفایل‌های رمزنگاری‌شده
│   │   ├── ApplicationConfiguration.cs
│   │   ├── ConfigurationBackupService.cs
│   │   ├── DataModelProfile.cs
│   │   └── ServerConfiguration.cs
│   ├── DataModelGenerator/          # موتور هسته تولید کدهای مدل
│   │   ├── Generators/              # CSharp, Python, Dart, Java Generators
│   │   ├── Models/                  # آپشن‌ها و تنظیمات تولید مدل
│   │   └── Services/                # سرویس اصلی، TypeMapper و NamingHelper
│   ├── DatabaseManagement/          # موتور هسته مدیریت پایگاه‌داده
│   │   ├── Connections/             # مدیریت اتصالات و Factory
│   │   ├── Decryption/              # رمزگشایی آبجکت‌های محافظت‌شده SQL
│   │   ├── Models/                  # مدل‌های متادیتا، عملیات و سیستم فایل
│   │   ├── SqlGenerators/           # ژنراتورهای اختصاصی SQL Server, PG, MySQL...
│   │   └── DatabaseManager.cs       # کنترلر اصلی عملیات تراکنش و اجرای کوئری
│   ├── Localization/                # سیستم دوزبانه (فارسی و انگلیسی)
│   ├── ScriptGenerator/             # سرویس‌ها و مدل‌های تولید اسکریپت
│   ├── Utility/                     # لاگر چرخشی، متدهای رمزنگاری و ابزارهای عمومی
│   ├── GlobalUsings.cs              # ارجاعات سراسری پروژه
│   └── Program.cs                   # پیکربندی هاست ASP.NET Core و تزریق وابستگی (DI)
└── README.md
```

---

## 📋 پیش‌نیازها / Prerequisites

برای اجرای پروژه یا ویرایش سورس‌کد، نیاز به موارد زیر دارید:
1. **.NET SDK 10.0** (یا بالاتر)
2. **یکی از محیط‌های توسعه:**
   - Visual Studio 2026 / 2022 (آخرین نسخه) با Workload توسعه ASP.NET و Web
   - Visual Studio Code با اکستنشن C# Dev Kit
   - JetBrains Rider
3. **Microsoft SQL Server** (نسخه‌های 2016 تا 2022 یا Azure SQL) جهت اتصال و تست

---

## 🚀 راه‌اندازی و اجرا / Getting Started

### ۱. کلون کردن ریپازیتوری
```bash
git clone https://github.com/husein83/BprDevKit.git
cd BprDevKit
```

### ۲. اجرای پروژه در حالت توسعه (Development)
```bash
dotnet run --project SourceCode/BprDevKit.csproj
```
پس از اجرا، مرورگر خود را باز کرده و به آدرس زیر مراجعه فرمایید:
```text
https://localhost:5001  یا  http://localhost:5000
```

---

## 📦 ساخت نسخه ریلیز / Build & Release

برای ساخت نسخه‌ی کامپایل‌شده بدون وابستگی به حالت Debug:

### حالت استاندارد ریلیز (Framework-Dependent):
```bash
dotnet publish -c Release -o Release/App SourceCode/BprDevKit.csproj
```
خروجی در مسیر `Release/App` ایجاد شده و فایل `BprDevKit.exe` قابل اجرا خواهد بود.

### حالت مستقل (Self-Contained Single-File برای Windows x64):
اگر می‌خواهید برنامه‌ای بسازید که حتی روی سیستم‌های بدون .NET نصب شده نیز اجرا شود:
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o Release/Standalone SourceCode/BprDevKit.csproj
```

---

## 🏷️ راهنمای انتشار در گیت‌هاب / GitHub Release Guide

برای تگ‌زدن و انتشار نسخه‌ی رسمی روی گیت‌هاب:

```bash
# ۱. پوش کردن تغییرات به مخزن
git push origin main

# ۲. ایجاد تگ نسخه جدید (مثلاً v1.0.0)
git tag -a v1.0.0 -m "Release Version 1.0.0"

# ۳. ارسال تگ به گیت‌هاب
git push origin v1.0.0
```

سپس در صفحه GitHub به بخش **Releases** رفته، گزینه **Create a new release** را بزنید، تگ `v1.0.0` را انتخاب کرده و فایل زیپ‌شده از پوشه‌ی `Release/App` را به عنوان فایل پیوست اجرایی آپلود فرمایید.

---

## 🔒 امنیت / Security

- **حفاظت از اطلاعات حساس:** تمامی کانکشن‌استرینگ‌ها و کلمات عبور پایگاه‌های داده در فایل‌های پیکربندی محلی با استفاده از الگوریتم‌های استاندارد رمزنگاری محافظت شده و به صورت متن‌باز ذخیره نمی‌شوند.
- **تراکنش‌های امن:** فرآیندهای DDL و اسکریپت‌نویسی در محیط‌های مدیریت‌شده و با سطوح دسترسی کنترل‌شده اجرا می‌شوند.

---

<div align="center">
طراحی و توسعه‌یافته با ❤️ برای جامعه توسعه‌دهندگان
<br>
<b>BprDevKit © 2026</b>
</div>
