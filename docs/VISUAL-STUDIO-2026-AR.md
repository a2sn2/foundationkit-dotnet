# تشغيل FoundationKit وWorkbench ومنصة أثَر على Visual Studio 2026

هذا الدليل مخصص لتشغيل الحل كاملًا على Windows مع SQL Server محلي من داخل Visual Studio. ابدأ أولًا من الدليل الكانوني [`LOCAL-RUN-WINDOWS-AR.md`](LOCAL-RUN-WINDOWS-AR.md) لفحص الأدوات وSQL Server والمنافذ قبل الدخول في إعدادات Visual Studio.

لا تحتاج إلى تشغيل مشروع Blazor منفصل؛ مشروع الـAPI يستضيف ملفات Blazor WebAssembly ويعمل العميل والـAPI من عنوان واحد.

> مهم: منافذ Visual Studio تختلف جزئيًا عن منافذ المدير الموحد `foundationkit.ps1`. Visual Studio يستخدم Workbench على `5057` وAthar على `5068`، بينما المدير الموحد Native يستخدم Workbench على `5057` وAthar على `8090`.

> خط التطوير الحالي هو **.NET 10 LTS / `net10.0`**. راجع [`NET10-LTS-BASELINE.md`](NET10-LTS-BASELINE.md) لقرار الترقية وحدود التوافق.

## 1. المتطلبات

ثبّت أو تأكد من وجود:

- Visual Studio 2026.
- Workload: **ASP.NET and web development**.
- .NET 10 SDK، وفق `global.json` داخل المستودع.
- SQL Server محلي مثل SQL Server 2025 أو SQL Server Express.
- SQL Server Management Studio لفحص قواعد البيانات.
- Git.

على جهاز يستخدم الـDefault Instance مثل `MSSQLSERVER` يكفي غالبًا:

```text
Server=.
```

أما SQL Express فيستخدم عادة:

```text
Server=.\SQLEXPRESS
```

## 2. تنزيل المستودع

من Terminal أو PowerShell:

```powershell
git clone https://github.com/a2sn2/foundationkit-dotnet.git
cd foundationkit-dotnet
git switch main
git pull --ff-only origin main
```

ثم افتح:

```text
FoundationKit.sln
```

انتظر حتى ينتهي Visual Studio من استعادة NuGet packages. إذا لم يبدأ تلقائيًا:

```text
Solution Explorer → Right click Solution → Restore NuGet Packages
```

## 3. تأكد من SQL Server

من `SQL Server Configuration Manager` أو `Services` تأكد أن الخدمة المناسبة تعمل:

```text
SQL Server (MSSQLSERVER)   Running
```

أو:

```text
SQL Server (SQLEXPRESS)    Running
```

اختبر الاتصال من SSMS باستخدام Windows Authentication قبل تشغيل الحل. استخدم في User Secrets نفس اسم الـServer الذي نجح في SSMS.

---

# تشغيل Workbench

Workbench هو المثال المعماري الذي يشرح مسار المستخدم ومسار الإدارة.

## 4. إعداد Connection String لـWorkbench

من Solution Explorer:

```text
samples
  → FoundationKit.Workbench.Api
  → Right click
  → Manage User Secrets
```

للـDefault Instance:

```json
{
  "ConnectionStrings": {
    "Workbench": "Server=.;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

لـSQL Express:

```json
{
  "ConnectionStrings": {
    "Workbench": "Server=.\\SQLEXPRESS;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

لا تضع كلمات مرور أو بيانات اتصال حساسة داخل `appsettings.json` في Git.

## 5. اختيار Startup Project

```text
Right click FoundationKit.Workbench.Api
→ Set as Startup Project
```

ثم:

```text
Build → Rebuild Solution
```

بعد نجاح البناء اضغط:

```text
F5
```

يفتح المتصفح على:

```text
http://localhost:5057
```

## 6. اختبار Workbench

افتح بالتسلسل:

```text
http://localhost:5057/
http://localhost:5057/user
http://localhost:5057/admin
http://localhost:5057/swagger
http://localhost:5057/api/health
```

السيناريو الكامل:

1. افتح `/user`.
2. أدخل بيانات مشروع تجريبي.
3. اختر قدرات من FoundationKit.
4. أرسل الطلب.
5. افتح `/admin`.
6. اختر الطلب.
7. اكتب اسم المراجع وملاحظاته.
8. اعتمد أو ارفض.
9. ارجع إلى `/user` واضغط تحديث الحالة.

أول تشغيل يطبق EF Core migrations تلقائيًا وينشئ:

```text
Database: FoundationKitWorkbench
Tables: BuildBriefs, AdminReviews, __EFMigrationsHistory
```

---

# تشغيل منصة أثَر

أثَر هو المنتج العربي الكامل: Identity، مستخدم، إدارة، CSRF، تدقيق، ومبادرات.

## 7. إعداد User Secrets لمنصة أثَر

من Solution Explorer:

```text
examples
  → Athar
  → Athar.Api
  → Right click
  → Manage User Secrets
```

أنشئ كلمة مرور محلية قوية وفريدة بنفسك. لا تستخدم كلمة مرور موحدة من التوثيق ولا تعيد استخدامها في حساب حقيقي.

للـDefault Instance ضع مثلًا:

```json
{
  "ConnectionStrings": {
    "Athar": "Server=.;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  },
  "AdminSeed": {
    "Enabled": true,
    "Email": "admin@athar.local",
    "DisplayName": "مسؤول منصة أثر",
    "Password": "<LOCAL-STRONG-PASSWORD>"
  }
}
```

ولـSQL Express:

```json
{
  "ConnectionStrings": {
    "Athar": "Server=.\\SQLEXPRESS;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  },
  "AdminSeed": {
    "Enabled": true,
    "Email": "admin@athar.local",
    "DisplayName": "مسؤول منصة أثر",
    "Password": "<LOCAL-STRONG-PASSWORD>"
  }
}
```

استبدل `<LOCAL-STRONG-PASSWORD>` بقيمة محلية فعلية داخل User Secrets فقط. لا تضعها في ملف tracked ولا ترسلها في issue أو screenshot أو رسالة تشخيص.

## 8. اختيار Startup Project

```text
Right click Athar.Api
→ Set as Startup Project
```

ثم:

```text
Build → Rebuild Solution
F5
```

يفتح Visual Studio على:

```text
http://localhost:5068
```

هذا صحيح لمسار Visual Studio. إذا شغلت Athar من `foundationkit.ps1 -Mode Native` فالعنوان يكون `http://localhost:8090` بدلًا منه.

مشروع `Athar.Api` يستضيف `Athar.Client` تلقائيًا؛ لا تجعل `Athar.Client` Startup Project مستقلًا عند تشغيل المسار الكامل.

## 9. اختبار منصة أثَر كمستخدم

1. افتح:

```text
http://localhost:5068/account
```

2. أنشئ مستخدمًا جديدًا ببريد مختلف عن بريد الإدارة.
3. بعد الدخول افتح:

```text
http://localhost:5068/initiatives
```

4. أنشئ مبادرة.
5. تأكد أنها ظهرت بحالة **قيد المراجعة**.

## 10. اختبار منصة أثَر كمسؤول

1. سجّل خروج المستخدم.
2. سجّل الدخول بـ:

```text
Email: admin@athar.local
Password: القيمة المحلية الموجودة في User Secrets
```

3. افتح:

```text
http://localhost:5068/admin
```

4. اختر المبادرة.
5. راجع الوصف والميزانية والمستفيدين.
6. أضف ملاحظات.
7. اختر اعتماد أو رفض.
8. سجّل الخروج.
9. ادخل بحساب المستخدم.
10. افتح `/initiatives` وتأكد من ظهور القرار وملاحظات الإدارة.

## 11. المسارات المهمة في أثَر

```text
/                     الصفحة الرئيسية
/account              التسجيل والدخول
/initiatives          مساحة المستخدم ومبادراته
/admin                مركز قرار الإدارة
/swagger              توثيق API
/health/live           هل عملية التطبيق حية؟
/health/ready          هل التطبيق وقاعدة البيانات جاهزان؟
```

أول تشغيل ينشئ قاعدة:

```text
Database: Athar
```

وتشمل جداول Identity وجداول المنتج مثل المبادرات والمراجعات وسجل التدقيق.

---

# تشغيل المشروعين معًا من Visual Studio

يمكن تشغيل Workbench وأثَر في وقت واحد لأن المنافذ مختلفة:

```text
Workbench: http://localhost:5057
Athar:     http://localhost:5068
```

من Visual Studio:

```text
Right click Solution
→ Configure Startup Projects
→ Multiple startup projects
```

اضبط:

```text
FoundationKit.Workbench.Api   Start
Athar.Api                     Start
```

ولا تضبط مشاريع Client على Start؛ كل API يستضيف عميله.

---

# فحص قاعدة البيانات في SSMS

بعد التشغيل:

1. افتح SSMS.
2. اتصل بنفس Server المستخدم في User Secrets.
3. اضغط Refresh على Databases.
4. ستجد:

```text
FoundationKitWorkbench
Athar
```

5. افحص:

```text
Databases
→ <Database>
→ Tables
```

لا تنشئ الجداول يدويًا؛ **EF migrations هي المصدر الرسمي لبنية قاعدة البيانات**.

---

# أوامر التحقق من Terminal

من جذر المستودع:

```powershell
.\foundationkit.ps1 doctor
.\foundationkit.ps1 restore
.\foundationkit.ps1 build
.\foundationkit.ps1 test
.\foundationkit.ps1 verify
```

أو مباشرة:

```powershell
dotnet restore FoundationKit.sln
dotnet build FoundationKit.sln --configuration Release --no-restore
dotnet test FoundationKit.sln --configuration Release --no-build
```

تشغيل Workbench مباشرة يستخدم launch profile على `5057`:

```powershell
dotnet run --project .\samples\FoundationKit.Workbench\FoundationKit.Workbench.Api.csproj
```

تشغيل أثَر مباشرة يستخدم launch profile على `5068`:

```powershell
dotnet run --project .\examples\Athar\Athar.Api\Athar.Api.csproj
```

## مشاكل شائعة

### Login failed أو Server not found

- تأكد أن SQL Server service تعمل.
- جرّب `Server=.` للـDefault Instance.
- جرّب `Server=.\SQLEXPRESS` للـExpress.
- تأكد أن اسم السيرفر نفسه يعمل في SSMS.

### قاعدة البيانات لم تظهر

- راجع Output في Visual Studio.
- تأكد أن Connection String ليست فارغة.
- تأكد أن حساب Windows لديه صلاحية إنشاء قاعدة محلية.
- أوقف التطبيق، صحح User Secrets، ثم شغله مرة أخرى.

### Port already in use / Address already in use

شغّل:

```powershell
.\foundationkit.ps1 doctor
```

سيعرض المنافذ الأساسية التي عليها listener. أوقف العملية القديمة أو اختر مسار تشغيل واحدًا فقط بدل تشغيل Visual Studio والمدير الموحد لنفس التطبيق في الوقت نفسه.

### المتصفح يعرض 401 أو 403

- `401`: المستخدم غير مسجل الدخول.
- `403`: المستخدم دخل لكنه لا يملك دور Administrator أو السياسة المطلوبة.
- تأكد أن AdminSeed مفعّل محليًا في أول تشغيل وأن بريد المسؤول صحيح.

### صفحة Blazor لا تحمل

- اجعل مشروع الـAPI هو Startup Project.
- نفذ Rebuild Solution.
- امسح `bin` و`obj` فقط عند الحاجة ثم Restore وRebuild.
- لا تشغّل Client وحده عند اختبار API وSQL Server.

### تغيير User Secrets لم ينعكس

- أوقف التطبيق بالكامل.
- تأكد أنك فتحت Manage User Secrets للمشروع الصحيح.
- شغّل من جديد؛ User Secrets تُقرأ في Development فقط.

عند طلب المساعدة، أرسل الخطأ وسجل التشغيل بدون User Secrets أو محتوى `.local/*.env` أو كلمات المرور.
