# تشغيل FoundationKit محليًا على Windows

هذا هو الدليل الكانوني لتشغيل المستودع على Windows. الخط الحالي هو **.NET 10 LTS / `net10.0`**.

> استخدم بيانات تجريبية فقط. الملفات تحت `.local/` محلية ومهملة من Git، ولا تمثل Secret Management لبيئة Production.

## 1. المتطلبات

الحد الأدنى:

- Git.
- PowerShell 5.1 أو أحدث.
- .NET 10 SDK وفق `global.json`.

لـWorkbench/Athar/Madar Native:

- SQL Server محلي يعمل، مثل `MSSQLSERVER` أو instance مناسب.

اختياري حسب الحاجة:

- Docker Desktop/Engine مع Docker Compose للتكامل والانحدار والحاويات.
- Microsoft Dev Tunnels CLI (`devtunnel`) لمشاركة UAT المؤقتة.
- `cloudflared` لمسار مشاركة UAT مستقل.
- Visual Studio 2026 مع ASP.NET and web development.
- SSMS.
- Python/Node.js لفحوصات إضافية.
- `sqlcmd` لبعض العمليات المحلية.

## 2. نسخة نظيفة

```powershell
git clone https://github.com/a2sn2/foundationkit-dotnet.git
cd foundationkit-dotnet
git switch main
git pull --ff-only origin main
git status --short
```

Visual Studio قد يولد `launchSettings.json` تحت مشروعات Madar؛ هذه الملفات المحلية مهملة في Git ولا تُستخدم في مسار Madar Native الكانوني.

## 3. Preflight

بسبب سياسات Execution Policy الشائعة على Windows، الصيغة المحمولة الموصى بها هي:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 doctor
```

وعند الحاجة:

```powershell
dotnet --info
docker info
docker compose version
devtunnel --version
cloudflared --version
```

`doctor` يتحقق من الأدوات و.NET 10 وخدمات SQL والمنافذ وGit وحالة التطبيقات، ويعرض Docker وأدوات الأنفاق كاختيارات إضافية.

## 4. Workbench — Native

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Workbench -Mode Native
```

الرابط:

```text
http://localhost:5057
```

Default SQL connection:

```text
Server=.;Database=FoundationKitWorkbench;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

إذا كان لديك instance مختلف، عدّل الإعداد المحلي تحت `.local/workbench-product.env` وفق توثيق Workbench.

## 5. Athar — Native

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Athar -Mode Native
```

الرابط:

```text
http://localhost:8090
```

بيانات الإدارة المحلية:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 credentials -Target Athar
```

Default SQL connection:

```text
Server=.;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

## 6. Madar — المسار الأساسي للمستخدم/UAT

Madar هو المنتج التشغيلي تحت `apps/Madar`. المسار الأساسي على Windows هو Native:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Madar -Mode Native
```

الرابط:

```text
http://localhost:8100
```

الـSQL الافتراضي:

```text
Server=.;Database=MadarDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

عند أول تشغيل ينشئ المشغل إعدادات Development داخل `.local/` ويقيّد ملف credentials بـACL على Windows. Madar يُنشر إلى `.local/madar-native/app` ثم يعمل مباشرة من `Madar.Api.dll`؛ لا يعتمد على `launchSettings.json`.

اعرض بيانات الدخول:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 credentials -Target Madar
```

افتحه:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 open -Target Madar
```

الحالة والسجلات:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 status -Target Madar
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 logs -Target Madar
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

الإيقاف يحافظ على SQL المحلي:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 stop -Target Madar
```

### ملاحظة credentials

Bootstrap لا يعيد ضبط كلمة مرور مستخدم موجود مسبقًا في `MadarDb`. لذلك إذا كانت القاعدة أقدم من `.local/madar-product.env` الحالي، قد لا تكون كلمة المرور المولدة حديثًا هي كلمة المرور الفعلية لذلك المستخدم الموجود.

## 7. مشاركة Madar مع مختبرين

يجب أن يكون Madar `READY` أولًا.

### Microsoft Dev Tunnels

بعد تثبيت `devtunnel` وتسجيل الدخول:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 expose -Target Madar -TunnelProvider Microsoft
```

يبدأ رابطًا عامًا مؤقتًا باستخدام `--allow-anonymous`. استخدم test data/accounts وأوقف الأمر بـ`Ctrl+C` بعد جلسة UAT.

### Cloudflare Quick Tunnel

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 expose -Target Madar -TunnelProvider Cloudflare
```

يظهر رابط `trycloudflare.com` مؤقت. لا تُخزَّن روابط أو credentials للأنفاق داخل Git.

كلا المسارين **UAT/Development فقط** وليسا Production hosting.

## 8. Madar — Docker regression path

Docker لم يُحذف. لتشغيل topology الحاويات:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Madar -Mode Docker
```

هذا المسار مهم للـCI، SQL container، readiness، container hardening، E2E، وsecurity scanning. لا تعتبر Docker SQL volume هو نفسه `MadarDb` المحلية المستخدمة في Native.

## 9. Madar Release Publish

لا يحتاج publish إلى Docker:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\madar-product.ps1 publish
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

هذا artifact تقني، وليس Production deployment.

## 10. تشغيل Docker للمنتجات

يمكن استخدام Docker صراحةً في المسارات المدعومة:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Workbench -Mode Docker
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Athar -Mode Docker
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Madar -Mode Docker
```

بالنسبة إلى Madar، `Auto` يفضّل Native على Windows عندما تتوفر .NET 10؛ يمكن طلب Docker صراحةً عند الحاجة.

## 11. منافذ التطوير المعروفة

```text
Workbench Native  5057
Workbench Docker  8080
Athar              8090
Madar              8100
Workbench SQL      14333 (Docker topology)
Athar SQL          14334 (Docker topology)
Madar SQL          14335 (Docker topology)
```

إذا كان منفذ مستخدمًا، `doctor` يظهر listener/PID بدل الادعاء أن التطبيق متوقف.

## 12. Build/Test/Verify

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 restore
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 build
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 test
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 verify
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 pack
```

الحزمة المتوقعة:

```text
17 .nupkg
17 .snupkg
```

## 13. GitHub Pages

`site/athar-demo/` و`site/madar-demo/` معاينات ثابتة. Madar demo بلا خادم أو SQL أو Authentication حقيقي أو حفظ دائم، ولا تستبدل runtime المنتج.

## 14. إذا فشل Madar Native

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 status -Target Madar
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 logs -Target Madar
```

ثم راجع:

- خدمة SQL المحلية.
- المنفذ 8100.
- `.local/logs/madar-native.err.log`.
- `.local/logs/madar-native.out.log`.
- connection string إذا كان SQL instance مختلفًا.

لا تبدأ بحذف `MadarDb`.

## 15. حدود Production

نجاح Native أو Docker أو Tunnel أو `dotnet publish` لا يثبت Production readiness. الاستضافة الحقيقية تحتاج قرارات بيئية مستقلة مثل domain/HTTPS/ingress، secret store، least-privilege database identity، Data Protection keys، backups، observability، object storage/malware scanning/retention، SLA الحقيقي، privacy/legal/performance acceptance.

راجع [`PRODUCTION-READINESS-AR.md`](PRODUCTION-READINESS-AR.md).

## 16. أقصر مسار لتجربة Madar

```powershell
git pull --ff-only origin main
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 doctor
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Madar -Mode Native
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 credentials -Target Madar
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 open -Target Madar
```

للمشاركة المؤقتة، شغّل Microsoft أو Cloudflare في Terminal مستقلة. بعد التجربة:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 stop -Target Madar
```
