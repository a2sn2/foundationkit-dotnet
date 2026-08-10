# قائمة قبول مدار محليًا — v0.10

هذه القائمة هي جولة **UAT بشرية** بعد اكتمال التسليم التقني. لا تحتاج لتعديل كود أو قاعدة بيانات يدويًا؛ المطلوب تشغيل Madar وتجربة المنتج من منظور الاستخدام.

راجع أيضًا:

- [`MADAR-SPECIFICATION-AR.md`](MADAR-SPECIFICATION-AR.md)
- [`MADAR-LOCAL-RUN-PUBLISH-AR.md`](MADAR-LOCAL-RUN-PUBLISH-AR.md)
- [`MADAR-OPERATIONS-AR.md`](MADAR-OPERATIONS-AR.md)

## 1. تجهيز البيئة

من جذر المستودع:

```powershell
git pull --ff-only origin main
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 doctor
```

المطلوب لمسار Native:

- .NET 10 موجود.
- SQL Server المحلي يعمل.
- المنفذ `8100` متاح أو مملوك لنسخة Madar سليمة.
- لا يوجد FAIL في متطلبات التشغيل الأساسية.

Docker و`devtunnel` و`cloudflared` أدوات اختيارية حسب topology والمشاركة المستخدمة.

```text
[ ] PASS
[ ] FAIL
ملاحظات:
```

## 2. تشغيل Madar Native

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 start -Target Madar -Mode Native
```

المطلوب:

```text
Madar: READY
URL: http://localhost:8100
```

ثم:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 credentials -Target Madar
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 open -Target Madar
```

ملاحظة: إذا كانت `MadarDb` موجودة من تشغيل أقدم، قد لا تطابق كلمات المرور المولدة حديثًا مستخدمين موجودين سابقًا لأن bootstrap لا يعيد كتابة كلمات مرورهم.

```text
[ ] PASS
[ ] FAIL
ملاحظات:
```

## 3. تسجيل الدخول كـAdministrator

تحقق من:

- نجاح تسجيل الدخول.
- ظهور الواجهة العربية بشكل طبيعي.
- عدم وجود assets أساسية مفقودة.
- وجود favicon بدل 404 المتكرر السابق.
- إمكانية الوصول إلى `/cases` و`/reports/cases` و`/admin/departments`.
- عدم ظهور تحذير browser خاص بوجود password input خارج form.

قد يظهر `401` على `/api/auth/me` قبل تسجيل الدخول أثناء اكتشاف حالة المصادقة؛ هذا متوقع إذا تحولت الواجهة بعدها إلى anonymous/login بصورة سليمة.

```text
[ ] PASS
[ ] FAIL
ملاحظات:
```

## 4. إدارة الأقسام

من `/admin/departments`:

- راجع الأقسام الموجودة.
- تأكد أن البيانات واضحة.
- راجع عضوية Operator.
- لا تعدّل بيانات لا تريد الاحتفاظ بها في SQL المحلي.

```text
[ ] PASS
[ ] FAIL
ملاحظات:
```

## 5. إنشاء حالة

من `/cases` أنشئ حالة تجريبية فقط وسجل:

```text
العنوان:
النوع:
الأولوية:
رقم الحالة:
```

تحقق من الحفظ والظهور والـlifecycle الأولي.

```text
[ ] PASS
[ ] FAIL
ملاحظات:
```

## 6. التوجيه والإسناد

- وجّه الحالة إلى قسم فعال.
- اسندها أو أعد إسنادها إلى Operator مؤهل.
- راجع طابور القسم.
- تأكد أن التوجيه والإسناد لا يمسحان بيانات الحالة السابقة.

```text
[ ] PASS
[ ] FAIL
ملاحظات:
```

## 7. تسجيل الدخول كـOperator

اخرج من الحساب الإداري وسجل الدخول بـOperator.

تحقق من:

- نطاق الرؤية الصحيح.
- طابور القسم والحالات المسندة.
- عدم توفر إجراءات إدارية غير مخولة.
- أن المصدر النهائي للصلاحية هو Application/API وليس مجرد إخفاء UI.

```text
[ ] PASS
[ ] FAIL
ملاحظات:
```

## 8. Claim ودورة الحالة

على حالة مؤهلة:

- جرّب claim.
- تقدم عبر الانتقالات المسموحة.
- تحقق من رفض الانتقال غير المنطقي.

الدورة المرجعية:

```text
new → assigned → in-progress → resolved → closed
```

```text
[ ] PASS
[ ] FAIL
ملاحظات:
```

## 9. التعليقات

- أضف تعليقًا تجريبيًا.
- أعد تحميل الصفحة.
- تأكد من بقاء التعليق وارتباطه بالحالة الصحيحة.

```text
[ ] PASS
[ ] FAIL
ملاحظات:
```

## 10. الموافقات

إذا كان السيناريو يعرض approval flow:

- أنشئ/راجع طلب موافقة حسب الدور.
- تحقق من maker-checker في القرار الحساس.

```text
[ ] PASS
[ ] FAIL
[ ] غير منطبق
ملاحظات:
```

## 11. التحويل وإعادة الإسناد

جرّب reassignment وtransfer على بيانات تجريبية مناسبة.

عند transfer المتوقع:

- القسم يتغير.
- الإسناد السابق يزال.
- الحالة تعود `new` في طابور القسم الهدف.
- المحتوى والتعليقات والموافقات والمرفقات والتاريخ السابق لا تختفي.

```text
[ ] PASS
[ ] FAIL
ملاحظات:
```

## 12. المرفقات

استخدم ملفًا تجريبيًا غير حساس.

```text
Allowed: PDF / PNG / JPEG / TXT
Maximum: 10 MiB
```

تحقق من upload، metadata، download للمخول، ورفض النوع/الحجم غير المسموح عند اختباره. لا ترفع مستندات شخصية أو مالية حقيقية.

```text
[ ] PASS
[ ] FAIL
ملاحظات:
```

## 13. البحث والتقارير

افتح:

```text
http://localhost:8100/reports/cases
```

جرّب كلمة من عنوان حالة، فلترًا متاحًا، paging عند توفر نتائج، وقارن النتائج/العدادات بين Administrator وOperator. لا يجب أن تكشف العدادات أو النتائج حالات خارج نطاق رؤية الدور الأضيق.

```text
[ ] PASS
[ ] FAIL
ملاحظات:
```

## 14. SLA — اختياري

SLA معطل افتراضيًا في الإعداد المحلي. لا تغيّره لقبول تجربة المنتج الأساسية. إذا اختبرته، استخدم قيم Development فقط واتبع `MADAR-OPERATIONS-AR.md`.

```text
[ ] PASS
[ ] FAIL
[ ] لم أختبر SLA يدويًا
ملاحظات:
```

## 15. Audit / Timeline

راجع أثر:

- الإنشاء.
- التوجيه/الإسناد.
- الانتقالات.
- الموافقات.
- transfer/reassignment عند تطبيقها.

```text
[ ] PASS
[ ] FAIL
ملاحظات:
```

## 16. مشاركة Microsoft — اختياري حسب جولة UAT

مع Madar `READY`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 expose -Target Madar -TunnelProvider Microsoft
```

تحقق من فتح الرابط من جهاز آخر/نافذة مستقلة. هذا tunnel anonymous ومؤقت؛ استخدم بيانات اختبار وأوقفه بـ`Ctrl+C`.

```text
[ ] PASS
[ ] FAIL
[ ] لم أختبر
ملاحظات:
```

## 17. مشاركة Cloudflare — اختياري حسب جولة UAT

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 expose -Target Madar -TunnelProvider Cloudflare
```

تحقق من الرابط المؤقت ثم أوقفه بـ`Ctrl+C`. عدم توفر مزود واحد لا يغيّر صحة تشغيل Madar المحلي؛ المساران بديلان مستقلان للمشاركة.

```text
[ ] PASS
[ ] FAIL
[ ] لم أختبر
ملاحظات:
```

## 18. Health وLogs

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 status -Target Madar
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 logs -Target Madar
```

تحقق من بقاء المنتج `READY` وعدم وجود exception متكرر أثناء الجولة.

```text
[ ] PASS
[ ] FAIL
ملاحظات:
```

## 19. Release Publish

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\madar-product.ps1 publish
```

تحقق من:

```text
artifacts/madar/publish/Madar.Api.dll
artifacts/madar/Madar-net10.0-Release.zip
artifacts/madar/Madar-net10.0-Release.zip.sha256
```

ثم قارن:

```powershell
Get-FileHash .\artifacts\madar\Madar-net10.0-Release.zip -Algorithm SHA256
Get-Content .\artifacts\madar\Madar-net10.0-Release.zip.sha256
```

```text
[ ] PASS
[ ] FAIL
ملاحظات:
```

## 20. الإيقاف

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 stop -Target Madar
powershell -NoProfile -ExecutionPolicy Bypass -File .\foundationkit.ps1 status -Target Madar
```

في Native تبقى `MadarDb` المحلية للتشغيل التالي.

```text
[ ] PASS
[ ] FAIL
ملاحظات:
```

## 21. نتيجة القبول

```text
تاريخ التجربة:
الجهاز/Windows:
.NET SDK:
SQL Server instance:
طريقة المشاركة إن استخدمت: Microsoft / Cloudflare / لا يوجد

النتيجة العامة:
[ ] مقبول للتجربة الحالية
[ ] مقبول مع ملاحظات UX/Business
[ ] يوجد عطل يمنع القبول

أهم الملاحظات:
1.
2.
3.
```

إذا وجدت مشكلة، احتفظ باسم الصفحة/العملية، الخطوات، المتوقع، الفعلي، Screenshot عند الحاجة، وآخر logs ذات الصلة بدون كلمات مرور أو أسرار. هذه المعلومات هي مدخل الإصلاح أو الإصدار التالي، لا التخمين.
