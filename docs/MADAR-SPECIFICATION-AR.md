# مواصفة منتج مدار — v0.10

## 1. الغرض من الوثيقة

هذه الوثيقة هي المواصفة الموحدة للحالة المنفذة حاليًا من **مدار** داخل `apps/Madar`. وهي تصف ما يملكه المنتج فعليًا في الكود والاختبارات وقاعدة البيانات وواجهات API وBlazor، ولا تضيف متطلبات مستقبلية على أنها منجزة.

مدار هو منتج لإدارة الحالات التشغيلية داخل مؤسسة، مبني فوق FoundationKit مع إبقاء منطق الحالات والأقسام وSLA والمرفقات والبحث والتقارير داخل المنتج نفسه.

## 2. حدود المنتج

مدار يملك:

- نموذج الحالة التشغيلية ودورة حياتها.
- الهوية والأدوار وصلاحيات المنتج.
- الأقسام والعضوية والتوجيه والإسناد وإعادة الإسناد والتحويل.
- التعليقات والموافقات والإشعارات التشغيلية.
- SLA وأدلة الخرق/التصعيد.
- المرفقات الخاصة بالحالات.
- البحث والتلخيص التشغيلي مع الحفاظ على نطاق الرؤية.
- SQL Server والمهاجرات الخاصة بمدار.
- API وواجهة Blazor العربية.
- سجل التدقيق الخاص بالمنتج.
- Docker والتشغيل المحلي وhealth/readiness.

لا يحول مدار هذه المفاهيم إلى حزم FoundationKit عامة لمجرد أنه يحتاجها.

## 3. الأدوار

| الدور | المسؤولية الحالية |
|---|---|
| `Requester` | إنشاء الحالات وقراءة الحالات التي أنشأها وفق نطاق الرؤية |
| `Operator` | قراءة الحالات المسموح بها، قراءة طوابير أقسامه، claim للحالات، والعمل على الإسناد المملوك له |
| `Supervisor` | قراءة أوسع، توجيه/إسناد/إعادة إسناد/تحويل، تقدم وإغلاق الحالات، تقييم SLA، واتخاذ قرارات الموافقة |
| `Administrator` | جميع صلاحيات مدار الحالية، بما فيها إدارة الأقسام وعضوية المشغلين |

طبقة Application هي المرجع النهائي للترخيص؛ إخفاء زر في الواجهة ليس بديلًا عن التحقق في الخادم.

## 4. دورة حياة الحالة

الدورة الحتمية الأساسية:

```text
new → assigned → in-progress → resolved → closed
```

التوجيه بين الأقسام ليس حالة Workflow مستقلة.

### التحويل

عند تحويل حالة نشطة إلى قسم آخر بواسطة جهة مخولة:

1. ينتقل `DepartmentId` إلى القسم الهدف النشط.
2. يزال الإسناد الحالي.
3. تعود الحالة إلى `new` في طابور القسم الهدف.
4. تبقى هوية المنشئ والمحتوى والتعليقات والموافقات والمرفقات وأدلة SLA والسجل السابق محفوظة.

### إعادة الإسناد

تغيّر المشغل المؤهل مع بقاء الحالة في سياقها التشغيلي الحالي وعدم مسح أدلة SLA السابقة.

## 5. المتطلبات الوظيفية المنفذة

### FR-01 — المصادقة والحسابات

- تسجيل دخول بالهوية الخاصة بمدار.
- Cookie authentication آمن.
- anti-CSRF لطلبات الكتابة.
- سياسة كلمة مرور وlockout.
- rate limiting لمسارات المصادقة والكتابة.

### FR-02 — إنشاء الحالات وقراءتها

- إنشاء حالة بعنوان ووصف ونوع وأولوية.
- حفظ منشئ الحالة ووقت الإنشاء.
- قراءة الحالة فقط إذا سمح نطاق الرؤية الحالي.

### FR-03 — الإسناد والتقدم

- إسناد الحالة لمشغل مؤهل.
- Claim من طابور القسم حسب الصلاحية والعضوية.
- الانتقال عبر دورة الحالة المسموحة فقط.
- رفض الانتقالات غير المعرفة أو غير المخولة.

### FR-04 — الأقسام والتوجيه

- أقسام فعالة/غير فعالة.
- رموز أقسام فريدة.
- عضوية Operator في الأقسام.
- طابور لكل قسم.
- route/transfer/reassignment ضمن حدود الصلاحيات الحالية.

### FR-05 — التعليقات

- تعليقات append-only مرتبطة بالحالة.
- لا تستخدم التعليقات لتجاوز صلاحية رؤية الحالة.

### FR-06 — الموافقات

- maker-checker للحالات الحساسة التي تتطلب قرارًا مستقلًا.
- قرارات approve/reject موثقة.
- منفذ الطلب لا يعتمد قراره الحساس بنفسه عندما تفرض السياسة الفصل.

### FR-07 — SLA

- هدف SLA محسوب حسب السياسة الحالية.
- تسجيل أول breach/escalation evidence.
- التقييم لا يمسح الأدلة التاريخية السابقة.
- قيم التطوير المحلية ليست سياسة Production تلقائية.

### FR-08 — الإشعارات

- إشعارات تشغيلية bounded/best-effort عبر عقود FoundationKit المناسبة.
- وجود SMTP adapter لا يعني وجود durable outbox أو ضمان تسليم Production.

### FR-09 — المرفقات

- metadata داخل SQL Server.
- المحتوى خلف `ICaseAttachmentContentStore` وليس public static files.
- storage key يولده الخادم.
- حد حالي 10 MiB.
- الأنواع الحالية: PDF وPNG وJPEG وTXT.
- فحص signature أساسي لتقليل type-confusion.
- صلاحية الحالة تطبق قبل list/upload/download.

### FR-10 — البحث والتقارير التشغيلية

- بحث SQL/EF داخل الحالات المسموح للمستخدم برؤيتها.
- filtering وpaging حتميان ومقيدان.
- summary counts تطبق بعد نطاق الرؤية، حتى لا تكشف الحالات المخفية عن طريق العدادات.
- لا يوجد external search index ولا BI engine ضمن v0.10.

### FR-11 — التدقيق

- تسجيل العمليات الحساسة والانتقالات والتوجيهات والقرارات ضمن سجل تدقيق المنتج.
- لا يفترض سجل التطبيق وحده أنه بديل عن central append-only security/audit sink في بيئة Production.

## 6. نموذج البيانات عالي المستوى

```text
Identity
├── Users
├── Roles
├── UserRoles
├── UserClaims
├── UserLogins
└── UserTokens

Madar
├── Cases
├── CaseComments
├── CaseApprovals
├── CaseAttachments
├── Departments
└── DepartmentMemberships

Audit
└── AuditEvents
```

ملفات EF Core migrations داخل `apps/Madar/Madar.Infrastructure/Migrations` هي مصدر حقيقة schema المنتج.

## 7. واجهات API الرئيسية

```text
GET  /health/live
GET  /health/ready
POST /api/auth/login
GET  /api/auth/me

GET/POST /api/cases
GET      /api/cases/search
GET      /api/cases/{caseId}
POST     /api/cases/{caseId}/assignment
POST     /api/cases/{caseId}/route
POST     /api/cases/{caseId}/transfer
POST     /api/cases/{caseId}/reassignment
POST     /api/cases/{caseId}/claim
POST     /api/cases/{caseId}/transition
GET      /api/cases/{caseId}/timeline
POST     /api/cases/sla/evaluate
GET/POST /api/cases/{caseId}/comments
GET/POST /api/cases/{caseId}/approvals
GET/POST /api/cases/{caseId}/attachments
GET      /api/departments/{departmentId}/queue
GET/POST /api/admin/departments
```

Swagger في Development هو المرجع التنفيذي لتفاصيل العقود الحالية.

## 8. واجهات UI الرئيسية

```text
/                    الصفحة الرئيسية
/login               تسجيل الدخول
/cases               مساحة الحالات والطوابير والإنشاء وSLA
/reports/cases       البحث المصرح والتلخيص التشغيلي
/cases/{CaseId:guid} تفاصيل الحالة والتعاون والمرفقات والموافقات والتدقيق
/admin/departments   إدارة الأقسام وعضوية المشغلين
```

الواجهة عربية وRTL، لكن الترخيص النهائي يبقى في الخادم.

## 9. المتطلبات غير الوظيفية الحالية

### الأمان

- عدم تشغيل container بصلاحية root.
- Cookie + anti-CSRF.
- rate limiting.
- authorization في Application layer.
- حدود حجم/نوع للمرفقات.
- secrets غير مخزنة في ملفات الإعدادات الملتزم بها.
- Security Scan وCodeQL وcontainer scanning ضمن CI.

### الاعتمادية

- liveness/readiness منفصلان.
- startup retry محدود لقاعدة البيانات.
- SQL-backed E2E في CI.
- Docker Compose مخصص للتطوير/CI وليس Production template.

### القابلية للصيانة

- Domain/Application/Infrastructure/API/Client boundaries واضحة.
- migrations مملوكة للمنتج.
- لا توجد dependency من Client إلى Infrastructure/DbContext.
- FoundationKit يعاد استخدامه فقط عندما يطابق عقدًا عامًا موجودًا.

## 10. التشغيل المحلي المعتمد

المسار المفضل على Windows:

```powershell
.\foundationkit.ps1 doctor
.\foundationkit.ps1 start -Target Madar -Mode Docker
.\scripts\madar-product.ps1 credentials
.\foundationkit.ps1 open -Target Madar
```

ثم:

```powershell
.\foundationkit.ps1 status -Target Madar
.\foundationkit.ps1 logs -Target Madar
.\foundationkit.ps1 stop -Target Madar
```

التفاصيل الكاملة في `docs/MADAR-LOCAL-RUN-PUBLISH-AR.md`.

## 11. Publish المحلي

```powershell
.\scripts\madar-product.ps1 publish
```

ينتج:

```text
artifacts/madar/publish/
artifacts/madar/Madar-net10.0-Release.zip
artifacts/madar/Madar-net10.0-Release.zip.sha256
```

هذا Release artifact تقني، وليس نشر Production تلقائيًا؛ بيئة الاستضافة الحقيقية ما زالت تحتاج SQL credentials وsecrets وTLS وstorage وobservability وسياسات تشغيل مناسبة.

## 12. معايير القبول قبل التسليم للمستخدم

- repository verification ينجح.
- Release build بلا أخطاء.
- جميع اختبارات FoundationKit/Workbench/Athar/Madar تنجح.
- Madar publish ينجح.
- Madar Docker يصل readiness.
- واجهة Blazor وSwagger/API assets تعمل.
- Madar E2E ينجح.
- department routing SQL workflow ينجح.
- Pages demo/assets validation ينجح.
- Security Scan وCodeQL ينجحان.
- لا تتغير حزم FoundationKit الـ17 ولا capability maturity بسبب هذه المرحلة.

## 13. خارج نطاق v0.10 الحالي

لا تدعي هذه المواصفة وجود:

- multi-tenancy Production مكتمل.
- شجرة مؤسسة نهائية أو HR/organization master data.
- object storage Production أو KMS أو malware scanning أو retention policy قانونية.
- external search engine.
- saved/scheduled/exported/BI reports.
- external-channel ingestion.
- durable outbox/background scheduler مكتمل.
- Production deployment approval أو شهادة امتثال خارجية.

هذه عناصر مستقبلية/بيئية ولا تُعامل كميزات منجزة حتى توجد أدلة تنفيذ واختبار مستقلة.
