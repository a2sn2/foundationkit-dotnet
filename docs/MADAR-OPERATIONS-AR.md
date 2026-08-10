# تشغيل مدار وإثبات الجاهزية — v0.10

هذه الوثيقة هي مرجع **تشغيل Madar** داخل `apps/Madar`. التفاصيل الوظيفية في [`MADAR-SPECIFICATION-AR.md`](MADAR-SPECIFICATION-AR.md)، ومسار التسليم/UAT/Publish في [`MADAR-LOCAL-RUN-PUBLISH-AR.md`](MADAR-LOCAL-RUN-PUBLISH-AR.md).

## 1. حدود التشغيل الحالي

المسار الأساسي للمستخدم والمختبر على Windows:

```text
Browser
   ↓
Madar.Client (Blazor)
   ↓
Madar.Api (ASP.NET Core)
   ↓
Local SQL Server
```

العنوان القياسي:

```text
http://localhost:8100
```

Docker Compose يبقى مسارًا مستقلاً للـCI والتكامل والانحدار والحاويات. GitHub Pages ليست runtime للمنتج؛ `site/madar-demo/` معاينة ثابتة بلا API أو SQL أو حفظ دائم.

## 2. المتطلبات لمسار Native

- Windows.
- Git.
- PowerShell 5.1 أو أحدث.
- .NET 10 SDK وفق `global.json`.
- SQL Server محلي يعمل ويمكن الوصول إليه افتراضيًا عبر `Server=.`.

ابدأ دائمًا بـ:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 doctor
```

`doctor` يفحص الأدوات وSDK وخدمات SQL والمنافذ وGit وحالة التطبيقات. كما يعرض Docker و`devtunnel` و`cloudflared` كقدرات اختيارية.

## 3. تشغيل Native

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Madar -Mode Native
```

أو المشغل المتخصص:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\madar-product.ps1 start -Mode Native
```

المشغل ينشئ/يستخدم:

```text
.local/madar-product.env
.local/madar-product.mode
.local/madar-native.pid
.local/madar-native/app/
.local/madar-native/attachments/
.local/logs/madar-native.out.log
.local/logs/madar-native.err.log
```

كلها ignored by Git. ملف credentials المحلي يقيَّد بـACL على Windows.

المشغل ينشر `Madar.Api` ثم يشغّل `Madar.Api.dll` مباشرة، لذلك لا يعتمد على `launchSettings.json` ولا على منافذ Visual Studio العشوائية.

## 4. إعداد Native الافتراضي

Connection string الافتراضي:

```text
Server=.;Database=MadarDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

يمكن للمشغل المتخصص استقبال `-NativeConnectionString` عند الحاجة إلى SQL instance مختلف. هذا إعداد Development/UAT وليس اختيار Production.

## 5. Bootstrap والـcredentials

عند أول إنشاء للإعداد المحلي يولد المشغل كلمات مرور عشوائية لـ:

```text
Administrator: admin@madar.local
Operator:      operator@madar.local
```

لعرضها:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 credentials -Target Madar
```

**قاعدة مهمة:** Bootstrap idempotent. إذا كان المستخدم موجودًا مسبقًا في `MadarDb` فلن يغير startup كلمة مروره. لذلك إعادة إنشاء `.local/madar-product.env` لا تعني إعادة ضبط PasswordHash لمستخدم موجود.

## 6. الروابط المحلية

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

فتح التطبيق:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 open -Target Madar
```

## 7. Live وReady

```text
GET /health/live
GET /health/ready
```

`live` يثبت أن ASP.NET Core يعمل. `ready` يثبت جاهزية المنتج، ومنها اتصال SQL وعدم وجود migrations معلقة وفق سياسة startup.

الحالة الطبيعية:

```json
{
  "status": "ready",
  "service": "madar-api"
}
```

لا تظهر secrets أو connection string في readiness.

## 8. قاعدة البيانات والمهاجرات

Madar يملك `MadarDbContext` ومهاجراته تحت:

```text
apps/Madar/Madar.Infrastructure/Migrations
```

EF Core migrations هي مصدر حقيقة schema. Native وDocker يشغّلان نفس product migrations، لكن كل topology قد يستخدم قاعدة Development مختلفة. لا تفترض أن SQL المحلي وDocker SQL volume هما نفس مخزن البيانات.

ترقية .NET 10 تحافظ صراحة على أطوال مفاتيح ASP.NET Identity المركبة القائمة (`128`) لتجنب schema churn غير مطلوب.

## 9. SLA

الإعداد المحلي الافتراضي:

```text
MADAR_SLA_ENABLED=false
MADAR_SLA_LOW=01:00:00
MADAR_SLA_MEDIUM=01:00:00
MADAR_SLA_HIGH=01:00:00
MADAR_SLA_CRITICAL=01:00:00
```

هذه placeholders للتطوير. السياسة الحقيقية وقيمها يجب أن تأتي من صاحب العملية.

التقييم التشغيلي:

```text
POST /api/cases/sla/evaluate
```

هو command مخول ومحدود، وليس scheduler دائمًا بحد ذاته.

## 10. المرفقات

Native Development يخزن المحتوى تحت:

```text
.local/madar-native/attachments/
```

Docker Development/CI يستخدم volume خاص. Metadata في SQL Server في الحالتين، والمحتوى خارج `wwwroot`.

حدود v0.10:

```text
Maximum size: 10 MiB
Allowed: PDF / PNG / JPEG / TXT
```

فحص التوقيع الأساسي ليس malware scanner. Object storage/KMS/retention/malware-scanning قرار Production.

## 11. البحث والتقارير

```text
GET /api/cases/search
/reports/cases
```

نطاق الرؤية يطبق قبل النتائج والعدادات. البحث SQL/EF product-owned وليس external search index ولا BI platform.

## 12. الحالة والسجلات

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 status -Target Madar
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 logs -Target Madar
```

الحالات المتوقعة:

```text
STOPPED or unreachable
PROCESS RUNNING but health is unreachable
LIVE but NOT READY
READY
```

في Native، الـlogs من `.local/logs/`. في Docker، logs من Compose.

## 13. الإيقاف وحفظ البيانات

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 stop -Target Madar
```

إذا كان النمط المخزن Native، يتوقف process وتبقى `MadarDb`. إذا كان Docker، يتوقف stack وتبقى SQL volume. المدير الموحد لا يعرض reset مدمر لـMadar عمدًا.

## 14. مشاركة UAT — Microsoft

بعد أن يصبح Madar `READY` ومع تثبيت `devtunnel` وتسجيل الدخول:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 expose -Target Madar -TunnelProvider Microsoft
```

المسار يستخدم:

```text
devtunnel host -p 8100 --allow-anonymous
```

هذا exposure مؤقت ومقصود للاختبار فقط. `--allow-anonymous` يعني أن من يملك الرابط يمكنه الوصول إلى التطبيق خلال الجلسة، لذلك استخدم test data/accounts وأوقفه بـ`Ctrl+C`.

## 15. مشاركة UAT — Cloudflare

مع توفر `cloudflared`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 expose -Target Madar -TunnelProvider Cloudflare
```

المسار يستخدم Quick Tunnel إلى `http://localhost:8100`. الرابط الناتج مؤقت وليس SLA أو hosting Production.

لا تُخزَّن tunnel credentials أو public URLs في المستودع.

## 16. Docker regression topology

Docker يبقى مدعومًا صراحةً:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Madar -Mode Docker
```

ويظل مسؤولاً عن أدلة مثل:

```text
Docker build/runtime
SQL container startup
readiness
non-root/container hardening
SQL/E2E regressions
security image scanning
```

عدم اعتماده كمسار UAT يومي لا يعني إزالة هذه الأدلة.

## 17. سلوك authentication قبل تسجيل الدخول

Blazor authentication-state provider يسأل:

```text
GET /api/auth/me
```

عندما لا توجد جلسة مصادق عليها قد يرجع API `401`. العميل يعامل فشل هذا الطلب كـanonymous principal. هذا السلوك متوقع أثناء اكتشاف حالة الجلسة قبل login، ولا ينبغي تفسيره كفشل session بعد نجاح المصادقة.

## 18. Release Publish

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\madar-product.ps1 publish
```

ينتج:

```text
artifacts/madar/publish/
artifacts/madar/Madar-net10.0-Release.zip
artifacts/madar/Madar-net10.0-Release.zip.sha256
```

هذا framework-dependent Release artifact، بلا أسرار deployment خاصة. لا يختار ingress أو secret store أو SQL principal ولا يساوي Production deployment.

## 19. التحقق الآلي

بوابات المستودع تغطي، بحسب النطاق:

```text
Repository hygiene + secret scan
Release restore/build/test
Madar publish
Windows PowerShell 5.1 launcher parsing/smokes
Native UAT launcher contract checks
Docker + SQL startup/readiness
Auth + anti-CSRF + lifecycle + audit
SLA + comments + approvals + notifications
Departments + routing + claim + administration
Transfer + reassignment
Attachments
Authorized search/reporting privacy
Department-routing SQL workflow
Security Scan + container scan + CodeQL
Composer + Pages
17 nupkg + 17 snupkg invariant
```

## 20. جولة القبول اليدوية

بعد `READY` استخدم [`MADAR-ACCEPTANCE-CHECKLIST-AR.md`](MADAR-ACCEPTANCE-CHECKLIST-AR.md). لا تستبدل UAT البشري بالاختبارات الآلية؛ هما دليلان مختلفان.

## 21. حدود Production

نجاح Native أو Docker أو Tunnel أو CI أو Release Publish لا يساوي Production Approval. البيئة الحقيقية ما زالت تحتاج، حسب استخدامها:

- HTTPS/domain/ingress.
- secret store.
- least-privilege SQL identity.
- Data Protection keys دائمة.
- object storage وmalware scanning وretention للمرفقات.
- durable notification/background delivery إذا كانت مطلوبة.
- central logs/metrics/traces/alerts.
- backup/restore للبيئة المستهدفة.
- قيم SLA المعتمدة.
- privacy/legal/performance acceptance.

## 22. المراجع

- [`MADAR-SPECIFICATION-AR.md`](MADAR-SPECIFICATION-AR.md)
- [`MADAR-LOCAL-RUN-PUBLISH-AR.md`](MADAR-LOCAL-RUN-PUBLISH-AR.md)
- [`MADAR-ACCEPTANCE-CHECKLIST-AR.md`](MADAR-ACCEPTANCE-CHECKLIST-AR.md)
- [`../apps/Madar/README.md`](../apps/Madar/README.md)
- `site/madar-demo/` — Demo ثابتة بلا خادم أو SQL.
