# إضافة منتج جديد بجانب FoundationKit

## القاعدة الحالية

كل منتج جديد يملك Domain وسياسات وبيانات وتشغيلًا مستقلًا، ويستهلك FoundationKit حسب الحاجة دون تعديل الكور ليناسب منتجًا واحدًا.

```text
samples/                  مراجع معمارية صغيرة/موجهة
examples/<ProjectName>/   منتجات مرجعية كاملة
apps/<ProjectName>/       منتجات تشغيلية فعلية
```

`apps/` **موجود ومستخدم بالفعل**؛ `apps/Madar` هو المنتج التشغيلي الحالي. `examples/Athar` يبقى المنتج العربي المرجعي الكامل ولا يحتاج نقله إلى `apps/`.

## Composer اليوم

`FoundationKit.Composer` يستطيع اكتشاف capabilities/profiles والتحقق من manifest والاعتماديات والنضج وcontract compatibility، لكنه **لا يولد مشروعًا بعد**. لا يوجد حاليًا `foundationkit new` أو scaffolding حتمي؛ لذلك إنشاء المنتج ما زال قرارًا معماريًا صريحًا وليس نسخ قالب آلي.

## هيكل مقترح عند الحاجة

```text
<Project>.Domain
<Project>.Application
<Project>.Infrastructure
<Project>.Contracts
<Project>.Api
<Project>.Client
tests/<Project>.Tests
```

هذا هو الشكل الذي أثبته Athar وMadar، لكنه ليس إلزامًا لكل خدمة صغيرة. استخدم فقط المشاريع التي يحتاجها المنتج فعليًا.

## اتجاه الاعتماد

```text
Domain
  ↑
Application ← Contracts
  ↑
Infrastructure
  ↑
Api ← Client hosting

Client → Contracts + FoundationKit.Blazor
```

الممنوع:

- Domain يعتمد على EF Core أو ASP.NET Core؛
- Client يعتمد على Infrastructure أو DbContext؛
- Migrations داخل `src/FoundationKit.*`؛
- Entity من قاعدة البيانات يُعاد مباشرة من API؛
- Generic CRUD يبتلع قواعد المنتج؛
- نقل organization/files/search/reporting أو أي سلوك منتج إلى FoundationKit بلا evidence مستقل.

## اختيار FoundationKit packages

ابدأ من الحاجة الفعلية، ثم راجع Capability Model/Composer. لا تضف كل الـ17 package تلقائيًا.

أمثلة:

- Domain/Application/Infrastructure/WebApi/Blazor حسب نوع التطبيق؛
- Authorization عند وجود permission/ownership boundary مناسب؛
- Workflow عند وجود state-transition graph حتمي؛
- Approvals عند توافق maker-checker/approve-reject v1 مع المنتج؛
- Notifications + SMTP عند الحاجة للنقل الحالي؛
- Settings/FeatureManagement/Localization/Caching عندما تنطبق حدودها الحالية.

الم maturity والـcontract version لا تُستنتجان من رقم NuGet package؛ اقرأ `catalog/foundationkit.capabilities.json` وComposer.

## خطوات إنشاء منتج

1. عرّف نطاق المنتج وملكيته للبيانات والسياسات أولًا.
2. اختر `apps/` للمنتج التشغيلي أو `examples/` للمرجع الكامل.
3. صمم Domain/use cases قبل UI والتخزين التفصيلي.
4. عرّف Contracts حسب الجمهور.
5. اختر FoundationKit capabilities التي تناسب boundary حقيقيًا فقط.
6. نفذ Infrastructure وDbContext ومهاجرات المنتج.
7. أضف Authentication/Authorization/Privacy حسب طبيعة المنتج.
8. أنشئ typed client وواجهة UI عندما يحتاجها المنتج.
9. أضف tests + SQL/E2E + Docker/operational path بحسب المخاطر.
10. اربط المنتج ببوابات CI/Security/CodeQL ومستندات التشغيل.
11. حدّث Atlas/README فقط بعد أن تصبح الأسطح حقيقية.
12. إذا كشف المنتج عن concern متكرر، اتركه product-owned أولًا ثم قيّم الاستخراج بعد ظهور evidence مستقل.

## ملاحظة مهمة

لا تنسخ Athar أو Madar حرفيًا ثم تغير الاسم. استخدمهما كـevidence وخرائط تنفيذ، وليس كقالب يفرض سياسات الهوية أو الأدوار أو الأقسام أو SLA أو التخزين على منتج جديد.
