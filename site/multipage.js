(() => {
  const current = location.pathname.split('/').pop() || 'index.html';
  const nav = document.getElementById('site-nav');
  const navToggle = document.querySelector('.nav-toggle');
  const languageKey = 'foundationkit-language';

  document.querySelectorAll('.site-nav a[data-page]').forEach(link => {
    const target = link.getAttribute('href')?.split('#')[0] || '';
    link.classList.toggle('active', target === current);
    link.addEventListener('click', () => {
      nav?.classList.remove('open');
      navToggle?.setAttribute('aria-expanded', 'false');
    });
  });

  document.querySelectorAll('[data-year]').forEach(node => {
    node.textContent = String(new Date().getFullYear());
  });

  if (!document.querySelector('link[href="creative.css"]')) {
    const creative = document.createElement('link');
    creative.rel = 'stylesheet';
    creative.href = 'creative.css';
    document.head.appendChild(creative);
  }

  const commonArabic = new Map([
    // Shared shell and overview.
    ['Overview', 'نظرة عامة'], ['Architecture', 'المعمارية'], ['Capabilities', 'القدرات'], ['Packages', 'الحزم'],
    ['Frontend', 'الواجهة'], ['Quality', 'الجودة'], ['Start', 'ابدأ'], ['Developer', 'المطور'],
    ['Composable .NET Application Foundation', 'أساس .NET قابل للتركيب وإعادة الاستخدام'], ['Skip to content', 'انتقل إلى المحتوى'],
    ['Open navigation', 'فتح التنقل'], ['Start locally', 'ابدأ محليًا'], ['Explore Composer', 'استكشف Composer'],
    ['Reusable packages', 'حزم قابلة لإعادة الاستخدام'], ['Verified tests', 'اختبارات موثقة'],
    ['Roadmap end phase', 'نهاية خارطة الطريق'], ['Composition profiles', 'ملفات التركيب'],
    ['EXPLORE THE CORE', 'استكشف الكور'], ['One system. Clear rooms.', 'نظام واحد. مساحات واضحة.'],
    ['Getting Started', 'البدء'], ['Developer page', 'صفحة المطور'],
    ['Open architecture →', 'افتح المعمارية ←'], ['Explore capabilities →', 'استكشف القدرات ←'],
    ['Browse packages →', 'تصفح الحزم ←'], ['Enter Composer →', 'ادخل Composer ←'],
    ['See frontend system →', 'شاهد نظام الواجهة ←'], ['View evidence →', 'شاهد الأدلة ←'],
    ['Start first project →', 'ابدأ أول مشروع ←'], ['Open developer profile →', 'افتح ملف المطور ←'],
    ['Compose', 'تركيب'], ['Generate', 'توليد'], ['Persist', 'حفظ'], ['Read', 'قراءة'], ['Transport', 'نقل العقد'], ['Present', 'عرض'],
    ['Manifest intent', 'نية الـManifest'], ['Product solution', 'حل المشروع'], ['Entities + tables', 'Entities + Tables'],
    ['SQL views', 'SQL Views'], ['Soft Orbit UI', 'واجهة Soft Orbit'],

    // Architecture.
    ['ARCHITECTURE · CLEAN BOUNDARIES', 'المعمارية · حدود واضحة'],
    ['Reusable Core.', 'كور قابل لإعادة الاستخدام.'], ['Product-owned reality.', 'واقع يملكه المنتج.'],
    ['FoundationKit shares contracts and implementation primitives without owning each product’s database, migrations, credentials, deployment, business semantics or authorization policy.', 'يشارك FoundationKit العقود ولبنات التنفيذ دون أن يمتلك قاعدة بيانات كل منتج أو Migrations أو بيانات الاعتماد أو النشر أو دلالات العمل أو سياسة الصلاحيات.'],
    ['Architecture source ↗', 'مصدر المعمارية ↗'], ['Capabilities →', 'القدرات ←'],
    ['DEPENDENCY DIRECTION', 'اتجاه الاعتماديات'], ['Five explicit layers.', 'خمس طبقات صريحة.'],
    ['Domain', 'النطاق'],
    ['Entities, aggregate roots, value objects, domain events and exceptions. No framework dependency.', 'Entities وAggregate Roots وValue Objects وDomain Events والاستثناءات، بلا اعتماد على Framework.'],
    ['Application', 'التطبيق'],
    ['Results, validation, repositories/specifications, UoW, capability composition, modules, generic CRUD orchestration and reliability contracts.', 'النتائج والتحقق وRepositories/Specifications وUoW وتركيب القدرات والموديولات وتنسيق CRUD العام وعقود الاعتمادية.'],
    ['Infrastructure', 'البنية التحتية'],
    ['Provider-neutral EF adapters, relational idempotency, domain-event dispatch and model-composition helpers.', 'محولات EF محايدة للمزوّد، وIdempotency علائقي، وإرسال Domain Events، ومساعدات تركيب الموديل.'],
    ['Problem Details, correlation, generic endpoints, request-pipeline helpers, concurrency/idempotency and runtime contract metadata.', 'Problem Details وCorrelation وEndpoints عامة ومساعدات Request Pipeline وConcurrency/Idempotency وRuntime Contract Metadata.'],
    ['Typed API/error presentation state plus the reusable Soft Orbit design system, without server persistence ownership.', 'حالة عرض Typed للـAPI والأخطاء مع نظام Soft Orbit القابل لإعادة الاستخدام، دون امتلاك تخزين السيرفر.'],
    ['Boundary rules', 'قواعد الحدود'],
    ['Lower layers do not depend upward', 'الطبقات الأدنى لا تعتمد إلى الأعلى'], ['Convenience never overrides dependency direction.', 'سهولة الاستخدام لا تتجاوز اتجاه الاعتماديات.'],
    ['Products own provider choices', 'المنتجات تملك اختيارات المزوّد'], ['Core does not silently choose deployment databases for every consumer.', 'الكور لا يختار قواعد بيانات النشر بصمت لكل مستهلك.'],
    ['Writes stay on entities/tables', 'الكتابة تبقى على Entities/Tables'], ['Commands mutate authoritative write models.', 'Commands تعدّل نماذج الكتابة المرجعية.'],
    ['Complex reads use SQL views', 'القراءات المعقدة تستخدم SQL Views'], ['Multi-table/report reads are explicit read models rather than browser joins.', 'قراءات الجداول المتعددة والتقارير هي Read Models صريحة بدل Joins داخل المتصفح.'],
    ['Authorization is server-authoritative', 'السيرفر هو المرجع الحاكم للصلاحيات'], ['UI visibility is presentation, not a security boundary.', 'إظهار العناصر في الواجهة مجرد عرض وليس حدًا أمنيًا.'],
    ['Generation fails closed', 'التوليد يفشل بشكل مغلق وآمن'], ['Unsupported shapes are rejected instead of silently generating ambiguous code.', 'الأشكال غير المدعومة تُرفض بدل توليد كود ملتبس بصمت.'],
    ['End-to-end contract chain', 'سلسلة العقد من البداية للنهاية'], ['Manifest', 'Manifest'], ['Project intent', 'نية المشروع'], ['Generator', 'المولّد'], ['Deterministic output', 'مخرجات حتمية'], ['Runtime', 'Runtime'], ['SQL + API', 'SQL + API'], ['OpenAPI', 'OpenAPI'], ['Transport SSOT', 'مصدر عقد النقل'], ['Typed Client', 'Typed Client'], ['Generated C#', 'C# مولّد'], ['Blazor', 'Blazor'], ['Shared UI', 'واجهة مشتركة'],
    ['Isolation is architectural, not cosmetic', 'العزل معماري وليس تجميليًا'],
    ['FoundationKit supports many products from one reusable baseline while runtime state remains product-owned. No cross-project data, provider configuration, migrations, secrets or product roles should bleed into another generated system.', 'يدعم FoundationKit منتجات متعددة من أساس واحد قابل لإعادة الاستخدام، بينما تبقى Runtime State ملكًا للمنتج. لا يجب أن تتسرب بيانات أو إعدادات مزوّد أو Migrations أو أسرار أو أدوار من مشروع إلى نظام مولّد آخر.'],
    ['Host-owned runtime state', 'Runtime State يملكها الـHost'], ['Project-owned migrations', 'Migrations يملكها المشروع'], ['Core-owned reusable contracts', 'العقود القابلة لإعادة الاستخدام يملكها الكور'],
    ['On this page', 'في هذه الصفحة'], ['Layers', 'الطبقات'], ['Contract chain', 'سلسلة العقد'], ['Isolation', 'العزل'],

    // Capabilities.
    ['CAPABILITIES · ONE COMPOSABLE CORE', 'القدرات · كور واحد قابل للتركيب'], ['Small contracts.', 'عقود صغيرة.'], ['Serious systems.', 'أنظمة جدية.'],
    ['FoundationKit focuses on reusable system capabilities instead of product-specific features: application orchestration, CRUD/API generation, SQL-first reads, authorization, workflow, approvals, notifications, settings, caching, localization and reliability.', 'يركز FoundationKit على قدرات نظام قابلة لإعادة الاستخدام بدل ميزات خاصة بمنتج واحد: تنسيق التطبيق، وتوليد CRUD/API، والقراءات SQL-first، والصلاحيات، وسير العمل، والموافقات، والإشعارات، والإعدادات، والتخزين المؤقت، والتوطين والاعتمادية.'],
    ['Composer profiles', 'Composer Profiles'], ['Phase 12 closure tracks', 'مسارات إغلاق Phase 12'],
    ['MODULE / CRUD / API ENGINE', 'محرك MODULE / CRUD / API'], ['Configuration-first without hidden magic.', 'Configuration-first بدون سحر مخفي.'],
    ['Generic CRUD orchestration', 'تنسيق CRUD عام'], ['Application-level CRUD behaviors are composed through explicit contracts and resource configuration.', 'يتم تركيب سلوكيات CRUD على مستوى التطبيق من خلال عقود صريحة وإعدادات Resource.'],
    ['Generated API surface', 'سطح API مولّد'], ['HTTP endpoints are emitted from the same model and remain aligned with runtime contract metadata.', 'يتم توليد HTTP Endpoints من نفس الموديل وتبقى متوافقة مع Runtime Contract Metadata.'],
    ['Validation + Problem Details', 'Validation + Problem Details'], ['Failures are shaped consistently instead of leaking arbitrary exception formats.', 'تُصاغ حالات الفشل بشكل موحد بدل تسريب صيغ Exceptions عشوائية.'],
    ['Auditing hooks', 'نقاط ربط للتدقيق'], ['CRUD events can emit bounded audit intent without hard-coding a product audit store.', 'يمكن لأحداث CRUD إصدار Audit Intent محدود دون ربط Store تدقيق خاص بمنتج داخل الكور.'],
    ['SQL-FIRST READS', 'قراءات SQL-FIRST'], ['Complex queries belong on the server.', 'الاستعلامات المعقدة مكانها السيرفر.'],
    ['Writes remain entity/table based. Multi-table responses, reports and statements default to dedicated SQL View-backed Read Models with thin services and server-side filter, sort, count and paging.', 'تبقى عمليات الكتابة مبنية على Entity/Table. أما استجابات الجداول المتعددة والتقارير والكشوف فتعتمد افتراضيًا على Read Models مخصصة مدعومة بـSQL Views مع خدمات رقيقة وFilter/Sort/Count/Paging على السيرفر.'],
    ['Read model store', 'مخزن Read Model'], ['View-backed mappings', 'Mappings مدعومة بـViews'], ['Keyless EF mappings and generated view DDL keep reporting shapes explicit.', 'تحافظ Keyless EF Mappings وView DDL المولدة على وضوح أشكال التقارير.'],
    ['No browser joins', 'لا Joins داخل المتصفح'], ['Frontend code consumes server endpoints instead of recreating relational logic.', 'كود الواجهة يستهلك Server Endpoints بدل إعادة بناء المنطق العلائقي.'],
    ['Simple reads stay simple', 'القراءات البسيطة تبقى بسيطة'], ['Single aggregate GetById can continue through the normal repository path.', 'يمكن لـGetById الخاص بـAggregate واحد الاستمرار عبر مسار Repository المعتاد.'],
    ['PROJECT ISOLATION + RELIABILITY', 'عزل المشاريع + الاعتمادية'], ['Safe retries and isolated products.', 'إعادات محاولة آمنة ومنتجات معزولة.'],
    ['Idempotency', 'Idempotency'], ['Bounded contracts plus relational adapter prevent duplicate effects for supported request flows.', 'عقود محدودة مع Relational Adapter تمنع تكرار الأثر في تدفقات الطلبات المدعومة.'],
    ['Concurrency / ETag', 'Concurrency / ETag'], ['Optimistic concurrency behavior is explicit at the HTTP/resource boundary.', 'سلوك Optimistic Concurrency صريح عند حدود HTTP/Resource.'],
    ['Correlation', 'Correlation'], ['Requests carry traceable correlation context across the API pipeline.', 'تحمل الطلبات سياق Correlation قابلًا للتتبع عبر API Pipeline.'],
    ['Per-product state', 'حالة مستقلة لكل منتج'], ['Each generated host owns its provider, schema, migrations, secrets and deployment.', 'كل Host مولّد يملك المزوّد وSchema وMigrations والأسرار والنشر الخاص به.'],
    ['SUPPORTING CAPABILITIES', 'القدرات الداعمة'], ['Composable, bounded, replaceable.', 'قابلة للتركيب، محدودة، وقابلة للاستبدال.'],
    ['Authorization', 'الصلاحيات'], ['Permissions, roles, grants, evaluator and ownership primitives.', 'Permissions وRoles وGrants وEvaluator ولبنات Ownership.'],
    ['Workflow + Approvals', 'Workflow + Approvals'], ['Deterministic transitions plus maker-checker approval gates.', 'انتقالات حتمية مع بوابات موافقة Maker-Checker.'],
    ['Notifications', 'الإشعارات'], ['Channel-neutral contracts with a narrow SMTP reference provider.', 'عقود محايدة للقناة مع SMTP Reference Provider ضيق.'],
    ['Settings + Feature Management', 'الإعدادات + إدارة الميزات'], ['Bounded configuration values and explicit feature decisions.', 'قيم إعداد محدودة وقرارات ميزات صريحة.'],
    ['Localization', 'التوطين'], ['Culture metadata, fallback, RTL/LTR directionality and time-zone identity.', 'Culture Metadata وFallback واتجاه RTL/LTR وهوية المنطقة الزمنية.'],
    ['Caching', 'التخزين المؤقت'], ['Byte-cache contracts with TTL/hit/miss/remove semantics and in-memory reference implementation.', 'عقود Byte-cache مع دلالات TTL/Hit/Miss/Remove وتنفيذ مرجعي داخل الذاكرة.'],

    // Package catalog.
    ['17 REUSABLE PACKAGES', '17 حزمة قابلة لإعادة الاستخدام'], ['Compact boundaries.', 'حدود مدمجة.'], ['No package sprawl.', 'بدون تضخم في الحزم.'],
    ['The verified baseline stays at exactly 17 reusable NuGet packages and 17 symbol packages. Package version, capability maturity and capability contract version remain separate concepts.', 'يبقى الخط الأساسي الموثق عند 17 حزمة NuGet قابلة لإعادة الاستخدام و17 حزمة Symbols بالضبط. إصدار الحزمة ونضج القدرة وإصدار عقد القدرة مفاهيم منفصلة.'],
    ['NuGet packages', 'حزم NuGet'], ['Symbol packages', 'حزم Symbols'], ['Base packages', 'حزم أساسية'],
    ['All · 17', 'الكل · 17'], ['Base · 5', 'أساسية · 5'], ['Optional / Reference · 12', 'اختيارية / مرجعية · 12'],
    ['Base · framework-free', 'أساسية · بلا Framework'], ['Entity, aggregate, value-object and domain-event primitives.', 'لبنات Entity وAggregate وValue Object وDomain Event.'],
    ['Base · orchestration', 'أساسية · تنسيق'], ['Results, validation, repositories, specifications, UoW, capability graph, project isolation, modules, CRUD and idempotency contracts.', 'Results وValidation وRepositories وSpecifications وUoW وCapability Graph وعزل المشاريع والموديولات وعقود CRUD وIdempotency.'],
    ['Base · provider-neutral EF', 'أساسية · EF محايد للمزوّد'], ['EF repository/UoW/event adapters, relational idempotency and Core model-composition helpers.', 'محولات EF للـRepository/UoW/Events، وRelational Idempotency، ومساعدات تركيب موديل الكور.'],
    ['Base · HTTP', 'أساسية · HTTP'], ['Problem Details, correlation, generic CRUD endpoints and request/reliability pipeline helpers.', 'Problem Details وCorrelation وCRUD Endpoints عامة ومساعدات Request/Reliability Pipeline.'],
    ['Base · frontend', 'أساسية · واجهة'], ['Typed transport/presentation state and the first-party Soft Orbit Razor design system.', 'Typed Transport/Presentation State ونظام Soft Orbit Razor من الطرف الأول.'],
    ['Reference', 'مرجعية'], ['Preview', 'معاينة'], ['Reference provider', 'مزوّد مرجعي'],
    ['Bounded audit event/context/sink contracts and CRUD audit observer.', 'عقود Audit Event/Context/Sink محدودة مع CRUD Audit Observer.'],
    ['Reverse-proxy, rate-partition and MFA-assurance conventions.', 'اتفاقيات Reverse Proxy وRate Partition وMFA Assurance.'],
    ['Account policy, security event, notification and step-up contracts without a universal user store.', 'عقود Account Policy وSecurity Event وNotification وStep-up دون User Store عالمي.'],
    ['Permissions, roles, grants, evaluator and ownership primitives; product roles remain host-owned.', 'لبنات Permissions وRoles وGrants وEvaluator وOwnership؛ وتبقى أدوار المنتج ملكًا للـHost.'],
    ['Deterministic transition definitions/resolution with bounded audit intent.', 'تعريف وحل انتقالات حتمية مع Audit Intent محدود.'],
    ['Approve/reject decisions, maker-checker gate, workflow resolution and audit intent.', 'قرارات قبول/رفض، وبوابة Maker-Checker، وحل Workflow وAudit Intent.'],
    ['Channel-neutral bounded message, sender and delivery-result contracts.', 'عقود Message/Sender/Delivery Result محدودة ومحايدة للقناة.'],
    ['Validated narrow SMTP transport adapter over the Notifications contract.', 'SMTP Transport Adapter ضيق ومتحقق فوق عقد Notifications.'],
    ['Bounded keys/scopes/values with deterministic source precedence.', 'Keys/Scopes/Values محدودة مع أولوية مصادر حتمية.'],
    ['Settings-backed Boolean feature decisions with explicit defaults and fail-closed invalid configuration.', 'قرارات Boolean Features مدعومة بالإعدادات مع Defaults صريحة وفشل مغلق عند إعداد غير صالح.'],
    ['Culture metadata, RTL/LTR directionality, fallback and bounded time-zone identity.', 'Culture Metadata واتجاه RTL/LTR وFallback وهوية منطقة زمنية محدودة.'],
    ['Byte-cache contracts, TTL/hit/miss/remove semantics and in-memory reference provider.', 'عقود Byte-cache ودلالات TTL/Hit/Miss/Remove ومزوّد مرجعي داخل الذاكرة.'],
    ['Package contracts →', 'عقود الحزم ←'], ['Exactly 17', 'بالضبط 17'], ['in the verified baseline.', 'في الخط الأساسي الموثق.'],

    // Composer.
    ['COMPOSER · SCHEMA V2', 'COMPOSER · SCHEMA V2'], ['Describe intent.', 'صف النية.'], ['Generate the product.', 'ولّد المنتج.'],
    ['The Visual Composer and CLI converge on one canonical parser, analyzer and generator. There is no second generation engine hiding behind the UI.', 'يلتقي Visual Composer وCLI على Parser وAnalyzer وGenerator مرجعية واحدة. لا يوجد محرك توليد ثانٍ مخفي خلف الواجهة.'],
    ['Run locally →', 'شغّل محليًا ←'], ['Schema source ↗', 'مصدر Schema ↗'], ['PROJECT MODEL', 'موديل المشروع'], ['One manifest describes the system shape.', 'Manifest واحد يصف شكل النظام.'],
    ['Seven canonical profiles', 'سبعة Profiles مرجعية'], ['Profiles expand through the canonical capability dependency graph. Resource behaviors do not create a parallel dependency system.', 'تتوسع Profiles عبر Capability Dependency Graph المرجعي. سلوكيات Resources لا تنشئ نظام اعتماديات موازيًا.'],
    ['Visual and CLI share the same engine', 'Visual وCLI يشتركان في نفس المحرك'], ['Choose', 'اختر'], ['Select project profile, modules, resources, ID shapes and behaviors.', 'اختر Profile المشروع والموديولات والموارد وأشكال ID والسلوكيات.'],
    ['Build manifest', 'ابنِ Manifest'], ['The UI writes the same schema understood by the canonical parser.', 'تكتب الواجهة نفس Schema التي يفهمها الـParser المرجعي.'],
    ['Validate', 'تحقق'], ['Unsupported or inconsistent shapes fail closed before generation.', 'الأشكال غير المدعومة أو غير المتسقة تفشل بشكل مغلق قبل التوليد.'],
    ['Output is deterministic and written into', 'المخرجات حتمية وتُكتب داخل'],
    ['Safe regeneration is part of the contract', 'إعادة التوليد الآمنة جزء من العقد'],
    ['Generated ownership and hashes protect the workspace. Regeneration does not blindly wipe files that no longer match expected generated ownership. The local Studio endpoint is constrained to the repository’s generated workspace instead of accepting arbitrary filesystem paths.', 'تحمي ملكية الملفات المولدة والـHashes مساحة العمل. إعادة التوليد لا تمسح عشوائيًا ملفات لم تعد تطابق الملكية المتوقعة، كما أن Studio Endpoint المحلي مقيد بمساحة generated داخل المستودع بدل قبول مسارات ملفات عشوائية.'],
    ['Deterministic output', 'مخرجات حتمية'], ['Owned-file protection', 'حماية ملكية الملفات'], ['Bounded local path', 'مسار محلي محدود'],
    ['Composer map', 'خريطة Composer'], ['Project model', 'موديل المشروع'], ['Profiles', 'Profiles'], ['Visual → generate', 'Visual ← توليد'], ['Regeneration safety', 'أمان إعادة التوليد'],

    // Frontend.
    ['FOUNDATIONKIT.BLAZOR · SOFT ORBIT', 'FOUNDATIONKIT.BLAZOR · SOFT ORBIT'], ['Light enough to breathe.', 'خفيف بما يكفي ليتنفس.'], ['Structured enough to scale.', 'منظم بما يكفي ليتوسع.'],
    ['Soft Orbit is the shared presentation language for Core Studio and generated Blazor applications: semantic tokens, reusable Razor components, true RTL/LTR, responsive layouts, real dark mode and purposeful motion.', 'Soft Orbit هو لغة العرض المشتركة بين Core Studio وتطبيقات Blazor المولدة: Semantic Tokens ومكونات Razor قابلة لإعادة الاستخدام وRTL/LTR حقيقي وتخطيطات متجاوبة ووضع داكن فعلي وحركة ذات معنى.'],
    ['Design system source ↗', 'مصدر نظام التصميم ↗'], ['DESIGN DNA', 'الحمض البصري'], ['Neutral first', 'الحياد أولًا'], ['Airy surfaces and restrained borders do the structural work; brand color marks importance rather than painting every component.', 'السطوح المريحة والحدود المنضبطة تبني الهيكل؛ لون الهوية يحدد الأهمية بدل طلاء كل مكوّن.'],
    ['Iris + Aqua accents', 'لمسات Iris + Aqua'], ['Primary Iris creates technology/creativity personality while Aqua supports activity and data emphasis.', 'Iris الأساسي يمنح شخصية تقنية/إبداعية، بينما يدعم Aqua النشاط وإبراز البيانات.'],
    ['Controlled radius', 'استدارة منضبطة'], ['Rounded geometry stays friendly without turning every control into a pill.', 'الهندسة المستديرة تبقى ودودة دون تحويل كل عنصر إلى Pill.'],
    ['Purposeful motion', 'حركة ذات معنى'], ['Hover, press, theme and state transitions communicate interaction rather than decorate it.', 'انتقالات Hover وPress والثيم والحالة تشرح التفاعل بدل أن تكون مجرد زينة.'],
    ['REUSABLE RAZOR LAYER', 'طبقة RAZOR قابلة لإعادة الاستخدام'], ['Generated apps consume FoundationKit components.', 'التطبيقات المولدة تستهلك مكونات FoundationKit.'],
    ['The reusable UI layer owns the visual language. Generated business UI should not couple itself directly to a third-party component library as the product contract.', 'طبقة الواجهة القابلة لإعادة الاستخدام تملك اللغة البصرية. لا ينبغي لواجهة المنتج المولدة أن تربط عقد المنتج مباشرة بمكتبة مكونات من طرف ثالث.'],
    ['FOUNDATIONS', 'الأساسات'], ['Theme, direction and state are first-class.', 'الثيم والاتجاه والحالة عناصر من الدرجة الأولى.'],
    ['Light / Dark', 'فاتح / داكن'], ['Semantic variables remap surfaces and text hierarchy without naive inversion or pure-black dependence.', 'تعيد Semantic Variables توزيع السطوح وهرمية النص دون عكس ساذج أو اعتماد على الأسود الخالص.'],
    ['RTL / LTR', 'RTL / LTR'], ['Direction belongs in the design system, with logical layout properties and isolated LTR technical values where needed.', 'الاتجاه جزء من نظام التصميم، باستخدام Logical Properties وعزل القيم التقنية LTR عند الحاجة.'],
    ['Responsive', 'متجاوب'], ['Desktop, tablet and mobile adapt navigation and density instead of stretching one layout.', 'سطح المكتب والتابلت والموبايل يكيّفون التنقل والكثافة بدل تمديد تخطيط واحد.'],
    ['Accessibility', 'إمكانية الوصول'], ['Focus visibility, reduced motion, contrast and non-color-only states remain part of the component contract.', 'وضوح Focus وتقليل الحركة والتباين والحالات التي لا تعتمد على اللون وحده تبقى جزءًا من عقد المكونات.'],
    ['PRESENTATION STATES', 'حالات العرض'], ['Every screen has more than “data”.', 'كل شاشة أكثر من مجرد «بيانات».'],
    ['maps API results into Idle, Loading, Ready, Empty and Error. The same state vocabulary is available to Core Studio and generated applications.', 'يحوّل نتائج API إلى Idle وLoading وReady وEmpty وError. نفس مفردات الحالة متاحة في Core Studio والتطبيقات المولدة.'],

    // Quality.
    ['ENGINEERING EVIDENCE', 'أدلة هندسية'], ['Green means proved.', 'الأخضر يعني مُثبتًا.'], ['Not production approved.', 'وليس معتمدًا للإنتاج.'],
    ['FoundationKit separates repository confidence from production governance. CI proves the tested baseline and deterministic contracts; production approval still requires the independent governance boundary.', 'يفصل FoundationKit ثقة المستودع عن حوكمة الإنتاج. يثبت CI الخط الأساسي المختبر والعقود الحتمية؛ أما اعتماد الإنتاج فما زال يتطلب حد الحوكمة المستقل.'],
    ['NuGet + symbols', 'NuGet + Symbols'], ['Exact-head proof families', 'عائلات إثبات Exact-head'], ['EXACT-HEAD QUALITY', 'جودة EXACT-HEAD'], ['Evidence belongs to the same commit.', 'الدليل يجب أن ينتمي لنفس Commit.'],
    ['A green result from another revision is not accepted as proof for a changed baseline. The closure work repeatedly validated all required gates against the exact PR head before merge.', 'نتيجة خضراء من Revision آخر لا تُقبل كدليل لخط أساسي متغير. أعمال الإغلاق تحققت مرارًا من جميع البوابات المطلوبة على PR Head نفسه قبل الدمج.'],
    ['Restore audit, build, tests, Workbench publish, package integrity, SQL/API/OpenAPI/Postman evidence.', 'Restore Audit وBuild واختبارات ونشر Workbench وسلامة الحزم وأدلة SQL/API/OpenAPI/Postman.'],
    ['Composer Generation', 'Composer Generation'], ['Deterministic generation and safe ownership/regeneration behavior.', 'توليد حتمي وسلوك آمن للملكية وإعادة التوليد.'],
    ['Composer Full-Stack SQL', 'Composer Full-Stack SQL'], ['A/B full-stack proof against SQL Server.', 'إثبات Full-stack من نوع A/B مقابل SQL Server.'],
    ['Typed Client Proof', 'إثبات Typed Client'], ['Runtime OpenAPI deterministically produces the expected typed C# transport.', 'ينتج Runtime OpenAPI بشكل حتمي Typed C# Transport المتوقع.'],
    ['Read Engine Proof', 'إثبات Read Engine'], ['SQL-side filter/sort/count/page and view-backed read behavior.', 'Filter/Sort/Count/Page داخل SQL وسلوك قراءة مدعوم بالـViews.'],
    ['Frontend Generation Proof', 'إثبات توليد الواجهة'], ['Generated Blazor restore, build and publish against the shared UI layer.', 'Restore وBuild وPublish لتطبيق Blazor المولد مقابل طبقة الواجهة المشتركة.'],
    ['Security Scan', 'فحص الأمان'], ['Repository security checks and tracked-secret scanning.', 'فحوص أمان المستودع ومسح الأسرار المتتبعة.'],
    ['CodeQL', 'CodeQL'], ['C# and JavaScript static analysis.', 'تحليل ساكن لـC# وJavaScript.'],
    ['Windows Manager Check', 'فحص Windows Manager'], ['Windows launcher and local manager behavior stays validated.', 'يبقى سلوك Windows Launcher والـManager المحلي متحققًا منه.'],
    ['CONTRACT DRIFT', 'انحراف العقد'], ['Generated artifacts are checked, not trusted.', 'الـArtifacts المولدة تُفحص ولا تُفترض صحتها.'],
    ['OpenAPI is the serialized runtime transport source of truth. Generated Postman and typed clients are verified against it so stale documentation cannot quietly become a second contract.', 'OpenAPI هو مصدر الحقيقة المتسلسل لعقد النقل في Runtime. تتم مقارنة Postman وTyped Clients المولدة به حتى لا يتحول توثيق قديم بصمت إلى عقد ثانٍ.'],
    ['OpenAPI canonical', 'OpenAPI مرجعي'], ['Postman drift check', 'فحص انحراف Postman'], ['Typed client drift check', 'فحص انحراف Typed Client'],
    ['PRODUCTION BOUNDARY', 'حد الإنتاج'], ['Repository Complete ≠ Production Approved', 'اكتمال المستودع ≠ اعتماد الإنتاج'],
    ['The reusable FoundationKit Core is complete enough to start real products. Production go-live still requires governance such as protected', 'الكور القابل لإعادة الاستخدام في FoundationKit مكتمل بما يكفي لبدء منتجات حقيقية. لكن Go-live للإنتاج لا يزال يتطلب حوكمة مثل حماية'],
    ['and independent review before release. Green CI alone is deliberately not treated as production approval.', 'ومراجعة مستقلة قبل الإصدار. CI الأخضر وحده لا يُعامل عمدًا كاعتماد للإنتاج.'],
    ['Production governance issue →', 'مهمة حوكمة الإنتاج ←'],

    // Getting started.
    ['START THE FIRST PROJECT', 'ابدأ أول مشروع'], ['Clone. Choose.', 'استنسخ. اختر.'], ['Generate locally.', 'ولّد محليًا.'],
    ['The intended local workflow is visual-first: run Core Studio on your machine, compose the project in the browser, validate through the canonical Composer engine and generate into', 'سير العمل المحلي المقصود Visual-first: شغّل Core Studio على جهازك، ركّب المشروع في المتصفح، تحقق عبر محرك Composer المرجعي ثم ولّد داخل'],
    ['Open repository ↗', 'افتح المستودع ↗'], ['Understand Composer', 'افهم Composer'], ['LOCAL REQUIREMENTS', 'متطلبات التشغيل المحلي'], ['Keep the setup boring.', 'اجعل الإعداد بسيطًا ومملًا.'],
    ['Docker Desktop', 'Docker Desktop'], ['Runs the local Workbench/Core Studio stack and SQL Server boundary.', 'يشغّل Workbench/Core Studio محليًا وحد SQL Server.'],
    ['.NET 10 SDK', '.NET 10 SDK'], ['Builds and opens the generated solution on the host machine.', 'يبني ويفتح الحل المولد على جهاز الـHost.'],
    ['PowerShell', 'PowerShell'], ['Runs the repository launcher on Windows.', 'يشغّل Launcher المستودع على Windows.'],
    ['Git', 'Git'], ['Recommended for cloning and updating the reusable Core baseline.', 'موصى به لاستنساخ وتحديث خط الكور القابل لإعادة الاستخدام.'],
    ['1. Clone FoundationKit', '1. استنسخ FoundationKit'], ['Copy', 'نسخ'], ['2. Start Core Studio', '2. شغّل Core Studio'],
    ['The launcher brings up the local stack and opens the Workbench at', 'يشغّل Launcher الـStack المحلي ويفتح Workbench على'], ['The Visual Composer is available at', 'يتوفر Visual Composer على'],
    ['3. Choose → Validate → Generate', '3. اختر ← تحقق ← ولّد'], ['Choose project settings', 'اختر إعدادات المشروع'], ['Project/profile, modules/resources, ID shapes and supported behaviors.', 'Project/Profile وModules/Resources وأشكال ID والسلوكيات المدعومة.'],
    ['Build the manifest', 'ابنِ Manifest'], ['The visual form writes the canonical Composer schema.', 'النموذج البصري يكتب Composer Schema المرجعية.'], ['The same parser/analyzer used by CLI validation checks the model.', 'نفس Parser/Analyzer المستخدم في تحقق CLI يفحص الموديل.'],
    ['Generate Project', 'ولّد المشروع'], ['Output is created inside', 'يتم إنشاء المخرجات داخل'], ['4. Open the generated solution', '4. افتح الحل المولد'],
    ['During local development the generated solution intentionally keeps project references to the local FoundationKit Core, so the generated product and reusable foundation can be exercised together.', 'أثناء التطوير المحلي يحتفظ الحل المولد عمدًا بـProject References إلى FoundationKit Core المحلي، حتى يمكن تشغيل المنتج المولد والأساس القابل لإعادة الاستخدام معًا.'],
    ['Local generation is bounded', 'التوليد المحلي محدود'], ['The browser does not receive arbitrary filesystem write access. Generation is constrained to the repository-owned', 'المتصفح لا يحصل على صلاحية كتابة عشوائية في File System. التوليد مقيد بمساحة'],
    ['workspace, while Core source is not exposed as an unrestricted writable area to the local UI.', 'التي يملكها المستودع، بينما لا يتم كشف مصدر الكور كمساحة قابلة للكتابة بلا قيود للواجهة المحلية.'],
    ['Localhost-bound', 'مقيد بـLocalhost'], ['Generated workspace only', 'مساحة generated فقط'], ['Ownership/hash safety', 'أمان الملكية/Hash'],
    ['Requirements', 'المتطلبات'], ['Clone', 'الاستنساخ'], ['Start Studio', 'تشغيل Studio'], ['Open solution', 'فتح الحل'], ['Safety', 'الأمان']
  ]);

  function normalizeLanguage(value) {
    return String(value || '').toLowerCase() === 'ar' ? 'ar' : 'en';
  }

  function readLanguage() {
    try {
      return normalizeLanguage(localStorage.getItem(languageKey) || document.documentElement.lang || 'en');
    } catch {
      return normalizeLanguage(document.documentElement.lang || 'en');
    }
  }

  function persistLanguage(language) {
    try { localStorage.setItem(languageKey, language); } catch { /* best effort */ }
  }

  function captureOriginalText() {
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, {
      acceptNode(node) {
        if (!node.nodeValue?.trim()) return NodeFilter.FILTER_REJECT;
        const parent = node.parentElement;
        if (!parent || ['SCRIPT', 'STYLE', 'CODE', 'PRE'].includes(parent.tagName)) return NodeFilter.FILTER_REJECT;
        if (parent.closest('[data-en][data-ar]')) return NodeFilter.FILTER_REJECT;
        return NodeFilter.FILTER_ACCEPT;
      }
    });
    const nodes = [];
    while (walker.nextNode()) {
      const node = walker.currentNode;
      node.__foundationOriginalText = node.nodeValue;
      nodes.push(node);
    }
    return nodes;
  }

  const commonTextNodes = captureOriginalText();

  function translateCommon(language) {
    commonTextNodes.forEach(node => {
      const original = node.__foundationOriginalText || node.nodeValue || '';
      if (language !== 'ar') {
        node.nodeValue = original;
        return;
      }
      const trimmed = original.trim();
      const translated = commonArabic.get(trimmed);
      if (!translated) {
        node.nodeValue = original;
        return;
      }
      const leading = original.match(/^\s*/)?.[0] || '';
      const trailing = original.match(/\s*$/)?.[0] || '';
      node.nodeValue = `${leading}${translated}${trailing}`;
    });
  }

  function applyLocalizedAttributes(language) {
    document.querySelectorAll('[data-en][data-ar]').forEach(element => {
      const value = language === 'ar' ? element.dataset.ar : element.dataset.en;
      if (typeof value === 'string') element.textContent = value;
    });
    document.querySelectorAll('[data-placeholder-en][data-placeholder-ar]').forEach(element => {
      element.setAttribute('placeholder', language === 'ar' ? element.dataset.placeholderAr : element.dataset.placeholderEn);
    });
    document.querySelectorAll('[data-aria-en][data-aria-ar]').forEach(element => {
      element.setAttribute('aria-label', language === 'ar' ? element.dataset.ariaAr : element.dataset.ariaEn);
    });
  }

  const topActions = document.querySelector('.top-actions');
  let languageButton = document.querySelector('[data-language-toggle]');
  if (!languageButton && topActions) {
    languageButton = document.createElement('button');
    languageButton.type = 'button';
    languageButton.className = 'language-toggle';
    languageButton.dataset.languageToggle = '';
    topActions.insertBefore(languageButton, topActions.firstChild);
  }

  function applyLanguage(language, persist = true) {
    const normalized = normalizeLanguage(language);
    document.documentElement.lang = normalized;
    document.documentElement.dir = normalized === 'ar' ? 'rtl' : 'ltr';
    document.documentElement.dataset.language = normalized;
    document.body.dataset.language = normalized;
    applyLocalizedAttributes(normalized);
    translateCommon(normalized);
    if (languageButton) {
      const targetArabic = normalized !== 'ar';
      languageButton.textContent = targetArabic ? 'ع' : 'EN';
      languageButton.title = targetArabic ? 'التبديل إلى العربية' : 'Switch to English';
      languageButton.setAttribute('aria-label', languageButton.title);
    }
    if (persist) persistLanguage(normalized);
    document.dispatchEvent(new CustomEvent('foundationkit:languagechange', { detail: { language: normalized } }));
    return normalized;
  }

  languageButton?.addEventListener('click', () => {
    applyLanguage(document.documentElement.lang === 'ar' ? 'en' : 'ar');
  });

  applyLanguage(readLanguage(), false);
})();
