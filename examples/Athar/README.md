# منصة أثَر — المنتج العربي المرجعي

**أثَر** هو المنتج المرجعي العربي الكامل في المستودع. دوره إثبات أن FoundationKit يمكن استهلاكه داخل منتج Full-Stack مستقل يملك Domain وقاعدة بيانات وهوية وأمنًا وواجهة وتشغيلًا خاصًا به، من دون نقل قواعد المنتج إلى الحزم القابلة لإعادة الاستخدام.

> أثَر ليس Workbench، وليس المنتج التشغيلي Madar. Workbench هو executable architecture/reference consumer، أثَر هو complete reference product، وMadar هو operational product تحت `apps/`.

## ما الذي يثبته أثَر؟

- Domain وقواعد أعمال مستقلة؛
- Application orchestration؛
- ASP.NET Core Identity وCookie Authentication؛
- أدوار وصلاحيات ومبدأ maker-checker؛
- Anti-CSRF وRate Limiting وقفل الحساب؛
- MFA وعمليات الحساب الحساسة؛
- SQL Server وEF Core migrations؛
- idempotency وoptimistic concurrency؛
- audit trail؛
- Notifications وSMTP عبر FoundationKit؛
- Blazor WebAssembly + MudBlazor + واجهة عربية؛
- liveness/readiness وSwagger وPostman؛
- Docker وCI وSQL-backed E2E؛
- backup/restore verification في المسار الآلي.

## خريطة المشاريع

```text
examples/Athar/
├── Athar.Domain
├── Athar.Application
├── Athar.Infrastructure
├── Athar.Contracts
├── Athar.Api
└── Athar.Client

tests/
└── Athar.Tests
```

المشروع يستهلك FoundationKit حيث توجد حدود عامة فعلًا، بينما يبقي schema وmigrations وIdentity configuration والصلاحيات والنسخ العربية وسياسة المنتج داخل Athar.

## التدفق الكامل

```text
المستخدم ينشئ حسابًا
        ↓
Authentication + CSRF
        ↓
ينشئ مبادرة
        ↓
InitiativeManager + Initiative Aggregate
        ↓
SQL Server
        ↓
تظهر في قائمة الإدارة
        ↓
اعتماد أو رفض مع maker-checker
        ↓
Audit + status transition
        ↓
المستخدم يرى النتيجة
```

## التشغيل المحلي

التشغيل المحلي **مطبق ومدعوم الآن**؛ لم يعد خطوة مستقبلية.

المدير الموحد:

```powershell
.\foundationkit.ps1 start  -Target Athar -Mode Auto
.\foundationkit.ps1 status -Target Athar
.\foundationkit.ps1 logs   -Target Athar
.\foundationkit.ps1 stop   -Target Athar
```

المشغل المتخصص:

```powershell
.\scripts\athar-product.ps1 -Action Start -Mode Auto
```

`Auto` يستخدم Docker عندما يكون جاهزًا، وإلا يمكن استخدام Native على Windows مع .NET 10 وSQL Server محلي. التفاصيل الكانونية في:

```text
docs/LOCAL-RUN-WINDOWS-AR.md
```

خط المشروع الحالي هو `net10.0` ضمن baseline .NET 10 LTS الموثق في `docs/NET10-LTS-BASELINE.md`.

المنافذ تختلف حسب مسار التشغيل؛ استخدم `foundationkit.ps1 status` أو دليل التشغيل بدل الاعتماد على رقم قديم مكتوب في وثيقة منفصلة.

## البيانات والأسرار

- لا تُحفظ كلمات المرور أو connection strings الحقيقية داخل Git.
- المسارات المحلية تستخدم `.local/` أو User Secrets/Environment حسب المشغل.
- ملفات الاعتماد المحلية التي ينشئها مشغل Windows تُحمى بـACL للمستخدم الحالي.
- Production لا يستخدم Development bootstrap كسياسة إدارة حقيقية.

## Postman وقاعدة البيانات

مجموعة Postman:

```text
postman/Athar.Api.postman_collection.json
```

Athar يملك schemas/migrations الخاصة به، ويستخدم SQL Server في مسارات الاختبار والتشغيل. EF migrations هي مصدر حقيقة schema، وليست الوثائق.

## FoundationKit reuse

أثَر يوفر evidence حقيقيًا لعدة حدود عامة، منها Security وIdentity وAuthorization وWorkflow وApprovals وAuditing وNotifications وSMTP. وجود هذا الاستهلاك لا يعني أن كل هذه capabilities `Stable` أو Production Approved؛ maturity يُقرأ من Capability Model والـmachine metadata.

## حدود الإنتاج

أثَر يثبت Production-oriented code paths واختبارات أمن وتشغيل قوية، لكنه **ليس تصريح Production جاهزًا لكل بيئة**. قبل إطلاق أي deployment حقيقي يجب استكمال بوابة البيئة الخاصة به: HTTPS/ingress، Vault/KMS، SMTP، SQL identities، observability/SIEM، backup operations، legal/privacy requirements، load/penetration acceptance، وحوكمة GitHub المطلوبة في Issue #35.

الدليل الكانوني:

```text
docs/PRODUCTION-READINESS-AR.md
```
