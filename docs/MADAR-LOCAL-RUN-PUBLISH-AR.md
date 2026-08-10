# تشغيل مدار محليًا وتجهيز UAT وRelease Publish

هذه هي نقطة التسليم العملية لتشغيل **مدار v0.10** كاملًا على Windows وتجربته محليًا أو مشاركته مؤقتًا مع مختبرين.

> المسار البشري/UAT الأساسي هو **Native + SQL Server محلي**. Docker يبقى مسارًا مدعومًا للـCI والتكامل والانحدار والحاويات، لكنه ليس شرطًا للتجربة اليومية على Windows. GitHub Pages تعرض Demo ثابتة فقط ولا تشغّل ASP.NET Core أو SQL Server.

## 1. المتطلبات لمسار Native

على Windows:

- Git.
- PowerShell 5.1 أو أحدث.
- .NET 10 SDK وفق `global.json`.
- SQL Server محلي يعمل. الاتصال الافتراضي هو `Server=.` باستخدام Windows Integrated Security.

للتأكد:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 doctor
```

يفحص `doctor` الأدوات، .NET 10، خدمات SQL المحلية، المنفذ `8100`، Git، وحالة Madar. كما يعرض توفر Docker و`devtunnel` و`cloudflared` كأدوات اختيارية حسب المسار المستخدم.

## 2. تشغيل Madar Native

من جذر المستودع:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Madar -Mode Native
```

المشغل يقوم تلقائيًا بـ:

1. التحقق من .NET 10 وWindows.
2. إنشاء إعداد Development محلي عند أول تشغيل في `.local/madar-product.env`.
3. توليد كلمات مرور عشوائية لحسابي Administrator وOperator عند إنشاء الإعداد المحلي لأول مرة.
4. تقييد ACL لملف الإعداد على حساب Windows الحالي.
5. نشر Madar Release محليًا داخل `.local/madar-native/app`.
6. تشغيل `Madar.Api.dll` مباشرة، بدون استخدام Visual Studio launch profile.
7. استخدام SQL Server المحلي وقاعدة `MadarDb`، وتطبيق EF Core migrations وفق سياسة startup الموجودة في المنتج.
8. انتظار `/health/ready` حتى يصبح المنتج جاهزًا.
9. حفظ PID والـlogs وحالة نمط التشغيل داخل `.local/` فقط.

الرابط القياسي:

```text
http://localhost:8100
```

ملفات `launchSettings.json` التي قد يولدها Visual Studio ليست جزءًا من المسار القياسي ومُهملة في Git، لذلك لا تغيّر منفذ Madar.

## 3. بيانات الدخول المحلية

بعد أول تشغيل:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 credentials -Target Madar
```

أو مباشرة:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\madar-product.ps1 credentials
```

هذه الحسابات **Development/UAT فقط**.

### مهم عند وجود قاعدة قديمة

Bootstrap في Madar idempotent: إذا كان المستخدم موجودًا بالفعل في `MadarDb`، فإن startup لا يعيد كتابة كلمة مروره. لذلك إذا كانت قاعدة البيانات أقدم من ملف `.local/madar-product.env` الحالي، قد لا تطابق كلمة المرور المعروضة كلمة مرور المستخدم الموجود سابقًا. لا تحذف قاعدة البيانات أو تغيّر كلمات المرور تلقائيًا لمعالجة ذلك؛ تعامل معه كحالة بيانات محلية معروفة.

## 4. فتح النظام

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 open -Target Madar
```

أو يدويًا:

```text
http://localhost:8100
```

## 5. الحالة والـLogs والإيقاف

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 status -Target Madar
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 logs -Target Madar
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 stop -Target Madar
```

الحالة الطبيعية:

```text
Madar: READY
URL: http://localhost:8100
```

إيقاف Native يوقف عملية التطبيق ويحذف PID المحلي فقط؛ **قاعدة `MadarDb` وبيانات SQL تبقى محفوظة**.

## 6. مشاركة مؤقتة مع المختبرين

يجب أن يكون Madar في حالة `READY` قبل فتح أي Tunnel.

### Microsoft Dev Tunnels

بعد تثبيت `devtunnel` وتسجيل الدخول مرة واحدة:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 expose -Target Madar -TunnelProvider Microsoft
```

المشغل يستخدم منفذ `8100` ويفتح Dev Tunnel مؤقتًا مع `--allow-anonymous` حتى يستطيع المختبرون الدخول بدون حساب Microsoft خاص بالتانل.

**حد الأمان:** أي شخص يحصل على الرابط قد يتمكن من الوصول إلى نسخة Development ما دام الأمر يعمل. استخدم بيانات اختبار فقط، لا ترسل أسرارًا حقيقية، وأوقف التانل بـ`Ctrl+C` فور انتهاء جلسة UAT.

### Cloudflare Quick Tunnel

مع توفر `cloudflared`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 expose -Target Madar -TunnelProvider Cloudflare
```

يتم تشغيل Quick Tunnel إلى `http://localhost:8100` ويظهر رابط `trycloudflare.com` مؤقتًا. هذا الرابط ليس عنوان Production ولا يملك ضمان استمرارية.

### لماذا مساران؟

المساران مستقلان ويصلحان لمشاركة نفس نسخة UAT مع مختبرين مختلفين أو كبديل عند تعذر أحد المزودين. لا تُخزَّن روابط التانل أو credentials الخاصة بالمزودين داخل السورس.

## 7. ماذا نجرب في UAT؟

استخدم [`MADAR-ACCEPTANCE-CHECKLIST-AR.md`](MADAR-ACCEPTANCE-CHECKLIST-AR.md). الحد الأدنى يشمل:

1. تسجيل الدخول كـAdministrator.
2. إدارة الأقسام والعضويات.
3. إنشاء حالة.
4. التوجيه/الإسناد والـclaim.
5. دورة الحالة والتعليقات.
6. maker-checker approvals.
7. transfer/reassignment.
8. المرفقات.
9. البحث والتقارير.
10. مقارنة صلاحيات Operator مع Administrator.
11. مراجعة التدقيق والسلوك غير المصرح به.

طلب `/api/auth/me` غير المصادق عليه أثناء اكتشاف حالة الجلسة قد يرجع `401`، والعميل يحوله إلى مستخدم anonymous؛ هذا متوقع قبل تسجيل الدخول ولا يعني فشل جلسة مصادق عليها.

## 8. مسار Docker المتبقي

Docker لم يُحذف. لتشغيل topology الحاويات صراحةً:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Madar -Mode Docker
```

يبقى هذا المسار مهمًا لـ:

- Docker Compose regression.
- SQL container topology.
- readiness/container checks.
- security/container scans.
- CI/E2E evidence.

لا تخلط بيانات SQL المحلية Native مع Docker SQL volume؛ كل topology له مخزن بيانات تطوير مستقل.

## 9. إعادة إنشاء ملف الإعداد المحلي

المشغل المتخصص يدعم:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\madar-product.ps1 start -Mode Native -Reset
```

`-Reset` يعيد إنشاء ملف الإعداد المحلي فقط. **لا يعيد ضبط كلمات مرور المستخدمين الموجودين مسبقًا داخل `MadarDb`** ولا يحذف قاعدة البيانات. لذلك استخدمه فقط وأنت تفهم هذه الحدود.

## 10. إنشاء Release Publish

لا تحتاج Docker لإنشاء publish artifact؛ تحتاج .NET 10:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\madar-product.ps1 publish
```

الناتج:

```text
artifacts/madar/publish/
artifacts/madar/Madar-net10.0-Release.zip
artifacts/madar/Madar-net10.0-Release.zip.sha256
```

للتحقق من SHA-256:

```powershell
Get-FileHash .\artifacts\madar\Madar-net10.0-Release.zip -Algorithm SHA256
Get-Content .\artifacts\madar\Madar-net10.0-Release.zip.sha256
```

## 11. الفرق بين Native وDocker وTunnel وPublish وPages

```text
Native UAT
  Windows + local SQL Server + Madar runtime @ localhost:8100

Docker regression
  Docker Compose + Madar container + SQL Server container

Temporary UAT tunnel
  public temporary HTTPS URL → running localhost:8100

Release Publish
  Madar.Api publish folder + ZIP + SHA256

GitHub Pages
  static demo/documentation only; no real server, auth, SQL, or persistence
```

لا يساوي أي واحد من هذه المسارات **Production Approval**.

## 12. قبل أي استضافة Production

البيئة الحقيقية ما زالت تحتاج قرارات وضوابط خارج هذا الـUAT، منها:

- domain/HTTPS/ingress.
- secret vault وهوية SQL بأقل صلاحية.
- Data Protection keys دائمة ومحمية.
- قاعدة بيانات Production ونسخ احتياطي/استعادة.
- central logs/metrics/traces/alerts.
- object storage/KMS/malware scanning/retention للمرفقات عند الحاجة.
- مزود إشعارات فعلي وجدولة/تسليم durable عند الحاجة.
- الخصوصية والاحتفاظ والأداء والاستجابة للحوادث.

## 13. أوامر الجولة اليومية

```powershell
git pull
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 doctor
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Madar -Mode Native
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 credentials -Target Madar
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 open -Target Madar
```

للمشاركة:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 expose -Target Madar -TunnelProvider Microsoft
# أو في جلسة مستقلة:
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 expose -Target Madar -TunnelProvider Cloudflare
```

بعد الاختبار:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 stop -Target Madar
```

هذه هي نقطة UAT المعتمدة قبل تجميد v0.10 والانتقال إلى تطوير FoundationKit Core بناءً على أدلة الاستخدام الفعلية.
