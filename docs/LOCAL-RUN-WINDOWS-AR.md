# تشغيل FoundationKit محليًا على Windows

هذا هو الدليل الكانوني لأول تشغيل محلي على Windows. الهدف أن نختبر المستودع تدريجيًا ونفصل مشاكل الأدوات أو SQL Server أو المنافذ عن مشاكل التطبيق نفسه.

> لا تستخدم بيانات حقيقية أو حساسة أثناء الاختبار المحلي. إعدادات `.local/` وUser Secrets محلية فقط ولا تُرفع إلى Git.

> خط التشغيل الحالي هو **.NET 10 LTS / `net10.0`**. راجع `docs/NET10-LTS-BASELINE.md` لقرار الترقية وحدود التوافق.

## 1. ما الذي تحتاجه

الحد الأدنى لمسار Native الخاص بـWorkbench وAthar:

- Git.
- PowerShell 5.1 أو أحدث.
- .NET 10 SDK؛ `global.json` يطلب خط .NET 10 ويقبل أحدث feature band متوافق.
- SQL Server محلي يعمل، مثل Default Instance (`MSSQLSERVER`) أو SQL Express.

Madar يستخدم في المسار التشغيلي الحالي Docker Compose، لذلك تحتاج Docker Desktop/Engine جاهزًا إذا أردت تشغيل Madar محليًا من المدير الموحد.

اختياري:

- Visual Studio 2026 مع workload **ASP.NET and web development**.
- SSMS لفحص قاعدة البيانات.
- Docker Desktop لتشغيل Workbench/Athar عبر Docker، وهو مطلوب حاليًا لمسار Madar التشغيلي.
- Python وNode.js لتشغيل كل فحوصات التحقق المحلية الإضافية.
- `sqlcmd` لعمليات النسخ الاحتياطي Native الخاصة بـAthar.

## 2. تنزيل نسخة نظيفة

```powershell
git clone https://github.com/a2sn2/foundationkit-dotnet.git
cd foundationkit-dotnet
git switch main
git pull --ff-only origin main
```

تأكد أن نسخة العمل نظيفة:

```powershell
git status --short
```

المخرجات الطبيعية لنسخة جديدة: لا شيء.

## 3. الفحص الأول قبل التشغيل

من جذر المستودع:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 doctor
```

ثم:

```powershell
dotnet --info
```

المهم في `doctor`:

- `git` = PASS.
- `dotnet` = PASS.
- `powershell` = PASS.
- وجود .NET 10 SDK.
- Git working tree = clean.
- حالة Docker موضحة بوضوح.
- فحص التطبيقات والمنافذ يشمل Workbench وAthar وMadar.

إذا فشل `doctor`، أصلح أول FAIL قبل تشغيل التطبيقات.

## 4. تحقق من SQL Server قبل التطبيق

اختبر نفس الـinstance من SSMS باستخدام Windows Authentication عند تشغيل Workbench أو Athar Native.

Default Instance:

```text
Server=.
```

SQL Express:

```text
Server=.\SQLEXPRESS
```

لا تنشئ الجداول يدويًا. Workbench وAthar وMadar يملكون EF Core migrations خاصة بكل مستهلك، وهي مصدر الحقيقة لبنية قواعد البيانات.

Madar عبر Docker لا يستخدم Windows Authentication للـinstance المحلي؛ Compose يشغّل SQL Server خاصًا بالتطوير.

## 5. أول اختبار: Workbench فقط — Native

ابدأ بـWorkbench لأنه أبسط من Athar ويختبر .NET + SQL Server + migrations + API + Blazor في مسار واحد.

```powershell
.\foundationkit.ps1 start -Target Workbench -Mode Native
```

المسار Native للمدير الموحد يستخدم افتراضيًا:

```text
Server=.;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

العنوان:

```text
http://localhost:5057
```

بعد التشغيل:

```powershell
.\foundationkit.ps1 status -Target Workbench -Mode Native
```

ثم جرّب:

```text
http://localhost:5057/
http://localhost:5057/user
http://localhost:5057/admin
http://localhost:5057/swagger
http://localhost:5057/api/health
```

إذا كنت تستخدم SQL Express أو instance مختلفًا، عدّل الملف المحلي الذي ينشئه المدير:

```text
.local/workbench-product.env
```

وغيّر فقط:

```text
WORKBENCH_NATIVE_CONNECTION_STRING=Server=.\SQLEXPRESS;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

الملف `.local/workbench-product.env` محمي محليًا ومهمل من Git. لا ترفعه للمستودع.

بعد التعديل:

```powershell
.\foundationkit.ps1 stop -Target Workbench -Mode Native
.\foundationkit.ps1 start -Target Workbench -Mode Native
```

## 6. ثاني اختبار: Athar فقط — Native

بعد نجاح Workbench:

```powershell
.\foundationkit.ps1 start -Target Athar -Mode Native
```

المدير الموحد يستخدم افتراضيًا:

```text
Server=.;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

العنوان في مسار المدير الموحد:

```text
http://localhost:8090
```

احصل على حساب الإدارة المحلي عند الحاجة فقط:

```powershell
.\foundationkit.ps1 credentials -Target Athar
```

ثم:

```powershell
.\foundationkit.ps1 status -Target Athar -Mode Native
```

المسارات المهمة:

```text
http://localhost:8090/
http://localhost:8090/account
http://localhost:8090/initiatives
http://localhost:8090/admin
http://localhost:8090/swagger
http://localhost:8090/health/live
http://localhost:8090/health/ready
```

إذا كان SQL Server على SQL Express، عدّل:

```text
.local/athar-product.env
```

إلى:

```text
ATHAR_NATIVE_CONNECTION_STRING=Server=.\SQLEXPRESS;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

ثم أوقف وأعد التشغيل.

## 7. ثالث اختبار: Madar — Docker

Madar هو أول منتج فعلي تحت `apps/`، ومساره التشغيلي المحلي المعتمد حاليًا Docker-only.

تأكد أولًا أن Docker جاهز:

```powershell
docker info
docker compose version
```

ثم:

```powershell
.\foundationkit.ps1 start -Target Madar -Mode Docker
```

المشغّل ينشئ تلقائيًا ملف تطوير محليًا:

```text
.local/madar-product.env
```

ويولّد كلمات مرور عشوائية لـSQL Server وحسابي Administrator وOperator. على Windows يقيّد الملف لحساب Windows الحالي عبر ACL.

العنوان:

```text
http://localhost:8100
```

تحقق من الحالة:

```powershell
.\foundationkit.ps1 status -Target Madar
```

المسارات المهمة:

```text
http://localhost:8100/
http://localhost:8100/login
http://localhost:8100/cases
http://localhost:8100/swagger
http://localhost:8100/health/live
http://localhost:8100/health/ready
```

`/health/live` يثبت أن العملية حية فقط، بينما `/health/ready` يتحقق من SQL Server وعدم وجود migrations معلقة.

للسجلات والإيقاف:

```powershell
.\foundationkit.ps1 logs -Target Madar
.\foundationkit.ps1 stop -Target Madar
```

الإيقاف يحافظ على SQL volume. `reset -Target Madar` غير معروض عمدًا في المدير الموحد حاليًا لمنع حذف البيانات عرضًا.

التفاصيل الكاملة موجودة في:

```text
docs/MADAR-OPERATIONS-AR.md
```

## 8. فرق المنافذ بين المدير الموحد وVisual Studio وDocker

هناك مسارات صحيحة مختلفة، لكن لا تخلط بين منافذها:

| المسار | Workbench | Athar | Madar |
|---|---:|---:|---:|
| `foundationkit.ps1` Native | `5057` | `8090` | غير مدعوم حاليًا |
| Visual Studio / `dotnet run` launch profile | `5057` | `5068` | راجع launch profile الخاص بمدار عند التطوير المباشر |
| Docker / unified operational path | `8080` | `8090` | `8100` |

SQL Server Docker host ports الحالية تشمل:

```text
Workbench: 14333
Athar:     14334
Madar:     14335
```

لذلك ظهور Athar على `8090` عند استخدام المدير الموحد ليس خطأ، وظهوره على `5068` من Visual Studio ليس خطأ أيضًا. Madar في المسار التشغيلي الموحد الحالي يظهر على `8100`.

## 9. تشغيل التطبيقات معًا

### All — Native

```powershell
.\foundationkit.ps1 start -Target All -Mode Native
```

هذا يحافظ على السلوك التاريخي لـWorkbench + Athar Native. Madar يُتجاوز برسالة واضحة لأنه لا يملك مسار Native موحدًا بعد.

ثم:

```powershell
.\foundationkit.ps1 status -Target All -Mode Native
```

الإيقاف:

```powershell
.\foundationkit.ps1 stop -Target All -Mode Native
```

### All — Auto

```powershell
.\foundationkit.ps1 start -Target All -Mode Auto
```

إذا كان Docker جاهزًا، يستطيع المدير تشغيل Madar عبر مساره Docker إضافة إلى المستهلكين الآخرين حسب آلية Auto الحالية. إذا لم يكن Docker جاهزًا، يوضّح المدير أن Madar لم يبدأ بدل الادعاء بنجاحه.

### All — Docker

```powershell
.\foundationkit.ps1 start -Target All -Mode Docker
```

المنافذ الأساسية:

```text
Workbench: http://localhost:8080
Athar:     http://localhost:8090
Madar:     http://localhost:8100
```

## 10. تشغيل Docker بشكل منفرد

إذا كان Docker Desktop جاهزًا:

```powershell
.\foundationkit.ps1 start -Target Workbench -Mode Docker
.\foundationkit.ps1 start -Target Athar -Mode Docker
.\foundationkit.ps1 start -Target Madar -Mode Docker
```

Docker ينشئ SQL Server containers خاصة بالتطوير ولا يستخدم Windows Authentication الخاص بالـinstance المحلي.

## 11. تشغيل Visual Studio 2026

افتح:

```text
FoundationKit.sln
```

لـWorkbench اجعل:

```text
FoundationKit.Workbench.Api
```

هو Startup Project.

لـAthar اجعل:

```text
Athar.Api
```

هو Startup Project.

لـMadar عند التطوير المباشر اجعل:

```text
Madar.Api
```

هو Startup Project، ثم استخدم إعدادات تطوير آمنة خاصة بك. المسار التشغيلي الموثق للمستخدم المحلي يظل Docker عبر `foundationkit.ps1`/`madar-product.ps1`.

لا تشغّل مشاريع Blazor Client وحدها عند اختبار المسار الكامل؛ الـAPI host يقدم ملفات العميل.

الدليل التفصيلي لـVisual Studio موجود في:

```text
docs/VISUAL-STUDIO-2026-AR.md
```

## 12. فحص المستودع قبل تشخيص Runtime

قبل أن تعتبر المشكلة من التطبيق، شغّل:

```powershell
.\foundationkit.ps1 restore
.\foundationkit.ps1 build
.\foundationkit.ps1 test
.\foundationkit.ps1 verify
```

`verify` يشغل البناء والاختبارات وفحوصات الكتالوج والـPages المتاحة محليًا، بما في ذلك مطابقة مسارات Razor الخاصة بـMadar مع Atlas. CI على GitHub يبقى أوسع لأنه يشغل أيضًا Linux containers وSQL integration وSecurity Scan وCodeQL.

## 13. أوامر التشخيص عند الفشل

Workbench:

```powershell
.\foundationkit.ps1 logs -Target Workbench -Mode Native
```

Athar:

```powershell
.\foundationkit.ps1 logs -Target Athar -Mode Native
```

Madar:

```powershell
.\foundationkit.ps1 status -Target Madar
.\foundationkit.ps1 logs -Target Madar
```

حالة Git:

```powershell
git status --short
git rev-parse HEAD
```

حالة .NET:

```powershell
dotnet --info
dotnet --list-sdks
```

حالة Docker عند مشكلة Madar أو Docker mode:

```powershell
docker info
docker compose version
```

اختبر SQL Server من SSMS بنفس اسم السيرفر الموجود في connection string عند تشخيص Native. بالنسبة إلى Madar Docker ابدأ من `/health/ready` ثم سجلات Compose بدل محاولة استخدام Windows Authentication على الـinstance المحلي.

## 14. ماذا ترسل عند ظهور مشكلة

أرسل المعلومات التالية بدون كلمات مرور أو أسرار:

1. الأمر الذي شغلته حرفيًا.
2. أول رسالة خطأ كاملة، وليس آخر سطر فقط.
3. ناتج `foundationkit.ps1 doctor`.
4. ناتج `dotnet --info` عند وجود مشكلة SDK/build.
5. اسم SQL Server المستخدم فقط عند Native، مثل `.` أو `.\SQLEXPRESS`.
6. هل الاتصال بنفس الاسم ينجح من SSMS أم لا عند Native.
7. ناتج `foundationkit.ps1 logs -Target <Athar|Workbench|Madar>` عند مشكلة Runtime.
8. ناتج `git rev-parse HEAD` حتى نتأكد أننا نشخّص نفس النسخة.
9. حالة `/health/live` و`/health/ready` عند مشكلة Madar أو Athar إن كانت متاحة.

لا ترسل محتوى `.local/*.env` ولا User Secrets ولا كلمات المرور.

## 15. تنظيف Runtime المحلي عند الحاجة

الإيقاف العادي يحافظ على البيانات:

```powershell
.\foundationkit.ps1 stop -Target All -Mode Native
```

`reset -Force` مخصص للمسارات التي تعرضه صراحةً. في Native لا يحذف قاعدة SQL Server تلقائيًا؛ هذا متعمد لحماية البيانات المحلية.

```powershell
.\foundationkit.ps1 reset -Target Workbench -Mode Native -Force
.\foundationkit.ps1 reset -Target Athar -Mode Native -Force
```

Madar لا يعرض reset موحدًا في v0.1.1. إذا احتجت حذف بيانات Madar Docker، افعل ذلك يدويًا وبوعي بعد التأكد أنها بيانات تطوير فقط، واتبع `docs/MADAR-OPERATIONS-AR.md`.

إذا احتجت حذف قواعد Native نفسها، افعل ذلك يدويًا وبوعي من SSMS بعد التأكد أنها قواعد تطوير محلية فقط.
