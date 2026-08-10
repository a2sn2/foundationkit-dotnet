# تشغيل مدار محليًا وإثبات الجاهزية — v0.10

هذه الوثيقة هي مرجع **تشغيل Madar** داخل `apps/Madar`. التفاصيل الوظيفية الكاملة موجودة في [`MADAR-SPECIFICATION-AR.md`](MADAR-SPECIFICATION-AR.md)، وخطوات التسليم للمستخدم وRelease Publish موجودة في [`MADAR-LOCAL-RUN-PUBLISH-AR.md`](MADAR-LOCAL-RUN-PUBLISH-AR.md).

## 1. حدود التشغيل الحالي

المسار المعتمد لتجربة Madar كاملة محليًا هو Docker Compose:

```text
Browser
   ↓
Madar.Client (Blazor)
   ↓
Madar.Api (ASP.NET Core)
   ↓
SQL Server
```

GitHub Pages ليست runtime للمنتج؛ `site/madar-demo/` معاينة ثابتة بلا API أو SQL أو حفظ دائم.

## 2. المتطلبات

على Windows:

- Git.
- PowerShell 5.1 أو أحدث.
- .NET 10 SDK وفق `global.json`.
- Docker Desktop/Engine مع Docker Compose في حالة Ready.

ابدأ دائمًا بـ:

```powershell
.\foundationkit.ps1 doctor
```

`doctor` يفحص الأدوات، SDK، Docker، المنافذ والحالة المحلية المعروفة. Madar يستخدم المنفذ `8100` للواجهة/API و`14335` لـSQL Server الخاص بـCompose.

## 3. تشغيل المنتج

```powershell
.\foundationkit.ps1 start -Target Madar -Mode Docker
```

أو المشغل المتخصص:

```powershell
.\scripts\madar-product.ps1 start
```

عند أول تشغيل ينشأ ملف محلي:

```text
.local/madar-product.env
```

ويحتوي secrets تطوير عشوائية لـSQL Server وحساب Administrator وحساب Operator. على Windows يتم تقييد ACL للملف على حساب المستخدم الحالي.

لا ترفع `.local/` إلى Git ولا تعيد استخدام هذه القيم كأسرار Production.

## 4. بيانات الدخول المحلية

بعد أول تشغيل:

```powershell
.\scripts\madar-product.ps1 credentials
```

الحسابان الافتراضيان من حيث البريد:

```text
Administrator: admin@madar.local
Operator:      operator@madar.local
```

كلمات المرور عشوائية وتظهر فقط من الإعداد المحلي الذي تم إنشاؤه على جهازك.

## 5. الروابط المحلية

بعد الجاهزية:

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

يمكن فتح التطبيق مباشرة:

```powershell
.\foundationkit.ps1 open -Target Madar
```

## 6. Live وReady

### Live

```text
GET /health/live
```

يثبت أن تطبيق ASP.NET Core يعمل.

### Ready

```text
GET /health/ready
```

يثبت أن المنتج جاهز لخدمة الطلبات، بما في ذلك الاتصال بقاعدة البيانات وعدم وجود EF Core migrations معلقة.

الحالة الطبيعية:

```json
{
  "status": "ready",
  "service": "madar-api"
}
```

تفاصيل الاتصال وأسرار SQL لا تظهر في استجابة readiness.

## 7. قاعدة البيانات والمهاجرات

Madar يملك `MadarDbContext` ومهاجراته تحت:

```text
apps/Madar/Madar.Infrastructure/Migrations
```

هذه المهاجرات هي مصدر حقيقة schema المنتج.

بيئة Development/CI تطبق المهاجرات بطريقة مضبوطة عند startup. عندما يعطل التطبيق الترحيل التلقائي، يجب أن يرفض readiness إذا كان schema غير محدث بدل العمل فوق قاعدة غير متوافقة.

ترقية .NET 10 تحافظ صراحة على أطوال مفاتيح ASP.NET Identity المركبة القائمة (`128`) حتى لا تتحول ترقية framework وحدها إلى تغيير schema غير مطلوب.

## 8. SLA في التشغيل المحلي

الملف المحلي ينشئ افتراضيًا:

```text
MADAR_SLA_ENABLED=false
MADAR_SLA_LOW=01:00:00
MADAR_SLA_MEDIUM=01:00:00
MADAR_SLA_HIGH=01:00:00
MADAR_SLA_CRITICAL=01:00:00
```

قيم الساعة الواحدة placeholders للتطوير فقط. عندما يكون SLA معطلًا لا تستخدم كسياسة تشغيلية.

للاختبار اليدوي فقط يمكن تعديل الملف ثم إعادة تشغيل Madar، مثال:

```text
MADAR_SLA_ENABLED=true
MADAR_SLA_LOW=04:00:00
MADAR_SLA_MEDIUM=02:00:00
MADAR_SLA_HIGH=01:00:00
MADAR_SLA_CRITICAL=00:30:00
```

السياسة الحقيقية وقيمها يجب أن تأتي من صاحب العملية في بيئة الاستخدام الفعلية.

المسار التشغيلي للتقييم:

```text
POST /api/cases/sla/evaluate
```

وهو command مخول ومحدود، وليس scheduler دائمًا بحد ذاته.

## 9. المرفقات

بيئة Development/CI الحالية تخزن محتوى المرفقات في filesystem خاص خارج `wwwroot`، بينما metadata في SQL Server.

حدود v0.10:

```text
Maximum size: 10 MiB
Allowed: PDF / PNG / JPEG / TXT
```

التوقيع الأساسي يقلل type-confusion لكنه ليس malware scanner. Object storage/KMS/retention/malware-scanning الحقيقي قرار بيئة Production وليس جزءًا من Docker المحلي.

## 10. البحث والتقارير

المسار:

```text
GET /api/cases/search
```

والواجهة:

```text
/reports/cases
```

يطبقان نطاق الرؤية قبل النتائج والعدادات. البحث الحالي SQL/EF product-owned وليس external search index ولا BI platform.

## 11. الحالة والسجلات

```powershell
.\foundationkit.ps1 status -Target Madar
.\foundationkit.ps1 logs -Target Madar
```

الحالات المتوقعة:

```text
STOPPED or unreachable
LIVE but NOT READY
READY
```

إذا فشل startup، المشغل يطبع آخر logs من Compose قبل إنهاء الأمر بخطأ.

## 12. الإيقاف وحفظ البيانات

```powershell
.\foundationkit.ps1 stop -Target Madar
```

الإيقاف يحافظ على SQL volume.

الحذف المتعمد للبيانات التجريبية عملية منفصلة ومدمرة، ولا يعرض المدير الموحد reset لـMadar عمدًا. لا تستخدم `down --volumes` إلا عندما تكون متأكدًا أن البيانات قابلة للحذف.

## 13. إعادة إنشاء secrets المحلية

لإعادة ملف الإعداد المحلي:

```powershell
.\scripts\madar-product.ps1 start -Reset
```

مهم: تغيير كلمة المرور في ملف البيئة لا يعيد تلقائيًا PasswordHash لمستخدم موجود بالفعل داخل SQL volume. إذا أردت بيئة تطوير جديدة بالكامل، تعامل مع volume القديم عمدًا وبشكل منفصل.

## 14. Release Publish

```powershell
.\scripts\madar-product.ps1 publish
```

ينتج:

```text
artifacts/madar/publish/
artifacts/madar/Madar-net10.0-Release.zip
artifacts/madar/Madar-net10.0-Release.zip.sha256
```

هذا framework-dependent Release artifact يحتوي Madar.Api والـBlazor assets المستضافة معه، لكنه لا يحتوي أسرار قاعدة بيانات Production ولا يختار لك ingress أو secret store أو SQL principal.

## 15. التحقق الآلي الحالي

بوابات المستودع تثبت على نفس التغييرات:

```text
Repository verification
        ↓
Release restore/build/test
        ↓
Madar publish
        ↓
Docker + SQL startup/readiness
        ↓
UI assets + Swagger/API surface
        ↓
Auth + anti-CSRF + lifecycle + audit
        ↓
SLA + comments + approvals + notifications
        ↓
Departments + routing + claim + administration
        ↓
Transfer + reassignment
        ↓
Attachments
        ↓
Authorized search/reporting privacy
        ↓
Department-routing SQL workflow
        ↓
Security Scan + container scan + CodeQL
```

CI يستخدم credentials وسياسات زمنية قصيرة خاصة بالاختبار ولا يحولها إلى default تجاري للمنتج.

## 16. جولة القبول اليدوية

بعد وصول Madar إلى `READY`:

1. ادخل كـAdministrator.
2. راجع `/admin/departments`.
3. راجع `/cases` وأنشئ/افتح حالة.
4. جرّب التوجيه والإسناد/إعادة الإسناد ضمن الصلاحية.
5. افتح تفاصيل الحالة وراجع التعليقات والموافقات والمرفقات والتدقيق.
6. راجع `/reports/cases`.
7. ادخل كـOperator وقارن نطاق الرؤية والإجراءات.
8. دوّن أي ملاحظة UX أو business rule؛ هذه هي المدخلات الصحيحة للإصدار التالي بدل إضافة ميزات عشوائية.

## 17. حدود Production

نجاح التشغيل المحلي وCI وRelease Publish لا يساوي Production Approval. البيئة الحقيقية ما زالت تحتاج، حسب استخدامها:

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

## 18. المراجع

- [`MADAR-SPECIFICATION-AR.md`](MADAR-SPECIFICATION-AR.md) — المواصفة الوظيفية الحالية.
- [`MADAR-LOCAL-RUN-PUBLISH-AR.md`](MADAR-LOCAL-RUN-PUBLISH-AR.md) — خطوات المستخدم من `git pull` حتى التشغيل والـpublish.
- [`../apps/Madar/README.md`](../apps/Madar/README.md) — مدخل المنتج.
- `site/madar-demo/` — Demo ثابتة لـGitHub Pages بلا خادم أو SQL.
