# جاهزية FoundationKit والمنتجات للإنتاج

## التعريف الصحيح

FoundationKit لا يمكن أن يمنح أي منتج عبارة «Production Ready لأي بيئة» بمجرد وجود الكود. الجاهزية النهائية تعتمد على deployment حقيقي، threat model، البيانات، الحمل، المراقبة، النسخ الاحتياطي، والحوكمة.

الوضع الصحيح للمستودع حاليًا:

```text
FoundationKit Core v0.1
  = technical/composable baseline مكتمل للاستهلاك

Workbench
  = executable architecture/reference consumer

Athar
  = complete Arabic reference product

Madar v0.10
  = operational case-management product
```

كل هذه الأسطح تمر ببوابات repository قوية، لكن **Production Approval يخص منتجًا وبيئة محددين**.

## ما يقدمه FoundationKit فعليًا

- 17 حزمة NuGet قابلة لإعادة الاستخدام + 17 symbol packages؛
- Domain/Application/Infrastructure/WebApi/Blazor؛
- Auditing/Security/Identity/Authorization/Workflow/Approvals؛
- Notifications + SMTP؛
- Settings/Feature Management/Localization/Caching؛
- capability graph، profiles، contract compatibility، maturity evidence؛
- Composer للتحقق والشرح، وليس project generation بعد؛
- CI/Security/CodeQL/package/integration gates.

Package existence لا يعني `Stable` maturity ولا Production Approval.

## دور Workbench

Workbench يثبت المسارات المعمارية والتكامل SQL/UI ويعطي runtime evidence لبعض capabilities. لا يُعامل كخدمة Production عامة، ولا ينبغي إضافة production identity إليه فقط كي يبدو مثل المنتج الحقيقي.

## دور Athar

أثَر يثبت منتجًا مرجعيًا كاملًا مع Identity، MFA، anti-CSRF، rate limiting، maker-checker، SQL Server، migrations، audit، notifications، Docker، E2E، readiness، وbackup/restore verification.

هذا يجعله reference قويًا، لكنه لا يختار مزودات Production الفعلية نيابة عن deployment.

## دور Madar

Madar v0.10 هو المنتج التشغيلي الحالي في المستودع. يغطي case lifecycle، الأقسام/التوجيه، SLA، التعليقات، الاعتمادات، الإشعارات، النقل/إعادة الإسناد، attachments، authorized search/reporting، SQL Server، Identity، Arabic Blazor، Docker وSQL/E2E.

لكن مسار Development/CI الحالي لا يساوي تلقائيًا production topology. Object storage/KMS/malware scanning، organization/tenancy، durable jobs/outbox، observability، ingress، backup operations وغيرها تُحسم عند deployment الحقيقي.

## بوابة الإطلاق الفعلي

### الأمن

- HTTPS إلزامي وشهادة موثوقة؛
- الأسرار في Vault/Secret Manager مناسب؛
- production bootstrap/seed مضبوط أو معطل حسب المنتج؛
- MFA/confirmed email/password recovery وفق السياسة المعتمدة؛
- CORS/Cookie/SameSite/AllowedHosts/reverse-proxy trust حسب topology؛
- SAST/dependency/secret/container scanning؛
- CSP/cache policy متوافقة ومختبرة مع Blazor إذا كانت مطلوبة؛
- penetration test بنطاق المنتج.

### البيانات

- SQL Server مُدار أو خطة تشغيل DBA واضحة؛
- TLS والتحقق من شهادة قاعدة البيانات؛
- runtime/migration principals بأقل صلاحية؛
- backup مشفر/off-site/immutable حسب السياسة واختبار restore دوري؛
- retention/deletion/export/PII decisions معتمدة؛
- migrations كخطوة نشر مضبوطة عند Production.

### التشغيل

- central logs مع Correlation ID؛
- metrics/tracing؛
- alerts + on-call ownership؛
- SLO/SLA حسب المنتج؛
- incident/rollback runbook؛
- realistic load/performance test؛
- WAF/reverse proxy/rate limiting على الحافة عند الحاجة؛
- release/rollback strategy.

### الحوكمة

قبل أول Production deployment/release-governed change يجب إغلاق متطلبات Issue #35، ومنها:

- protected `main` أو production release branch؛
- PR requirement؛
- reviewer مستقل واحد على الأقل؛
- required exact checks؛
- conversation resolution؛
- force-push/deletion restrictions؛
- break-glass path موثق؛
- evidence من PR حقيقي محكوم بهذه القواعد.

### المنتج والامتثال

- product owner acceptance؛
- approved roles/permissions؛
- privacy notice/terms عندما تنطبق؛
- product threat model؛
- متطلبات KYC/AML/PCI/sector controls حسب المجال؛
- accessibility/device/language acceptance؛
- residual-risk approval من الجهة المخولة.

## إطار .NET المدعوم

المستودع حاليًا يستهدف `net8.0` مع servicing packages المحدثة إلى خط `8.0.29`. هذا baseline مدعوم حاليًا، لكنه له نهاية دعم زمنية. الانتقال إلى .NET 10 LTS قبل انتهاء دعم .NET 8 مُسجل في Issue #104 ويجب ألا يُترك حتى يتحول إلى دين صامت.

## قرار الجاهزية

```text
Repository/Core gates pass
    +
Product E2E/security tests pass
    +
Production environment controls pass
    +
Recovery/observability/load acceptance pass
    +
Governance/product acceptance pass
    =
Production Approved for that deployment
```

فشل شرط deployment لا يعني أن FoundationKit Core مكسور؛ يعني أن المنتج أو البيئة لم يكملا بوابة الإطلاق بعد.
