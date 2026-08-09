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

`FoundationKit.Composer` هو نقطة البداية الآلية الحالية عند إنشاء هيكل منتج جديد. يستطيع:

- اكتشاف capabilities وprofiles؛
- التحقق الصارم من manifest والاعتماديات؛
- فحص capability contract compatibility؛
- إظهار maturity warnings أو رفض التوليد مع `--require-stable`؛
- توليد Solution حتمية من نفس Capability Graph؛
- إنشاء Domain/Application/Infrastructure وAPI/Client عندما تحل قدراتهما؛
- إنشاء Test project؛
- توليد `ARCHITECTURE.md` يشرح سبب كل capability وما إذا كان لها reusable package حقيقي؛
- إعادة التوليد بـ`--force` فقط إذا كان المجلد ما زال يحتوي حصريًا على الملفات التي سبق للـComposer توليدها.

مثال داخل نسخة repository محلية:

```powershell
dotnet run --project tools/FoundationKit.Composer -- `
  new docs/examples/foundationkit.project.minimal.json `
  --output artifacts/MySystem `
  --foundation-root .
```

بدون `--foundation-root` يكتب Composer `PackageReference` إلى حزم FoundationKit الحالية. هذا الوضع يحتاج NuGet source يحتوي تلك الحزم. وضع `--foundation-root` يستخدم `ProjectReference` إلى نفس source tree وهو المسار الذي يثبته CI حاليًا بالبناء والاختبار.

التوليد الحالي **غير تفاعلي**؛ أنت تكتب/تختار manifest ثم تولد. الـwizard التفاعلي والـvisual composer ما زالا طبقة UX لاحقة، لكن محرك التوليد الحتمي نفسه موجود الآن.

## ماذا يولد وماذا لا يولد؟

الهيكل الأساسي الناتج هو:

```text
<Project>.Domain
<Project>.Application
<Project>.Infrastructure
<Project>.Api        عند حل web-api
<Project>.Client     عند حل blazor
tests/<Project>.Tests
```

ويولد كذلك:

```text
<Project>.sln
Directory.Build.props
Directory.Packages.props
foundationkit.project.json
.foundationkit-generated.json
README.md
ARCHITECTURE.md
```

لكن Composer لا يخترع:

- Entities أو Aggregates خاصة بالمنتج؛
- DbContext أو Migrations؛
- أدوار وصلاحيات المنتج؛
- tenant/organization model؛
- SLA أو routing أو attachment/search/reporting semantics؛
- أسرار أو connection strings؛
- Production deployment topology.

إذا احتوى الـprofile على capability مخططة أو لا يوجد لها reusable package، يسجلها Composer بوضوح في تقرير المعمارية ولا يصنع package وهمية لها.

## اتجاه الاعتماد

```text
Domain
  ↑
Application
  ↑
Infrastructure
  ↑
Api

Client = boundary مستقل للواجهة
```

بعد التوليد تستطيع إضافة Contracts أو تقسيمات إضافية بحسب جمهور المنتج؛ المولد v1 يتعمد إبقاء البداية صغيرة بدل نسخ Athar/Madar بالكامل.

الممنوع:

- Domain يعتمد على EF Core أو ASP.NET Core؛
- Client يعتمد على Infrastructure أو DbContext؛
- Migrations داخل `src/FoundationKit.*`؛
- Entity من قاعدة البيانات يُعاد مباشرة من API؛
- Generic CRUD يبتلع قواعد المنتج؛
- نقل organization/files/search/reporting أو أي سلوك منتج إلى FoundationKit بلا evidence مستقل؛
- التعامل مع وجود capability في catalog كأنه يعني وجود implementation/package مكتمل.

## اختيار FoundationKit packages

ابدأ من الحاجة الفعلية، ثم استخدم Capability Model/Composer. لا تضف كل الـ17 package تلقائيًا.

أمثلة:

- Domain/Application/Infrastructure/WebApi/Blazor حسب نوع التطبيق؛
- Authorization عند وجود permission/ownership boundary مناسب؛
- Workflow عند وجود state-transition graph حتمي؛
- Approvals عند توافق maker-checker/approve-reject v1 مع المنتج؛
- Notifications + SMTP عند الحاجة للنقل الحالي؛
- Settings/FeatureManagement/Localization/Caching عندما تنطبق حدودها الحالية.

الـmaturity والـcontract version لا تُستنتجان من رقم NuGet package؛ اقرأ `catalog/foundationkit.capabilities.json` واستخدم Composer.

## خطوات إنشاء منتج

1. عرّف نطاق المنتج وملكيته للبيانات والسياسات أولًا.
2. أنشئ manifest يعبّر فقط عن profile/capabilities/providers المطلوبة فعلًا.
3. نفذ `validate` و`explain` قبل التوليد عند الحاجة لمراجعة التركيب.
4. شغل `new ... --output ...` لتوليد الهيكل، أو `--foundation-root .` إذا كنت تطور داخل نسخة FoundationKit المصدرية.
5. راجع `ARCHITECTURE.md` الناتج، خصوصًا maturity warnings والقدرات التي لا تملك runtime binding.
6. انقل الهيكل إلى `apps/` للمنتج التشغيلي أو `examples/` للمرجع الكامل إذا كان سيعيش داخل هذا repository.
7. صمم Domain/use cases الحقيقية قبل UI والتخزين التفصيلي.
8. أضف Contracts فقط بحسب الجمهور الفعلي.
9. نفذ Infrastructure وDbContext ومهاجرات المنتج عندما يحتاج قاعدة بيانات.
10. أضف Authentication/Authorization/Privacy حسب طبيعة المنتج، ولا تفترض أن المولد اختار سياسة العمل بدلًا عنك.
11. أنشئ typed clients وUI الحقيقيين فوق boundary المولد عندما يحتاج المنتج.
12. أضف tests + SQL/E2E + Docker/operational path بحسب المخاطر.
13. اربط المنتج ببوابات CI/Security/CodeQL ووثائق التشغيل.
14. حدّث Atlas/README فقط بعد أن تصبح الأسطح حقيقية.
15. إذا كشف المنتج عن concern متكرر، اتركه product-owned أولًا ثم قيّم الاستخراج بعد ظهور evidence مستقل.

## ملاحظة مهمة

لا تنسخ Athar أو Madar حرفيًا ثم تغير الاسم. استخدمهما كـevidence وخرائط تنفيذ. واستخدم Composer لتوحيد **البداية المعمارية** فقط، لا لفرض سياسات الهوية أو الأدوار أو الأقسام أو SLA أو التخزين على منتج جديد.
