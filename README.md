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

---

[📖 مطالعه راهنمای کامل به زبان فارسی](#-راهنمای-جامع-فارسی) &nbsp;|&nbsp; [📖 Read Full Documentation in English](#-complete-english-documentation)

---

</div>

<a name="-راهنمای-جامع-فارسی"></a>
# 🇮🇷 راهنمای جامع فارسی (Persian Documentation)

## 📑 فهرست مطالب فارسی
1. [معرفی BprDevKit](#fa-overview)
2. [معماری و تکنولوژی‌ها](#fa-architecture)
3. [راهنمای ماژول‌ها و امکانات قدم‌به‌قدم](#fa-modules)
   - [۳.۱. مدیریت سرورها و پایگاه‌های داده (Database Management)](#fa-db-management)
   - [۳.۲. تولید دیتامدل چندزبانه (Data Model Generator)](#fa-model-generator)
   - [۳.۳. تنظیمات پروفایل و گزینه‌های پیشرفته سی‌شارپ (C# Advanced Config)](#fa-csharp-advanced)
   - [۳.۴. مدیریت منعطف انوتیشن‌ها (Annotations Management)](#fa-annotations)
   - [۳.۵. موتور تبدیل پیشرفته پراپرتی‌ها (Property Transformations)](#fa-transformations)
   - [۳.۶. کنترل دسترسی اسکیماها (Schema Access Control)](#fa-schema-access)
   - [۳.۷. تنظیمات زبان‌های Python ،Dart و Java](#fa-other-languages)
   - [۳.۸. تولید اسکریپت پایگاه‌داده (Script Generator)](#fa-script-generator)
   - [۳.۹. پشتیبان‌گیری و بازگردانی تنظیمات (Config Backup & Restore)](#fa-config-backup)
   - [۳.۱۰. لاگ و مانیتورینگ سیستم (System Logs)](#fa-logs)
4. [پیش‌نیازها و راه‌اندازی](#fa-getting-started)
5. [ساخت نسخه مستقل (Release & Publish)](#fa-release)

---

<a name="fa-overview"></a>
### ۱. معرفی BprDevKit
**BprDevKit** یک پلتفرم تحت وب پیشرفته، سبک و سریع است که با هدف خودکارسازی کارهای زمان‌بر دیتابیس و برنامه‌نویسی بک‌اند توسعه یافته است. این ابزار به توسعه‌دهندگان و مدیران دیتابیس (DBA) اجازه می‌دهد به جای صرف ساعت‌ها زمان برای نوشتن دستی کلاس‌های دیتامدل، مدیریت دستی اسکریپت‌ها یا اتصال جداگانه به SSMS، تمام این فرآیندها را در یک داشبورد مدرن، دوزبانه و استاندارد انجام دهند.

---

<a name="fa-architecture"></a>
### ۲. معماری و تکنولوژی‌ها
- **فریم‌ورک هسته:** Microsoft .NET 10.0 (ASP.NET Core Blazor Server)
- **زبان برنامه‌نویسی:** C# 13
- **رابط کاربری:** Bootstrap 5 با پشتیبانی بومی از راست‌چین (RTL) و چپ‌چین (LTR)
- **بانک اطلاعاتی هدف:** Microsoft SQL Server (2016 تا 2022 و Azure SQL)
- **معماری نرم‌افزار:** تفکیک ماژولار Domain-Driven، بدون وابستگی‌های سنگین خارجی، دارای لاگر چرخشی اختصاصی (Rolling File Logger) و سیستم رمزنگاری داخلی کانفیگ‌ها.

---

<a name="fa-modules"></a>
### ۳. راهنمای ماژول‌ها و امکانات قدم‌به‌قدم

<a name="fa-db-management"></a>
#### ۳.۱. مدیریت پایگاه‌داده (Database Management)
این ماژول امکانات جامعی را برای مدیریت اشیاء و داده‌ها فراهم می‌کند:
- **مدیریت کامل اشیاء:** مرور، مشاهده ساختار و تغییرات در جداول (Tables)، ستون‌ها، کلیدهای اصلی (PK)، کلیدهای خارجی (FK)، ایندکس‌ها، قیود چک (Check Constraints)، تریگرها (Triggers)، ویوها (Views)، توابع (Functions) و پروسیجرها (Stored Procedures).
- **رمزگشایی آبجکت‌های محافظت‌شده (WITH ENCRYPTION):** مشاهده سورس‌کد پروسیجرها و توابعی که با دستور `WITH ENCRYPTION` در SQL Server قفل شده‌اند.
- **پشتیبان‌گیری پیشرفته (Database Backup):**
  - **مرورگر فایل سرور (Server Disk Browser):** امکان کلیک روی درایوها و پوشه‌های سرور برای انتخاب دقیق مسیر ذخیره‌سازی، بدون محدود شدن به مسیر پیش‌فرض بکاپ SQL.
  - **نام‌گذاری هوشمند فایل:** درج خودکار نام دیتابیس به همراه تایم‌استمپ تمیز (`DatabaseName_YYYY-MM-DD_HH-mm-ss.bak`).
  - **پشتیبانی از گزینه‌های پیشرفته:** فشرده‌سازی (`WITH COMPRESSION`) و پشتیبان مستقل (`COPY_ONLY`).
- **بازگردانی پیشرفته (Database Restore):**
  - امکان مرور دیسک‌های سرور و انتخاب مستقیم فایل‌های `.bak`.
  - گزینه بازنویسی دیتابیس موجود (`WITH REPLACE`).
  - قطع خودکار نشست‌ها و اتصالات فعال قبل از ریستور (`ROLLBACK IMMEDIATE`).

---

<a name="fa-model-generator"></a>
#### ۳.۲. تولید دیتامدل چندزبانه (Data Model Generator)
ویزارد ساده و سریع در ۳ گام:
1. **گام اول (Select Server):** انتخاب سرور متصل.
2. **گام دوم (Select Database):** انتخاب پایگاه داده مورد نظر.
3. **گام سوم (Select Profile):** انتخاب پروفایل تنظیمات (پیش‌فرض یا پروفایل سفارشی شما).

##### تب‌های انتخاب اشیاء دیتابیس:
- **جداول (Tables):** انتخاب گروهی یا انتخابی جداول.
- **ویوها (Views):** مهندسی معکوس ستون‌های خروجی ویو به کلاس مدل.
- **پروسیجرها (Stored Procedures):** تشخیص متادیتای ستون‌های خروجی (`Procedure_Result`) و پارامترهای ورودی (`Procedure_Parameters`).
- **توابع (Functions):** پشتیبانی از توابع جدول‌پایه (TVF) و اسکالر.
- **دکمه‌های انتخاب سریع:** `Select All`، `Deselect All` و فیلتر جستجوی لحظه‌ای براساس نام یا اسکیما.

##### تنظیمات عمومی مدل (General Model Settings):
- **Property Naming Style:** تعیین فرمت نام‌گذاری فیلدها شامل:
  - `PascalCase` (استاندارد C#)
  - `camelCase` (استاندارد Dart و Java)
  - `snake_case` (استاندارد Python)
  - `Original` (حفظ دقیق نام ستون در دیتابیس)
- **Strip Common Table/Object Prefixes:** حذف خودکار پیشوندهای متداول از نام کلاس‌ها (مانند `tbl_`، `vw_`، `sp_`، `usp_`).
- **Singularize Class Names:** مفردسازی هوشمند نام کلاس‌ها (مثلاً تبدیل جدول `Users` به کلاس `User` و `Categories` به `Category`).
- **Generate Navigation Properties:** تولید خودکار رفرنس‌ها و کالکشن‌های ارتباطی کلید خارجی (FK).
- **Include XML / Docstring Comments:** استخراج اتوماتیک توضیحات ستون‌ها (Extended Properties) از دیتابیس و قرار دادن آن به عنوان کامنت بالای هر پراپرتی.

##### خروجی و دانلود:
- پیش‌نمایش آنی کد با هایلایت سینتکس، شمارنده خطوط و تعداد کاراکترها.
- کپی در کلیپ‌بورد با یک کلیک یا دانلود فایل منفرد جاری.
- **دانلود آرشیو ZIP چندحالته:**
  - `Flat`: قرار گرفتن تمام فایل‌ها کنار هم در ریشه فایل زیپ.
  - `BySchema`: دسته‌بندی و پوشه‌بندی فایل‌ها بر اساس نام اسکیمای دیتابیس.
  - `ByNamespace`: پوشه‌بندی فایل‌ها دقیقاً بر اساس ساختار Namespace سی‌شارپ.

---

<a name="fa-csharp-advanced"></a>
#### ۳.۳. تنظیمات پروفایل و گزینه‌های پیشرفته سی‌شارپ (C# Advanced Config)
در بخش تنظیمات پروفایل، کنترل کاملی بر معماری کدهای C# وجود دارد:

##### ۱. تب ساختار و ریجن‌ها (Structure & Regions):
- **C# Type Kind:** انتخاب نوع تعریف (`class`، `record`، یا `struct`).
- **Property Accessor:** انتخاب نوع پراپرتی (`{ get; set; }` معمولی، `{ get; init; }` تغییرناپذیر، یا `backing_field` چندخطی).
- **Base Class & Interfaces:** تعیین نام کلاس پایه یا اینترفیس‌هایی که کلاس‌ها باید از آن‌ها ارث‌بری کنند (با پشتیبانی از توکن‌های `{TableName}` و `{ClassName}`).
- **Partial Class:** تولید کلاس به صورت `public partial class`.
- **Organize Class with #region Blocks:** تفکیک منظم پراپرتی‌ها، کلیدها، فیلدهای پشتیبان و متدها در بلوک‌های `#region`.
- **Outer Region Name:** قرار دادن کل اعضای کلاس در یک ریجن سراسری (مثلاً `#region Generator`).
- **Class Constants Definition:** تعریف ثابت‌های اختصاصی برای هر کلاس با توکن‌های `{TableName}`، `{SchemaName}`، `{ClassName}` و `{Database}`.
- **Table & Schema Overrides:** تولید خودکار متدهای کمکی نظیر `GetTableName()` و `GetSchemaName()`.

##### ۲. تب فضا‌های نام (Namespace & Usings):
- **Namespace Mode:**
  - `Single`: استفاده از یک Namespace سراسری برای کل پروژه.
  - `PerSchema`: نگاشت و تفکیک خودکار Namespace به ازای هر اسکیمای دیتابیس (مثلاً `MyProject.Sales` برای اسکیمای Sales و `MyProject.HR` برای اسکیمای HR).
- **Additional Custom Usings:** افزودن Usingهای دلخواه به ابتدای تمامی فایل‌ها.

---

<a name="fa-annotations"></a>
#### ۳.۴. مدیریت منعطف انوتیشن‌ها (Annotations Management)
در تب **Custom Attributes**، سیستمی مدرن برای کنترل DataAnnotations تعبیه شده است:

| قابلیت / انوتیشن | کاربرد و تنظیمات موجود |
| :--- | :--- |
| **`[Required]` هوشمند** | دارای سوییچ فعال‌سازی، با قابلیت تعیین **محدوده اعمال (Scope)**:<br>• `AllNonNullable`: اعمال روی همه فیلدهای اجباری (از جمله اعداد، تاریخ، Guid و انواع Value Type)<br>• `StringsAndByteArrays`: فقط فیلدهای مرجع متنی و آرایه بایت<br>• `NonIdentity`: همه فیلدهای اجباری به‌جز فیلدهای اتواینکرمنت<br>• کلید اختصاصی شمول/عدم شمول روی کلیدهای اصلی (PK)<br>• قالب پیام خطای سفارشی (`ErrorMessage`) با توکن‌های `{PropertyName}` و `{ColumnName}` |
| **`[Column]` هوشمند** | نحوه درج نام ستون:<br>• `WhenNameDiffers`: فقط وقتی نام فیلد در سی‌شارپ با دیتابیس متفاوت است (تمیزترین حالت)<br>• `Always`: درج روی تمام فیلدها<br>• `Never`: عدم درج اتریبیوت<br>• گزینه درج نوع دقیق دیتابیس: `[Column(TypeName = "varchar(50)")]` |
| **`[StringLength]` و `[MaxLength]`** | امکان انتخاب اینکه برای ستون‌های متنی طول‌دار، ویژگی `[StringLength(n)]` تولید شود یا `[MaxLength(n)]`. |
| **`[Key]` و Identity** | تولید ویژگی `[Key]` برای کلیدهای اصلی و `[DatabaseGenerated(Identity)]` برای ستون‌های اتواینکرمنت. |
| **`[Timestamp]`** | تولید اتریبیوت `[Timestamp]` برای ستون‌های همزمانی (RowVersion / Concurrency). |
| **`[Keyless]` و Viewها** | تولید خودکار `[Keyless]` و `.HasNoKey()` در EF Core برای ویوها و پروسیجرهای بدون کلید اصلی تا در زمان اجرا خطایی رخ ندهد. |
| **`[Display]` و `[Description]`** | تولید خودکار `[Display(Name = "...")]` با عناوین خوانا و انسانی، و `[Description]` از روی توضیحات دیتابیس. |
| **اتریبیوت‌های سفارشی و قوانین شرطی** | اعمال قوانین شرطی بر اساس الگو (مثلاً `*Date:[Auditable]` یا `uniqueidentifier:[GuidColumn]`) و اتریبیوت‌های سراسری کلاس و پراپرتی‌ها. |

---

<a name="fa-transformations"></a>
#### ۳.۵. موتور تبدیل پیشرفته پراپرتی‌ها (Property Transformations)
امکان تغییر نوع و رفتار پراپرتی‌ها بر اساس الگوهای نام ستون یا نوع داده:
- **تولید ویژگی کمکی `[NotMapped]`:** تولید پراپرتی الحاقی که در دیتابیس ذخیره نمی‌شود اما داده را به شیء ساخت‌یافته تبدیل می‌کند.
- **تغییر مستقیم نوع داده (Type Override):** مثلاً تبدیل خودکار ستون عددی `StatusId` به Enum متناظر در C#.
- **قالب‌های آماده با یک کلیک (Quick Templates):**
  - **JSON to JsonDocument:** تبدیل خودکار فیلدهای متنی JSON به `JsonDocument?` به همراه پارس و سریالایز خودکار.
  - **XML to XElement:** تبدیل ستون‌های XML به شیء `XElement?`.
  - **CSV to List<string>:** تبدیل رشته‌های کاما-جدا به لیست رشته.
  - **Int to Enum:** تبدیل ستون وضعیت عددی به Enum دلخواه.

---

<a name="fa-schema-access"></a>
#### ۳.۶. کنترل دسترسی اسکیماها (Schema Access Control)
در پروژه‌های بزرگ با معماری **Modular Monolith** یا **Domain-Driven Design (DDD)**، وابستگی‌های کنترل‌نشده بین اسکیماها می‌تواند معماری را تخریب کند:
- این قابلیت به شما اجازه می‌دهد روابط مجاز بین اسکیماها را مشخص کنید.
- اگر یک اسکیما اجازه دسترسی به اسکیمای دیگر را نداشته باشد، کلیدهای خارجی و ارجاعات ناوبری (Navigation Properties) بین آن‌ها در مدل خروجی حذف خواهد شد.
- دارای امکان ایزوله‌سازی کامل (Completely Isolated Schema) بدون هیچ خروجی وابستگی.

---

<a name="fa-other-languages"></a>
#### ۳.۷. تنظیمات زبان‌های Python ،Dart و Java
- **Python:**
  - سبک‌ها: `Pydantic (BaseModel)`، `SQLAlchemy (ORM)`، `Dataclass` یا `TypedDict`.
  - تنظیم Type Hints و مقادیر پیش‌فرض.
- **Dart (Flutter):**
  - سبک‌ها: `Plain Dart`، `JsonSerializable` یا `Freezed`.
  - پشتیبانی کامل از Null Safety و تولید فیلدهای `final`.
- **Java:**
  - سبک‌ها: `Java 16+ Record`، `JPA / Hibernate Entity`، `Lombok (@Data, @Builder)` یا کلاس استاندارد با Getters/Setters.
  - افزودن انوتیشن‌های Jackson (`@JsonProperty`) و پشتیبانی از `Serializable`.

---

<a name="fa-script-generator"></a>
#### ۳.۸. تولید اسکریپت پایگاه‌داده (Script Generator)
- امکان انتخاب دسته‌ای جداول، ویوها، پروسیجرها و توابع.
- گزینه‌های تولید اسکریپت:
  - ساخت ساختار (`CREATE`)
  - حذف و ساخت مجدد (`DROP AND CREATE`)
  - استخراج داده‌ها به صورت دستورات `INSERT` (همراه با مدیریت `IDENTITY_INSERT`)
- تحلیل وابستگی‌ها و ایجاد اسکریپت مرتب بر اساس اولویت کلیدهای خارجی.

---

<a name="fa-config-backup"></a>
#### ۳.۹. پشتیبان‌گیری و بازگردانی تنظیمات (Config Backup & Restore)
- خروجی گرفتن از تمام سرورهای متصل، پروفایل‌های مدل‌ساز و تنظیمات در قالب یک فایل رمزنگاری‌شده `.json`.
- امکان بازگردانی سریع در کامپیوتر یا محیط‌های دیگر با پیش‌نمایش تعداد سرورها و پروفایل‌ها پیش از اعمال نهایی.

---

<a name="fa-logs"></a>
#### ۳.۱۰. لاگ و مانیتورینگ سیستم (System Logs)
- سیستم لاگ اختصاصی Rolling File در مسیر محلی بدون وابستگی سنگین.
- ثبت تمام رخدادهای تولید کد، خطاهای اتصال به دیتابیس و عملیات بکاپ/ریستور.
- امکان جستجو در متن پیام‌ها، فیلتر بر اساس شدت لاگ (اطلاعات، هشدار، خطا) و مشاهده جزئیات Exception در مودال اختصاصی.

---

<a name="fa-getting-started"></a>
### ۴. پیش‌نیازها و راه‌اندازی

#### پیش‌نیازها:
- [.NET 10.0 SDK](https://dotnet.microsoft.com/) یا بالاتر.
- دسترسی به یک سرور Microsoft SQL Server (لوکال یا ریموت).

#### اجرای سریع (Development):
```bash
# کلون ریپازیتوری
git clone https://github.com/husein83/BprDevKit.git
cd BprDevKit

# اجرای پروژه
dotnet run --project SourceCode/BprDevKit.csproj
```
مرورگر خود را باز کرده و وارد آدرس `http://localhost:5000` یا `https://localhost:5001` شوید.

---

<a name="fa-release"></a>
### ۵. ساخت نسخه مستقل (Release & Publish)

#### ساخت نسخه پرتابل (Self-Contained برای ویندوز بدون نیاز به نصب بودن .NET روی سیستم مقصد):
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o Release/Standalone SourceCode/BprDevKit.csproj
```
فایل خروجی `Release/Standalone/BprDevKit.exe` را می‌توانید به راحتی جابه‌جا کرده و روی هر سیستم ویندوز ۶۴ بیتی با دو بار کلیک اجرا فرمایید.

---

<br><br>

---

<a name="-complete-english-documentation"></a>
# 🇬🇧 Complete English Documentation

## 📑 English Table of Contents
1. [Overview & Philosophy](#en-overview)
2. [Architecture & Tech Stack](#en-architecture)
3. [Step-by-Step Feature Walkthrough](#en-features)
   - [3.1. Database Management](#en-db-management)
   - [3.2. Data Model Generator](#en-model-generator)
   - [3.3. Advanced C# Architecture & Settings](#en-csharp-advanced)
   - [3.4. Fine-Grained Annotations Management](#en-annotations)
   - [3.5. Property Transformation Engine](#en-transformations)
   - [3.6. Schema Access Control](#en-schema-access)
   - [3.7. Python, Dart, and Java Generators](#en-other-languages)
   - [3.8. Script Generator](#en-script-generator)
   - [3.9. Configuration Backup & Restore](#en-config-backup)
   - [3.10. System Logging & Audit](#en-logs)
4. [Prerequisites & Getting Started](#en-getting-started)
5. [Build & Release Guide](#en-release)

---

<a name="en-overview"></a>
### 1. Overview & Philosophy
**BprDevKit** is an enterprise-grade developer productivity suite designed to bridge the gap between relational databases and modern software codebases. Instead of writing boilerplate entities, DTOs, or schema scripts by hand, BprDevKit reverses engineers your database structures into clean, highly customizable domain models across multiple programming languages.

---

<a name="en-architecture"></a>
### 2. Architecture & Tech Stack
- **Core Framework:** Microsoft .NET 10.0 (ASP.NET Core Blazor Server)
- **Language:** C# 13
- **Frontend / Styling:** Bootstrap 5 with native dual-mode RTL (Right-to-Left) and LTR (Left-to-Right) layout
- **Database Targets:** Microsoft SQL Server (2016 through 2022, Azure SQL Database)
- **Internal Design:** Domain-driven modular design, native rolling file logger, client-side AES encrypted configuration store, and zero heavy third-party dependencies.

---

<a name="en-features"></a>
### 3. Step-by-Step Feature Walkthrough

<a name="en-db-management"></a>
#### 3.1. Database Management
- **Complete Object Explorer:** View schema and manage Tables, Columns, Primary Keys (PK), Foreign Keys (FK), Indexes, Check Constraints, Triggers, Views, Functions, and Stored Procedures.
- **Protected Object Decryption:** Decompile and inspect definition source code for Stored Procedures, Views, Triggers, and Functions created `WITH ENCRYPTION`.
- **Advanced Database Backup:**
  - **Server Disk Browser:** Interactively browse remote or local server disk drives and folders to pick any destination folder, freeing you from default SQL backup directories.
  - **Automatic Timestamps:** Auto-formats filenames cleanly as `DatabaseName_YYYY-MM-DD_HH-mm-ss.bak`.
  - **Backup Options:** Support for native SQL compression (`WITH COMPRESSION`) and isolated copy backups (`COPY_ONLY`).
- **Advanced Database Restore:**
  - Browse server disks to pick `.bak` files directly.
  - Overwrite option (`WITH REPLACE`).
  - Active connection termination (`ROLLBACK IMMEDIATE`).

---

<a name="en-model-generator"></a>
#### 3.2. Data Model Generator
Fast 3-step generation workflow:
1. **Step 1:** Select active server connection.
2. **Step 2:** Select target database.
3. **Step 3:** Select or customize a configuration profile.

##### Object Selection Tabs:
- **Tables:** Full entity reverse-engineering.
- **Views:** Generates read-only and keyless models.
- **Stored Procedures:** Extracts metadata for output query results (`Procedure_Result`) and input parameter DTOs (`Procedure_Parameters`).
- **Functions:** Table-Valued Functions (TVF) and Scalar Function results.
- **Quick Controls:** `Select All`, `Deselect All`, and real-time schema/name search filtering.

##### General Model Settings:
- **Property Naming Style:**
  - `PascalCase`: Recommended for C#.
  - `camelCase`: Recommended for Dart and Java.
  - `snake_case`: Recommended for Python.
  - `Original`: Exact match to SQL database column casing.
- **Strip Common Table/Object Prefixes:** Removes prefixes like `tbl_`, `tbl`, `vw_`, `sp_`, `usp_`, `fn_` from generated class names.
- **Singularize Class Names:** Converts plural table names into singular entity names (e.g., `Users` $\rightarrow$ `User`, `Companies` $\rightarrow$ `Company`), while intelligently preserving View and Stored Procedure names.
- **Generate Navigation Properties:** Automatically creates reference and collection navigation properties for foreign keys.
- **Include XML / Docstring Comments:** Extracts database extended properties and column descriptions into XML documentation comments (`/// <summary>`) or docstrings.

##### Output & Packaging:
- Instant syntax-highlighted code preview with character and line counters.
- One-click clipboard copying and single-file downloads.
- **Multi-Structure ZIP Packaging:**
  - `Flat`: All generated files placed in a single root archive.
  - `BySchema`: Organized into dedicated folders by database schema (e.g., `dbo/`, `Sales/`).
  - `ByNamespace`: Organized into directories matching the C# namespace hierarchy.

---

<a name="en-csharp-advanced"></a>
#### 3.3. Advanced C# Architecture & Settings
Comprehensive options accessible via profile management:

##### 1. Structure & Regions:
- **C# Type Kind:** Choose between `class`, `record` (immutable data structures), or `struct`.
- **Property Accessor:** Choose `{ get; set; }`, `{ get; init; }`, or multi-line `backing_field` (`private _field`).
- **Base Class & Interfaces:** Specify inheritance using token variables (`{TableName}`, `{ClassName}`).
- **Partial Class:** Adds `partial` modifier to class signatures.
- **#region Blocks:** Segregates fields, properties, foreign keys, and constructors cleanly.
- **Outer Region Name:** Wraps entire class body in an overarching region (e.g., `#region Generator`).
- **Class Constants Definition:** Define compile-time constants per entity with token helpers (`{TableName}`, `{SchemaName}`, `{ClassName}`, `{Database}`).
- **Table & Schema Overrides:** Automatically outputs helper methods such as `GetTableName()` and `GetSchemaName()`.

##### 2. Namespace & Usings:
- **Namespace Mode:**
  - `Single`: Applies one global namespace to all generated files.
  - `PerSchema`: Automatically maps namespaces based on database schemas (e.g., `App.Core.Sales`, `App.Core.Inventory`).
- **Additional Custom Usings:** Injects custom using directives across all files.

---

<a name="en-annotations"></a>
#### 3.4. Fine-Grained Annotations Management
Located under the **Custom Attributes** tab:

| Attribute Feature | Capabilities & Configurations |
| :--- | :--- |
| **Flexible `[Required]`** | Master switch with configurable **Inclusion Scope**:<br>• `AllNonNullable`: Generates on all required columns, including value types (`int`, `DateTime`, `Guid`, `decimal`)<br>• `StringsAndByteArrays`: Generates only on reference types<br>• `NonIdentity`: Generates on all mandatory fields except auto-increment identity columns<br>• Dedicated Primary Key toggle (`RequiredIncludePrimaryKeys`)<br>• Customizable `ErrorMessage` template with token buttons: `{PropertyName}`, `{ColumnName}` |
| **Flexible `[Column]`** | Placement mode selector:<br>• `WhenNameDiffers`: Only when C# property name differs from database column name (Clean Code recommended)<br>• `Always`: Generates on every property<br>• `Never`: Omits `[Column]`<br>• Optional SQL TypeName inclusion: `[Column(TypeName = "varchar(100)")]` |
| **`[StringLength]` & `[MaxLength]`** | Choice between `[StringLength(n)]` and `[MaxLength(n)]` for character columns. |
| **`[Key]` & Identity** | Generates `[Key]` for primary keys and `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` for auto-increment fields. |
| **`[Timestamp]`** | Concurrency token attribute for `rowversion` / `timestamp` fields. |
| **`[Keyless]` for Views** | Generates `[Keyless]` and `.HasNoKey().ToView(...)` in EF Core for Views and Procedures without primary keys to prevent runtime query exceptions. |
| **`[Display]` & `[Description]`** | Generates `[Display(Name = "...")]` with human-readable titles, and `[Description]` from database documentation. |
| **Conditional Column Rules** | Define wildcard rules (e.g., `*Date:[Auditable]`, `uniqueidentifier:[GuidColumn]`) plus class-level and property-level custom attributes. |

---

<a name="en-transformations"></a>
#### 3.5. Property Transformation Engine
Dynamic rule engine that alters how columns are represented in the generated models:
- **`[NotMapped]` Helper Properties:** Adds calculated or parsed properties without altering the primary database column.
- **Type Overrides:** Directly changes property types (e.g., mapping an integer status column to a domain Enum).
- **One-Click Pre-built Templates:**
  - **JSON to JsonDocument:** Converts JSON columns into `JsonDocument?` with automated parsing and serialization getters/setters.
  - **XML to XElement:** Converts XML columns into `XElement?`.
  - **CSV to List<string>:** Converts comma-delimited text into `List<string>?`.
  - **Int to Enum:** Binds integer flags directly to strongly-typed C# enums.

---

<a name="en-schema-access"></a>
#### 3.6. Schema Access Control
Enforce architectural boundaries for **Modular Monoliths** or **Domain-Driven Design (DDD)**:
- Define explicit allowed dependency pathways between database schemas.
- If Schema A is forbidden from referencing Schema B, all cross-schema foreign keys and navigation properties between them are omitted during generation.
- Supports marking schemas as **Completely Isolated**.

---

<a name="en-other-languages"></a>
#### 3.7. Python, Dart, and Java Generators
- **Python:**
  - Architecture options: `Pydantic (BaseModel)`, `SQLAlchemy (ORM)`, `Dataclass`, or `TypedDict`.
  - Configurable type hints and default values.
- **Dart (Flutter):**
  - Architecture options: `Plain Dart`, `JsonSerializable`, or `Freezed`.
  - Full Null Safety support and `final` field modifiers.
- **Java:**
  - Architecture options: `Java 16+ Record`, `JPA / Hibernate Entity`, `Lombok (@Data, @Builder)`, or standard POJO with getters/setters.
  - Jackson annotations (`@JsonProperty`) and `Serializable` support.

---

<a name="en-script-generator"></a>
#### 3.8. Script Generator
- Select any combination of database objects (Tables, Views, Procedures, Functions).
- Script generation modes:
  - `CREATE`
  - `DROP AND CREATE`
  - `DATA (INSERT)` with automated `IDENTITY_INSERT` wrapping
- Foreign key dependency resolution ensures scripts execute without constraint violations.

---

<a name="en-config-backup"></a>
#### 3.9. Configuration Backup & Restore
- Export all server connection details, generator profiles, and app settings into an encrypted `.json` configuration file.
- Restore onto other developer workstations or environments with a pre-import summary verification.

---

<a name="en-logs"></a>
#### 3.10. System Logging & Audit
- Built-in rolling file logging engine with zero external dependencies.
- Logs all model generations, database connection events, and backup/restore executions.
- Real-time search, severity filtering (Info, Warning, Error), and full stack trace inspection modal.

---

<a name="en-getting-started"></a>
### 4. Prerequisites & Getting Started

#### Prerequisites:
- [.NET 10.0 SDK](https://dotnet.microsoft.com/) or higher.
- Microsoft SQL Server instance (local or remote).

#### Quick Start:
```bash
# Clone the repository
git clone https://github.com/husein83/BprDevKit.git
cd BprDevKit

# Run development server
dotnet run --project SourceCode/BprDevKit.csproj
```
Open your browser and navigate to `http://localhost:5000` or `https://localhost:5001`.

---

<a name="en-release"></a>
### 5. Build & Release Guide

#### Build Self-Contained Standalone Executable (Windows x64 - No .NET installation required on target machine):
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o Release/Standalone SourceCode/BprDevKit.csproj
```
The output `Release/Standalone/BprDevKit.exe` can be distributed as a portable standalone executable.

---

<div align="center">

Made with ❤️ for software engineers and database professionals.  
**BprDevKit © 2026**

</div>
