# تشغيل FoundationKit محليًا على Windows

هذا هو الدليل الكانوني لتشغيل المستودع على Windows. الخط الحالي هو **.NET 10 LTS / `net10.0`**.

> استخدم بيانات تجريبية فقط. الملفات تحت `.local/` محلية ومهملة من Git، ولا تمثل Secret Management لبيئة Production.

## 1. المتطلبات

الحد الأدنى:

- Git.
- PowerShell 5.1 أو أحدث.
- .NET 10 SDK وفق `global.json`.

لـWorkbench/Athar Native:

- SQL Server محلي يعمل، مثل `MSSQLSERVER` أو `SQLEXPRESS`.

لـMadar:

- Docker Desktop/Engine مع Docker Compose في حالة Ready.

اختياري:

- Visual Studio 2026 مع ASP.NET and web development.
- SSMS.
- Python/Node.js لفحوصات المستودع الإضافية.
- `sqlcmd` لبعض عمليات Athar Native.

## 2. نسخة نظيفة

```powershell
git clone https://github.com/a2sn2/foundationkit-dotnet.git
cd foundationkit-dotnet
git switch main
git pull --ff-only origin main
git status --short
```

في نسخة نظيفة يجب ألا يعرض `git status --short` ملفات متغيرة.

## 3. Preflight

من جذر المستودع:

```powershell
.\foundationkit.ps1 doctor
```

ثم عند الحاجة:

```powershell
dotnet --info
docker info
docker compose version
```

`doctor` يتحقق من الأدوات، .NET 10، Docker عند توفره، الخدمات/المنافذ المحلية وحالة التطبيقات المعروفة.

إذا ظهر FAIL لمتطلب تحتاجه في المسار الذي ستشغله، أصلحه قبل بدء المنتج.

## 4. Workbench — Native

```powershell
.\foundationkit.ps1 start -Target Workbench -Mode Native
```

الرابط:

```text
http://localhost:5057
```

الحالة:

```powershell
.\foundationkit.ps1 status -Target Workbench -Mode Native
```

المسارات الأساسية:

```text
http://localhost:5057/
http://localhost:5057/user
http://localhost:5057/admin
http://localhost:5057/swagger
http://localhost:5057/api/health
```

Default SQL connection:

```text
Server=.;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

إذا كنت تستخدم SQL Express، عدّل فقط القيمة المحلية داخل:

```text
.local/workbench-product.env
```

مثال:

```text
WORKBENCH_NATIVE_CONNECTION_STRING=Server=.\SQLEXPRESS;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

ثم أوقف وأعد التشغيل.

## 5. Athar — Native

```powershell
.\foundationkit.ps1 start -Target Athar -Mode Native
```

الرابط:

```text
http://localhost:8090
```

بيانات الإدارة المحلية عند الحاجة:

```powershell
.\foundationkit.ps1 credentials -Target Athar
```

الحالة:

```powershell
.\foundationkit.ps1 status -Target Athar -Mode Native
```

المسارات الأساسية:

```text
http://localhost:8090/
http://localhost:8090/account
http://localhost:8090/initiatives
http://localhost:8090/admin
http://localhost:8090/swagger
http://localhost:8090/health/live
http://localhost:8090/health/ready
```

Default SQL connection:

```text
Server=.;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

لـSQL Express عدّل:

```text
.local/athar-product.env
```

مثال:

```text
ATHAR_NATIVE_CONNECTION_STRING=Server=.\SQLEXPRESS;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

ثم أوقف وأعد التشغيل.

## 6. Madar — المسار الحقيقي للتجربة المحلية

Madar هو المنتج التشغيلي تحت `apps/Madar`، والمسار المعتمد حاليًا لتجربته كاملة هو Docker.

ابدأ:

```powershell
.\foundationkit.ps1 start -Target Madar -Mode Docker
```

عند أول تشغيل ينشئ المشغل:

```text
.local/madar-product.env
```

ويولد أسرار تطوير عشوائية لـSQL Server وحساب Administrator وحساب Operator. على Windows يتم تقييد ACL للملف على حساب Windows الحالي.

اعرض بيانات الدخول بعد أول تشغيل:

```powershell
.\scripts\madar-product.ps1 credentials
```

الرابط:

```text
http://localhost:8100
```

افتحه:

```powershell
.\foundationkit.ps1 open -Target Madar
```

الحالة والسجلات:

```powershell
.\foundationkit.ps1 status -Target Madar
.\foundationkit.ps1 logs -Target Madar
```

المسارات الأساسية:

```text
http://localhost:8100/
http://localhost:8100/login
http://localhost:8100/cases
http://localhost:8100/reports/cases
http://localhost:8100/admin/departments
http://localhost:8100/swagger
http://localhost:8100/health/live
http://localhost:8100/health/ready
```

`/health/live` يثبت أن العملية حية. `/health/ready` يتحقق من جاهزية قاعدة البيانات وعدم وجود EF migrations معلقة.

الإيقاف بدون حذف البيانات:

```powershell
.\foundationkit.ps1 stop -Target Madar
```

SQL volume يبقى محفوظًا.

### جولة قبول Madar المقترحة

1. ادخل كـAdministrator.
2. راجع الأقسام وعضوية المشغلين.
3. أنشئ/افتح حالة.
4. جرّب route/assign/reassign/transfer ضمن الصلاحيات.
5. راجع التعليقات والموافقات والمرفقات والتدقيق.
6. افتح البحث/التقارير.
7. ادخل كـOperator وقارن نطاق الرؤية والإجراءات.

للتفاصيل الوظيفية:

- [`MADAR-SPECIFICATION-AR.md`](MADAR-SPECIFICATION-AR.md)
- [`MADAR-OPERATIONS-AR.md`](MADAR-OPERATIONS-AR.md)
- [`MADAR-LOCAL-RUN-PUBLISH-AR.md`](MADAR-LOCAL-RUN-PUBLISH-AR.md)

## 7. Madar Release Publish

لا يحتاج أمر الـpublish إلى Docker، لكنه يحتاج .NET 10 SDK:

```powershell
.\scripts\madar-product.ps1 publish
```

الناتج:

```text
artifacts/madar/publish/
artifacts/madar/Madar-net10.0-Release.zip
artifacts/madar/Madar-net10.0-Release.zip.sha256
```

تحقق من البصمة:

```powershell
Get-FileHash .\artifacts\madar\Madar-net10.0-Release.zip -Algorithm SHA256
Get-Content .\artifacts\madar\Madar-net10.0-Release.zip.sha256
```

هذا Release artifact تقني، وليس Production deployment تلقائيًا.

## 8. تشغيل Docker للمنتجات

يمكن للمدير استخدام Docker في المسارات المدعومة:

```powershell
.\foundationkit.ps1 start -Target Workbench -Mode Docker
.\foundationkit.ps1 start -Target Athar -Mode Docker
.\foundationkit.ps1 start -Target Madar -Mode Docker
```

`-Target All -Mode Auto` يستخدم Docker عندما يكون جاهزًا ويطبق المسارات المدعومة لكل منتج.

`-Target All -Mode Native` يحافظ على Workbench/Athar Native ويخطر بأن Madar لم يتم تشغيله لأن Madar لا يملك Native path موحدًا حاليًا.

## 9. منافذ التطوير المعروفة

```text
Workbench Native  5057
Workbench Docker  8080
Athar              8090
Madar              8100
Workbench SQL      14333
Athar SQL          14334
Madar SQL          14335
```

إذا كان منفذ مستخدمًا، `doctor` يساعد على إظهار listener/PID بدل الادعاء أن التطبيق متوقف.

## 10. Build/Test/Verify

من جذر المستودع:

```powershell
.\foundationkit.ps1 restore
.\foundationkit.ps1 build
.\foundationkit.ps1 test
.\foundationkit.ps1 verify
```

ولإنشاء حزم FoundationKit القابلة لإعادة الاستخدام:

```powershell
.\foundationkit.ps1 pack
```

الحزمة المتوقعة للمستودع هي:

```text
17 .nupkg
17 .snupkg
```

## 11. GitHub Pages

FoundationKit Atlas تحت `site/` هو توثيق ثابت ويحتوي معاينات، منها:

```text
site/athar-demo/
site/madar-demo/
```

Madar demo ثابتة بلا خادم أو SQL أو حفظ بيانات. لا تستخدمها كبديل عن التشغيل المحلي الحقيقي.

## 12. إذا فشل Madar

نفذ:

```powershell
.\foundationkit.ps1 status -Target Madar
.\foundationkit.ps1 logs -Target Madar
```

ثم افحص:

```powershell
docker compose --project-name madar-product -f deploy/madar-compose.yml ps
```

لا تبدأ بحذف volume. حافظ على البيانات حتى تعرف السبب.

إذا كانت بيئة تطوير قابلة للحذف بالكامل، راجع `MADAR-OPERATIONS-AR.md` قبل أي `down --volumes` متعمد.

## 13. حدود Production

نجاح local run أو `dotnet publish` لا يثبت أن بيئة Production جاهزة. الاستضافة الحقيقية تحتاج قرارات بيئية مستقلة مثل:

- domain/HTTPS/ingress.
- secret store.
- least-privilege database account.
- Data Protection keys دائمة.
- backup/restore.
- central logs/metrics/traces/alerts.
- object storage/malware scanning/retention عندما تستخدم المرفقات.
- SLA values المعتمدة.
- privacy/legal/performance acceptance.

راجع [`PRODUCTION-READINESS-AR.md`](PRODUCTION-READINESS-AR.md).

## 14. أقصر مسار لتجربة Madar

```powershell
git pull --ff-only origin main
.\foundationkit.ps1 doctor
.\foundationkit.ps1 start -Target Madar -Mode Docker
.\scripts\madar-product.ps1 credentials
.\foundationkit.ps1 open -Target Madar
```

بعد التجربة:

```powershell
.\foundationkit.ps1 stop -Target Madar
```

ولإنشاء Release:

```powershell
.\scripts\madar-product.ps1 publish
```
