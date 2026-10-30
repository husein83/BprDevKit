# BprDevKit (Bpr Developer Toolkit) 🛠️

<div align="center">

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor&logoColor=white)
![C#](https://img.shields.io/badge/C%23-13.0-239120?logo=csharp&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-5.0+-3178C6?logo=typescript&logoColor=white)
![Databases](https://img.shields.io/badge/Databases-SQL%20Server%20%7C%20Oracle%20%7C%20PostgreSQL%20%7C%20MySQL%20%7C%20MariaDB%20%7C%20SQLite-blue)
![UI](https://img.shields.io/badge/UI-Bootstrap%205%20%7C%20Dark%20Mode%20%7C%20RTL-7952B3?logo=bootstrap&logoColor=white)
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
3. [پایگاه‌های داده تحت پشتیبانی](#fa-supported-databases)
4. [راهنمای ماژول‌ها و امکانات قدم‌به‌قدم](#fa-modules)
   - [۴.۱. مدیریت پایگاه‌داده و کنسول اجرای کوئری (Database Management & Query Runner)](#fa-db-management)
   - [۴.۲. تولید دیتامدل چندزبانه (Data Model Generator)](#fa-model-generator)
   - [۴.۳. تنظیمات پیشرفته سی‌شارپ، DbContext و Fluent API](#fa-csharp-advanced)
   - [۴.۴. مدیریت منعطف انوتیشن‌ها (Annotations Management)](#fa-annotations)
   - [۴.۵. موتور تبدیل پراپرتی‌ها و همگام‌سازی دوطرفه JSON به Dictionary](#fa-transformations)
   - [۴.۶. کنترل دسترسی اسکیماها (Schema Access Control)](#fa-schema-access)
   - [۴.۷. پشتیبانی از زبان‌های TypeScript ،Python ،Dart و Java](#fa-other-languages)
   - [۴.۸. تولید اسکریپت پایگاه‌داده (Script Generator)](#fa-script-generator)
   - [۴.۹. پشتیبان‌گیری و بازگردانی تنظیمات (Config Backup & Restore)](#fa-config-backup)
   - [۴.۱۰. تنظیمات نرم‌افزار و پالت اختصاصی تم تاریک (Dark Mode)](#fa-app-settings)
   - [۴.۱۱. لاگ و مانیتورینگ سیستم (System Logs)](#fa-logs)
5. [پیش‌نیازها و راه‌اندازی](#fa-getting-started)
6. [ساخت نسخه مستقل و پرتابل (Release & Publish)](#fa-release)

---

<a name="fa-overview"></a>
### ۱. معرفی BprDevKit
**BprDevKit** یک پلتفرم تحت وب پیشرفته، سبک و سریع است که با هدف خودکارسازی کارهای زمان‌بر دیتابیس و برنامه‌نویسی بک‌اند توسعه یافته است. این ابزار به توسعه‌دهندگان و مدیران دیتابیس (DBA) اجازه می‌دهد به جای صرف ساعت‌ها زمان برای نوشتن دستی کلاس‌های دیتامدل، مدیریت دستی اسکریپت‌ها یا اتصال جداگانه به نرم‌افزارهای مدیریتی سنگین، تمام این فرآیندها را در یک داشبورد مدرن، دوزبانه و استاندارد انجام دهند.

---

<a name="fa-architecture"></a>
### ۲. معماری و تکنولوژی‌ها
- **فریم‌ورک هسته:** Microsoft .NET 10.0 (ASP.NET Core Blazor Server)
- **زبان‌های پشتیبانی‌شده برای تولید مدل:** C# 13 ،TypeScript ،Python 3 ،Dart و Java 17+
- **رابط کاربری:** Bootstrap 5 با پشتیبانی بومی از راست‌چین (RTL) و چپ‌چین (LTR)، همراه با پالت تیره اختصاصی، ارگونومیک و بدون خستگی چشم (Dark Mode).
- **سیستم ذخیره‌سازی پیکربندی:** رمزنگاری محلی AES-256 فایل‌های تنظیمات بدون نیاز به دیتابیس واسط.
- **معماری نرم‌افزار:** تفکیک ماژولار Domain-Driven، بدون وابستگی‌های سنگین خارجی، دارای لاگر چرخشی اختصاصی (Rolling File Logger).

---

<a name="fa-supported-databases"></a>
### ۳. پایگاه‌های داده تحت پشتیبانی
BprDevKit از طیف گسترده‌ای از محبوب‌ترین پایگاه‌های داده رابطه‌ای پشتیبانی می‌کند و ژنراتورهای اختصاصی SQL و مهندسی معکوس متادیتا برای هر کدام پیاده‌سازی شده است:

| پایگاه داده | نسخه و توضیحات | درایور اتصال |
| :--- | :--- | :--- |
| **Microsoft SQL Server** | نسخه‌های 2016 تا 2022 و Azure SQL Database | `Microsoft.Data.SqlClient` |
| **Oracle Database** | نسخه‌های 11g تا 23ai با پشتیبانی از کاتالوگ‌های سراسری اوراکل | `Oracle.ManagedDataAccess.Core` |
| **PostgreSQL** | نسخه‌های 10 به بالا با پشتیبانی از اسکیماها و انواع داده پیشرفته | `Npgsql` |
| **MySQL** | نسخه‌های 5.7 و 8.0 به بالا | `MySqlConnector` |
| **MariaDB** | نسخه‌های 10.3 به بالا | `MySqlConnector` |
| **SQLite** | پایگاه داده سبک فایل‌محور SQLite 3 | `Microsoft.Data.Sqlite` |

---

<a name="fa-modules"></a>
### ۴. راهنمای ماژول‌ها و امکانات قدم‌به‌قدم

<a name="fa-db-management"></a>
#### ۴.۱. مدیریت پایگاه‌داده و کنسول اجرای کوئری (Database Management & Query Runner)
این ماژول امکانات جامعی را برای مدیریت اشیاء، داده‌ها و کوئری‌زدن مستقیم فراهم می‌کند:
- **مدیریت کامل اشیاء:** مرور، مشاهده ساختار و تغییرات در جداول (Tables)، ستون‌ها، کلیدهای اصلی (PK)، کلیدهای خارجی (FK)، ایندکس‌ها، قیود چک (Check Constraints)، تریگرها (Triggers)، ویوها (Views)، توابع (Functions) و پروسیجرها (Stored Procedures).
- **کنسول اجرای سریع کوئری (Quick SQL Query Runner):** 
  - تب اختصاصی در صفحه مدیریت دیتابیس جهت نوشتن و اجرای سریع دستورات و کوئری‌های SQL بدون نیاز به باز کردن ابزارهای خارجی.
  - نمایش داده‌ها به صورت جدول ساخت‌یافته و پویا، نمایش زمان اجرای کوئری، تعداد رکوردهای تحت تاثیر، و پیام‌های خطا یا وضعیت به همراه قابلیت پاکسازی سریع.
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
#### ۴.۲. تولید دیتامدل چندزبانه (Data Model Generator)
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
  - `Original` (پیش‌فرض، حفظ دقیق نام ستون در دیتابیس بدون دستکاری)
  - `PascalCase` (استاندارد C#)
  - `camelCase` (استاندارد TypeScript ،Dart و Java)
  - `snake_case` (استاندارد Python)
- **Strip Common Table/Object Prefixes:** حذف پیشوندهای متداول جداول و آبجکت‌ها از نام کلاس‌ها (مانند `tbl_`، `vw_`، `sp_`، `usp_`) — پیش‌فرض: غیرفعال (`false`).
- **Strip Common Column/Field Prefixes:** حذف پیشوندهای فنی ستون‌ها و فیلدها از نام پراپرتی‌ها (مانند `fld_`، `col_`) — پیش‌فرض: غیرفعال (`false`).
- **Singularize Class Names:** مفردسازی هوشمند نام کلاس‌ها (مثلاً تبدیل جدول `Users` به کلاس `User`) — پیش‌فرض: غیرفعال (`false`).
- **Generate Navigation Properties:** تولید خودکار رفرنس‌ها و کالکشن‌های ارتباطی کلید خارجی (FK). در تمام کلاس‌ها، ریجن‌های `#region References` و `#region Relations` حتی در صورت عدم وجود کلید خارجی برای حفظ ساختار یکنواخت درج می‌شوند.
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
#### ۴.۳. تنظیمات پیشرفته سی‌شارپ، DbContext و Fluent API
در بخش تنظیمات پروفایل، کنترل کاملی بر معماری کدهای C# و Entity Framework Core وجود دارد:

##### ۱. ساختار کلاس و ریجن‌ها (Structure & Regions):
- **C# Type Kind:** انتخاب نوع تعریف (`class`، `record`، یا `struct`).
- **Property Accessor:** انتخاب نوع پراپرتی (`{ get; set; }` معمولی، `{ get; init; }` تغییرناپذیر، یا `backing_field` چندخطی).
- **Base Class & Interfaces:** تعیین نام کلاس پایه یا اینترفیس‌هایی که کلاس‌ها باید از آن‌ها ارث‌بری کنند (با پشتیبانی از توکن‌های `{TableName}` و `{ClassName}`).
- **Partial Class:** تولید کلاس به صورت `public partial class`.
- **Organize Class with #region Blocks:** تفکیک منظم پراپرتی‌ها، کلیدها، فیلدهای پشتیبان و رفرنس‌ها در بلوک‌های `#region`. ریجن‌های References و Relations حتی برای کلاس‌های فاقد کلید خارجی جهت یکدستی سورس‌کد همیشه ایجاد می‌شوند.
- **Class Constants Definition:** تعریف ثابت‌های اختصاصی برای هر کلاس با توکن‌های `{TableName}`، `{SchemaName}`، `{ClassName}` و `{Database}`.

##### ۲. تولید خودکار DbContext و Fluent API Configurations:
- **Generate DbContext:** تولید خودکار کلاس متنی `DbContext` شامل تمام `DbSet<T>`های جداول و ویوها، متدهای کانفیگ و تنظیمات پیشرفته.
  - **DbContext Namespace:** امکان تعیین فضانام اختصاصی برای کانتکست (مانند `{Database}.Context` یا `MyProject.Data`).
  - **Use Fully-Qualified Model Names:** جلوگیری از تداخل اسامی جداول هم‌نام در اسکیماهای مختلف با ارجاع دقیق به `DbSet<global::Namespace.ClassName>`.
- **Generate EntityTypeConfiguration (Fluent API):**
  - تولید فایل‌های مجزای `{ClassName}Configuration.cs` پیاده‌سازی‌کننده `IEntityTypeConfiguration<T>`.
  - نگاشت کلیدهای اصلی (`builder.HasKey`)، ستون‌های افزایشی Identity، طول فیلدها، نوع ستون‌های دیتابیس، نام جدول و اسکیما (`builder.ToTable`).
  - نگاشت کامل روابط کلید خارجی با رفتارهای ارجاعی (`OnDelete`).

---

<a name="fa-annotations"></a>
#### ۴.۴. مدیریت منعطف انوتیشن‌ها (Annotations Management)
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
#### ۴.۵. موتور تبدیل پراپرتی‌ها و همگام‌سازی دوطرفه JSON به Dictionary
امکان تغییر نوع و رفتار پراپرتی‌ها بر اساس الگوهای نام ستون یا نوع داده:
- **تولید ویژگی کمکی `[NotMapped]` با همگام‌سازی دوطرفه (Two-Way Sync):**
  - فیلد رشته‌ای متناظر با دیتابیس مقدار JSON خود را از متغیرهای دیکشنری می‌خواند.
  - فیلد دیکشنری `Dictionary<string, object?>` به صورت خودکار تغییرات را با رشته JSON پشت‌صحنه (`_payloadJson`) همگام می‌کند و موقع خواندن از دیتابیس نیز متن را به ساختار دیکشنری تبدیل می‌نماید.
- **تغییر مستقیم نوع داده (Type Override):** مثلاً تبدیل خودکار ستون عددی `StatusId` به Enum متناظر در C#.
- **قالب‌های آماده با یک کلیک (Quick Templates):**
  - **JSON to Dictionary:** تبدیل خودکار فیلدهای متنی JSON به دیکشنری با قابلیت فعال‌سازی سوییچ دوطرفه.
  - **JSON to JsonDocument:** تبدیل ستون‌های متنی JSON به `JsonDocument?` به همراه پارس و سریالایز خودکار.
  - **XML to XElement:** تبدیل ستون‌های XML به شیء `XElement?`.
  - **CSV to List<string>:** تبدیل رشته‌های کاما-جدا به لیست رشته.
  - **Int to Enum:** تبدیل ستون وضعیت عددی به Enum دلخواه.

---

<a name="fa-schema-access"></a>
#### ۴.۶. کنترل دسترسی اسکیماها (Schema Access Control)
در پروژه‌های بزرگ با معماری **Modular Monolith** یا **Domain-Driven Design (DDD)**، وابستگی‌های کنترل‌نشده بین اسکیماها می‌تواند معماری را تخریب کند:
- این قابلیت به شما اجازه می‌دهد روابط مجاز بین اسکیماها را مشخص کنید.
- اگر یک اسکیما اجازه دسترسی به اسکیمای دیگر را نداشته باشد، کلیدهای خارجی و ارجاعات ناوبری (Navigation Properties) بین آن‌ها در مدل خروجی حذف خواهد شد.
- دارای امکان ایزوله‌سازی کامل (Completely Isolated Schema) بدون هیچ خروجی وابستگی.

---

<a name="fa-other-languages"></a>
#### ۴.۷. پشتیبانی از زبان‌های TypeScript ،Python ،Dart و Java
- **TypeScript:**
  - سبک‌های اعلان: `interface`، `type alias`، یا `class`.
  - گزینه‌های کاربردی: اعمال کلمه کلیدی `readonly` روی کلیدهای اصلی (PK)، تبدیل انواع تاریخ به شیء `Date` یا رشته استاندارد ایزو `string`، درج دستورات `export` و ایمپورت‌های سفارشی.
  - تولید کامل ارتباطات ناوبری (Parent Navigation و کالکشن‌های فرزند `Child[]`).
- **Python:**
  - سبک‌ها: `Pydantic (BaseModel)`، `SQLAlchemy (ORM)`، `Dataclass` یا `TypedDict`.
  - تنظیم Type Hints و مقادیر پیش‌فرض.
- **Dart (Flutter):**
  - سبک‌ها: `Plain Dart`، `JsonSerializable` یا `Freezed`.
  - پشتیبانی کامل از Null Safety و تولید فیلدهای `final`.
- **Java:**
  - سبک‌ها: `Java 17+ Record`، `JPA / Hibernate Entity`، `Lombok (@Data, @Builder)` یا کلاس استاندارد با Getters/Setters.
  - افزودن انوتیشن‌های Jackson (`@JsonProperty`) و پشتیبانی از `Serializable`.

---

<a name="fa-script-generator"></a>
#### ۴.۸. تولید اسکریپت پایگاه‌داده (Script Generator)
- امکان انتخاب دسته‌ای جداول، ویوها، پروسیجرها و توابع در انواع دیتابیس‌ها.
- گزینه‌های تولید اسکریپت:
  - ساخت ساختار (`CREATE`)
  - حذف و ساخت مجدد (`DROP AND CREATE`)
  - استخراج داده‌ها به صورت دستورات `INSERT` (همراه با مدیریت `IDENTITY_INSERT`)
- تحلیل وابستگی‌ها و ایجاد اسکریپت مرتب بر اساس اولویت کلیدهای خارجی.

---

<a name="fa-config-backup"></a>
#### ۴.۹. پشتیبان‌گیری و بازگردانی تنظیمات (Config Backup & Restore)
- خروجی گرفتن از تمام سرورهای متصل، پروفایل‌های مدل‌ساز و تنظیمات در قالب یک فایل رمزنگاری‌شده `.json`.
- امکان بازگردانی سریع در کامپیوتر یا محیط‌های دیگر با پیش‌نمایش تعداد سرورها و پروفایل‌ها پیش از اعمال نهایی.

---

<a name="fa-app-settings"></a>
#### ۴.۱۰. تنظیمات نرم‌افزار و پالت اختصاصی تم تاریک (Dark Mode)
- **پایداری کامل تم (Theme Persistence):** تم انتخابی کاربر (حالت روشن / حالت تاریک) مستقیماً در تنظیمات نرم‌افزار (`ApplicationConfiguration`) ذخیره می‌شود و با جابه‌جایی بین صفحات و تب‌ها هرگز ریست نمی‌شود.
- **پالت تاریک لوکس و چشم‌نواز:** طراحی ارگونومیک سطوح تیره (`#0b0f19`) با گرادینت‌های نوری کیهانی، حذف کارت‌های سفید زننده، کنتراست بالای متون و فونت‌ها، و هایلایت‌های شفاف فیروزه‌ای (`#38bdf8`).
- **سوئیچ سریع در سایدبار:** دکمه شیشه‌ای مدرن در انتهای سایدبار برای سوئیچ آنی تم همراه با هماهنگی کامل سمت سرور.

---

<a name="fa-logs"></a>
#### ۴.۱۱. لاگ و مانیتورینگ سیستم (System Logs)
- سیستم لاگ اختصاصی Rolling File در مسیر محلی بدون وابستگی سنگین.
- ثبت تمام رخدادهای تولید کد، خطاهای اتصال به دیتابیس و عملیات بکاپ/ریستور.
- امکان جستجو در متن پیام‌ها، فیلتر بر اساس شدت لاگ (اطلاعات، هشدار، خطا) و مشاهده جزئیات Exception در مودال اختصاصی.

---

<a name="fa-getting-started"></a>
### ۵. پیش‌نیازها و راه‌اندازی

#### پیش‌نیازها:
- [.NET 10.0 SDK](https://dotnet.microsoft.com/) یا بالاتر (تنها در صورت اجرای سورس‌کد).
- دسترسی به یکی از پایگاه‌های داده تحت پشتیبانی (SQL Server ،Oracle ،PostgreSQL ،MySQL ،MariaDB ،SQLite).

#### اجرای سریع از روی سورس‌کد (Development):
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
### ۶. ساخت نسخه مستقل و پرتابل (Release & Publish)

#### ساخت نسخه پرتابل مستقل (Self-Contained Single-File برای ویندوز بدون نیاز به نصب بودن .NET روی سیستم مقصد):
```bash
dotnet publish SourceCode/BprDevKit.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o Release/BprDevKit-v1.2.0-Standalone
```
فایل خروجی `BprDevKit.exe` یک نسخه کاملاً مستقل و خودکفا است که بدون نیاز به پیش‌نیاز ران‌تایم، بر روی هر سیستم ویندوز ۶۴ بیتی با دو بار کلیک اجرا می‌شود.

---

<br><br>

---

<a name="-complete-english-documentation"></a>
# 🇬🇧 Complete English Documentation

## 📑 English Table of Contents
1. [Overview & Philosophy](#en-overview)
2. [Architecture & Tech Stack](#en-architecture)
3. [Supported Databases](#en-supported-databases)
4. [Step-by-Step Feature Walkthrough](#en-features)
   - [4.1. Database Management & Quick SQL Query Runner](#en-db-management)
   - [4.2. Data Model Generator](#en-model-generator)
   - [4.3. Advanced C# Architecture, DbContext & Fluent API](#en-csharp-advanced)
   - [4.4. Fine-Grained Annotations Management](#en-annotations)
   - [4.5. Property Transformations & Two-Way JSON Synchronization](#en-transformations)
   - [4.6. Schema Access Control](#en-schema-access)
   - [4.7. TypeScript, Python, Dart, and Java Generators](#en-other-languages)
   - [4.8. Script Generator](#en-script-generator)
   - [4.9. Configuration Backup & Restore](#en-config-backup)
   - [4.10. Application Settings & Premium Dark Mode Palette](#en-app-settings)
   - [4.11. System Logging & Audit](#en-logs)
5. [Prerequisites & Getting Started](#en-getting-started)
6. [Build & Release Guide (Standalone Packaging)](#en-release)

---

<a name="en-overview"></a>
### 1. Overview & Philosophy
**BprDevKit** is an enterprise-grade developer productivity suite designed to bridge the gap between relational databases and modern software codebases. Instead of writing boilerplate entities, DTOs, or schema scripts by hand, BprDevKit reverse-engineers your database structures into clean, highly customizable domain models across multiple programming languages.

---

<a name="en-architecture"></a>
### 2. Architecture & Tech Stack
- **Core Framework:** Microsoft .NET 10.0 (ASP.NET Core Blazor Server)
- **Supported Model Languages:** C# 13, TypeScript, Python 3, Dart, and Java 17+
- **Frontend & Design:** Bootstrap 5 with native dual-mode RTL (Right-to-Left) and LTR (Left-to-Right) layout, accompanied by an ergonomic, high-contrast Dark Mode palette.
- **Internal Security:** Local AES-256 encrypted configuration storage with no external database requirements.
- **Design Philosophy:** Domain-driven modular design, native rolling file logger, and zero heavy third-party dependencies.

---

<a name="en-supported-databases"></a>
### 3. Supported Databases
BprDevKit provides dedicated SQL generator engines, metadata extractors, and connection management for six major database platforms:

| Database Engine | Supported Versions & Features | Connection Provider |
| :--- | :--- | :--- |
| **Microsoft SQL Server** | 2016 through 2022, Azure SQL Database | `Microsoft.Data.SqlClient` |
| **Oracle Database** | 11g through 23ai with Oracle global catalog extraction | `Oracle.ManagedDataAccess.Core` |
| **PostgreSQL** | PostgreSQL 10+ with schema and custom data type support | `Npgsql` |
| **MySQL** | MySQL 5.7 and 8.0+ | `MySqlConnector` |
| **MariaDB** | MariaDB 10.3+ | `MySqlConnector` |
| **SQLite** | Embedded SQLite 3 file-based database | `Microsoft.Data.Sqlite` |

---

<a name="en-features"></a>
### 4. Step-by-Step Feature Walkthrough

<a name="en-db-management"></a>
#### 4.1. Database Management & Quick SQL Query Runner
- **Complete Object Explorer:** View schema and manage Tables, Columns, Primary Keys (PK), Foreign Keys (FK), Indexes, Check Constraints, Triggers, Views, Functions, and Stored Procedures.
- **Quick SQL Query Runner:** 
  - Integrated interactive query console tab in Database Management to execute ad-hoc SQL statements directly in your browser.
  - Dynamically renders structured data tables, execution duration timers, affected row counts, and status/error alerts with instant clear controls.
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
#### 4.2. Data Model Generator
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
  - `Original`: Exact match to SQL database column casing (default).
  - `PascalCase`: Standard for C#.
  - `camelCase`: Standard for TypeScript, Dart, and Java.
  - `snake_case`: Standard for Python.
- **Strip Common Table/Object Prefixes:** Removes prefixes like `tbl_`, `vw_`, `sp_`, `usp_` from class names (default: `false`).
- **Strip Common Column/Field Prefixes:** Strips prefixes like `fld_`, `col_` from property names (default: `false`).
- **Singularize Class Names:** Converts plural table names into singular entity names (e.g., `Users` $\rightarrow$ `User`), preserving exact casing when desired (default: `false`).
- **Generate Navigation Properties:** Automatically creates reference and collection navigation properties for foreign keys. Navigation regions (`#region References` and `#region Relations`) are always rendered with closing `#endregion` to maintain consistent architectural structure across all entities.
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
#### 4.3. Advanced C# Architecture, DbContext & Fluent API
Comprehensive options accessible via profile management:

##### 1. Structure & Regions:
- **C# Type Kind:** Choose between `class`, `record` (immutable data structures), or `struct`.
- **Property Accessor:** Choose `{ get; set; }`, `{ get; init; }`, or multi-line `backing_field` (`private _field`).
- **Base Class & Interfaces:** Specify inheritance using token variables (`{TableName}`, `{ClassName}`).
- **Partial Class:** Adds `partial` modifier to class signatures.
- **#region Blocks:** Segregates fields, properties, foreign keys, and constructors cleanly. Empty navigation regions are safely preserved.
- **Class Constants Definition:** Define compile-time constants per entity with token helpers (`{TableName}`, `{SchemaName}`, `{ClassName}`, `{Database}`).

##### 2. DbContext & Fluent API Generator:
- **Generate DbContext:** Automatically creates an EF Core `DbContext` class containing all `DbSet<T>` properties for selected tables and views.
  - **DbContext Namespace:** Customize the target context namespace (e.g. `{Database}.Context` or `MyProject.Data`).
  - **Fully Qualified Model Names:** Automatically generates `DbSet<global::Namespace.Entity>` when enabled to prevent naming collisions between identical table names across schemas.
- **Generate EntityTypeConfiguration (Fluent API):**
  - Generates standalone `{ClassName}Configuration.cs` files implementing `IEntityTypeConfiguration<T>`.
  - Configures primary keys (`HasKey`), auto-increment identity columns, column names and types (`ToTable`, `HasColumnName`), string lengths, and foreign key relationships with referential actions (`OnDelete`).

---

<a name="en-annotations"></a>
#### 4.4. Fine-Grained Annotations Management
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
#### 4.5. Property Transformations & Two-Way JSON Synchronization
Dynamic rule engine that alters how columns are represented in the generated models:
- **`[NotMapped]` Helper Properties with Two-Way Sync:**
  - Adds a strongly-typed `Dictionary<string, object?>` property backed by an underlying JSON string field (`_payloadJson`).
  - **Two-Way Synchronization:** Modifying the Dictionary automatically serializes into the database-mapped JSON string, while reading from the database automatically deserializes into the Dictionary.
- **Type Overrides:** Directly changes property types (e.g., mapping an integer status column to a domain Enum).
- **One-Click Pre-built Templates:**
  - **JSON to Dictionary:** Converts JSON columns into dictionaries with optional two-way backing field synchronization.
  - **JSON to JsonDocument:** Converts JSON columns into `JsonDocument?` with automated parsing and serialization getters/setters.
  - **XML to XElement:** Converts XML columns into `XElement?`.
  - **CSV to List<string>:** Converts comma-delimited text into `List<string>?`.
  - **Int to Enum:** Binds integer flags directly to strongly-typed C# enums.

---

<a name="en-schema-access"></a>
#### 4.6. Schema Access Control
Enforce architectural boundaries for **Modular Monoliths** or **Domain-Driven Design (DDD)**:
- Define explicit allowed dependency pathways between database schemas.
- If Schema A is forbidden from referencing Schema B, all cross-schema foreign keys and navigation properties between them are omitted during generation.
- Supports marking schemas as **Completely Isolated**.

---

<a name="en-other-languages"></a>
#### 4.7. TypeScript, Python, Dart, and Java Generators
- **TypeScript:**
  - Declaration Styles: `interface`, `type alias`, or `class`.
  - Configurable flags: `readonly` modifier for Primary Keys, Date representation as `Date` object or ISO `string`, automated `export` statements, and custom import headers.
  - Fully supports Parent and Child collection navigation arrays (`Child[]`).
- **Python:**
  - Architecture options: `Pydantic (BaseModel)`, `SQLAlchemy (ORM)`, `Dataclass`, or `TypedDict`.
  - Configurable type hints and default values.
- **Dart (Flutter):**
  - Architecture options: `Plain Dart`, `JsonSerializable`, or `Freezed`.
  - Full Null Safety support and `final` field modifiers.
- **Java:**
  - Architecture options: `Java 17+ Record`, `JPA / Hibernate Entity`, `Lombok (@Data, @Builder)`, or standard POJO with getters/setters.
  - Jackson annotations (`@JsonProperty`) and `Serializable` support.

---

<a name="en-script-generator"></a>
#### 4.8. Script Generator
- Select any combination of database objects (Tables, Views, Procedures, Functions).
- Script generation modes:
  - `CREATE`
  - `DROP AND CREATE`
  - `DATA (INSERT)` with automated `IDENTITY_INSERT` wrapping
- Foreign key dependency resolution ensures scripts execute without constraint violations.

---

<a name="en-config-backup"></a>
#### 4.9. Configuration Backup & Restore
- Export all server connection details, generator profiles, and app settings into an encrypted `.json` configuration file.
- Restore onto other developer workstations or environments with a pre-import summary verification.

---

<a name="en-app-settings"></a>
#### 4.10. Application Settings & Premium Dark Mode Palette
- **Persistent Theme Preferences:** Theme choice (Light / Dark mode) is saved directly into the encrypted `ApplicationConfiguration`, ensuring zero resets when switching tabs or pages.
- **Premium Obsidian / Slate Palette:** Bespoke dark theme styling with deep canvas tones (`#0b0f19`), elimination of jarring white active elements, vibrant cyan-blue accents (`#38bdf8`), and high-contrast typography.
- **Sidebar Theme Quick Toggle:** Frosted glassmorphism toggle button in the sidebar with instant client-server synchronization.

---

<a name="en-logs"></a>
#### 4.11. System Logging & Audit
- Built-in rolling file logging engine with zero external dependencies.
- Logs all model generations, database connection events, and backup/restore executions.
- Real-time search, severity filtering (Info, Warning, Error), and full stack trace inspection modal.

---

<a name="en-getting-started"></a>
### 5. Prerequisites & Getting Started

#### Prerequisites:
- [.NET 10.0 SDK](https://dotnet.microsoft.com/) or higher (only required when running from source code).
- Access to any supported database (SQL Server, Oracle, PostgreSQL, MySQL, MariaDB, SQLite).

#### Quick Start from Source (Development):
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
### 6. Build & Release Guide (Standalone Packaging)

#### Build Self-Contained Standalone Executable (Windows x64 - Zero .NET installation required on target machine):
```bash
dotnet publish SourceCode/BprDevKit.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o Release/BprDevKit-v1.2.0-Standalone
```
The output `BprDevKit.exe` is completely self-contained and ready to execute on any 64-bit Windows machine by double-clicking.

---

<div align="center">

Made with ❤️ for software engineers and database professionals.  
**BprDevKit © 2026**

</div>
