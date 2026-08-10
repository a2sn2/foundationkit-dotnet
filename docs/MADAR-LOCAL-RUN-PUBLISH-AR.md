# تشغيل مدار محليًا وتجهيز Release Publish

هذه هي نقطة التسليم العملية للمستخدم الذي يريد تشغيل **مدار** كاملًا على جهازه بعد تنزيل المستودع.

> المسار المحلي المعتمد حاليًا لمدار هو Docker. GitHub Pages تعرض Demo ثابتة فقط ولا تشغل ASP.NET Core أو SQL Server.

## 1. المتطلبات

على Windows:

- Git.
- PowerShell 5.1 أو أحدث.
- .NET 10 SDK وفق `global.json`.
- Docker Desktop مع Docker Compose، ويجب أن يكون Engine في حالة Running.

للتأكد:

```powershell
.\foundationkit.ps1 doctor
```

يجب ألا يظهر فشل في الأدوات المطلوبة. وجود SQL Server محلي ليس مطلوبًا لمسار Madar Docker لأن Compose يشغل SQL Server الخاص ببيئة التطوير.

## 2. تشغيل مدار لأول مرة

من جذر المستودع:

```powershell
.\foundationkit.ps1 start -Target Madar -Mode Docker
```

المشغل يقوم تلقائيًا بـ:

1. التحقق من Docker.
2. إنشاء secrets عشوائية محلية للتطوير عند أول تشغيل.
3. حفظها في `.local/madar-product.env`.
4. تقييد ACL للملف على Windows.
5. بناء وتشغيل SQL Server وMadar API/Blazor.
6. انتظار `/health/ready` حتى يصبح المنتج جاهزًا.

الرابط الافتراضي:

```text
http://localhost:8100
```

## 3. معرفة بيانات الدخول المحلية

بعد أول `start`:

```powershell
.\scripts\madar-product.ps1 credentials
```

سيظهر حساب Administrator وحساب Operator وكلمات المرور التي تم توليدها على جهازك.

هذه حسابات **Development محلية فقط**. لا تنسخ ملف `.local/madar-product.env` إلى Git ولا تستخدم هذه الحسابات كسياسة Production.

## 4. فتح النظام

```powershell
.\foundationkit.ps1 open -Target Madar
```

أو افتح يدويًا:

```text
http://localhost:8100
```

## 5. ماذا تجرب؟

الحد الأدنى لجولة القبول اليدوية:

1. سجل الدخول كـAdministrator.
2. افتح إدارة الأقسام وتأكد من ظهور بيانات التطوير المبدئية.
3. افتح الحالات وأنشئ حالة جديدة أو استخدم البيانات المتاحة.
4. جرّب التوجيه والإسناد ضمن الصلاحيات.
5. افتح تفاصيل الحالة وتحقق من التعليقات والموافقات والمرفقات والتدقيق.
6. افتح `/reports/cases` وتحقق من البحث والتلخيص.
7. سجل الدخول كـOperator وقارن ما يستطيع رؤيته وتنفيذه مع Administrator.

هذه الجولة لا تستبدل الاختبارات الآلية، لكنها هي دور المستخدم النهائي للتحقق من تجربة المنتج نفسها.

## 6. الحالة والـLogs

```powershell
.\foundationkit.ps1 status -Target Madar
.\foundationkit.ps1 logs -Target Madar
```

الحالة الطبيعية:

```text
Madar: READY
URL: http://localhost:8100
```

## 7. إيقاف النظام بدون حذف البيانات

```powershell
.\foundationkit.ps1 stop -Target Madar
```

هذا يوقف containers ويحافظ على SQL volume.

في التشغيل التالي:

```powershell
.\foundationkit.ps1 start -Target Madar -Mode Docker
```

سيستخدم نفس الإعداد المحلي والبيانات المحفوظة.

## 8. إعادة توليد بيانات التطوير المحلية

المشغل المتخصص يدعم إعادة إنشاء ملف secrets المحلي عند الحاجة:

```powershell
.\scripts\madar-product.ps1 start -Reset
```

استخدمه فقط عندما تفهم أثر تغيير بيانات الاعتماد على بيئة التطوير القائمة. هذا الخيار لا يُقدَّم كعملية Production لإدارة الأسرار.

## 9. إنشاء Release Publish

لا تحتاج Docker لإنشاء publish folder، لكن تحتاج .NET 10 SDK:

```powershell
.\scripts\madar-product.ps1 publish
```

الناتج:

```text
artifacts/madar/publish/
artifacts/madar/Madar-net10.0-Release.zip
artifacts/madar/Madar-net10.0-Release.zip.sha256
```

الـZIP هو framework-dependent Release output لـ`Madar.Api` ويحتوي واجهة Blazor المستضافة معه.

للتحقق من SHA-256 على PowerShell:

```powershell
Get-FileHash .\artifacts\madar\Madar-net10.0-Release.zip -Algorithm SHA256
Get-Content .\artifacts\madar\Madar-net10.0-Release.zip.sha256
```

## 10. الفرق بين Local Run وPublish وGitHub Pages

### Local Run

```text
Docker Compose
  ├─ Madar ASP.NET Core + Blazor
  └─ SQL Server
```

هذا هو المنتج الحقيقي القابل للتجربة.

### Release Publish

```text
Madar.Api publish folder + ZIP + SHA256
```

هذا artifact لنقله إلى بيئة استضافة مناسبة بعد توفير إعداداتها.

### GitHub Pages

GitHub Pages لا تستطيع استضافة ASP.NET Core/SQL Server الخاص بمدار. لذلك البوابة تنشر **Demo ثابتة بلا خادم** لشرح تجربة المنتج، مع روابط واضحة للسورس والمواصفة وأوامر التشغيل المحلي. لا تحفظ Demo بيانات حقيقية ولا تدعي أنها runtime المنتج.

## 11. قبل أي استضافة حقيقية

الـpublish artifact لا يساوي Production deployment. تحتاج البيئة المستهدفة إلى قرارات فعلية، منها:

- Connection string بحساب SQL أقل صلاحية.
- secret store مناسب.
- HTTPS/TLS وdomain/ingress.
- Data Protection keys دائمة ومحمية.
- object storage وسياسة malware scanning/retention للمرفقات إذا كانت البيئة تتطلب ذلك.
- SMTP أو مزود إشعار فعلي إن استخدم.
- central logs/metrics/traces/alerts.
- backup/restore للبيئة المستهدفة.
- سياسة SLA وقيمها الحقيقية.
- مراجعة الخصوصية والاحتفاظ والأداء.

## 12. أوامر اليوم الواحد

إذا كان المستودع موجودًا لديك ومحدثًا:

```powershell
git pull
.\foundationkit.ps1 doctor
.\foundationkit.ps1 start -Target Madar -Mode Docker
.\scripts\madar-product.ps1 credentials
.\foundationkit.ps1 open -Target Madar
```

بعد التجربة:

```powershell
.\foundationkit.ps1 stop -Target Madar
```

ولإنشاء نسخة Release:

```powershell
.\scripts\madar-product.ps1 publish
```

هذه هي نقطة التسليم التي يجب أن تصل إليها المرحلة الحالية قبل أن يصبح دور المستخدم هو القبول اليدوي للمنتج.
