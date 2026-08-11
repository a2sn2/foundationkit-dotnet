(() => {
    const translations = new Map([
        // Shared sample navigation / loading.
        ['نظرة عامة', 'Overview'],
        ['جاري تشغيل Core Studio', 'Starting Core Studio'],
        ['نجهّز Composer والعقود ونظام التصميم الموحد…', 'Preparing Composer, contracts and the unified design system…'],
        ['حدث خطأ غير متوقع.', 'An unexpected error occurred.'],
        ['إعادة التحميل', 'Reload'],

        // Studio explorer.
        ['تحديث الأدلة', 'Refresh evidence'],
        ['افهم ما يعلنه الكور، وما يصبح فعالًا، وما يصل إلى الـAPI.', 'Understand what the Core declares, what becomes effective, and what reaches the API.'],
        ['الصفحة تقرأ الكتالوج وتركيب الموديولات من Workbench. لا تنشئ صلاحيات ولا تخزن أسرارًا ولا تحول metadata العرض إلى business rules.', 'This page reads the catalog and module composition from Workbench. It creates no permissions, stores no secrets, and never turns presentation metadata into business rules.'],
        ['جاري تحميل تركيب الكور...', 'Loading Core composition…'],
        ['Declared مقابل Effective مع عقد الـAPI لكل Module.', 'Declared versus Effective with the API contract for each module.'],
        ['لا توجد module composition حية في هذا الوضع.', 'No live module composition is available in this mode.'],
        ['ابحث في القدرات المنشورة دون تغيير العقود.', 'Search published capabilities without changing contracts.'],
        ['ابحث بالاسم أو المعرّف...', 'Search by name or identifier…'],
        ['إعدادات العرض التي يثبتها Workbench', 'Presentation settings proved by Workbench'],
        ['لم يتم التحقق من Settings/Feature Management/Localization في هذا الوضع.', 'Settings, Feature Management and Localization were not verified in this mode.'],
        ['تعذر تحميل الكتالوج وتركيب الموديولات. شغّل Workbench API أو تحقق من الكتالوج الثابت.', 'Could not load the catalog and module composition. Start the Workbench API or verify the static catalog.'],

        // Visual Composer.
        ['تحقق', 'Validate'],
        ['جاري التوليد...', 'Generating…'],
        ['اختر أساس المشروع بصريًا، ثم ولّده محليًا بنفس Composer Engine.', 'Choose the project foundation visually, then generate it locally with the same Composer Engine.'],
        ['لا يوجد Generator داخل المتصفح. الواجهة تبني Manifest فقط، والتحقق والتوليد الفعليان يمران عبر', 'There is no generator inside the browser. The UI only builds a Manifest; validation and actual generation run through'],
        ['وCompositionAnalyzer وComposerProjectModelGenerator على السيرفر المحلي.', 'CompositionAnalyzer and ComposerProjectModelGenerator on the local server.'],
        ['اختيارات bounded لبناء schema-v2 starter قبل التعديل المتقدم في JSON.', 'Bounded choices build a schema-v2 starter before advanced JSON editing.'],
        ['طبّق الاختيارات على Manifest', 'Apply choices to Manifest'],
        ['للموديلات المتقدمة، عدّل JSON مباشرة لإضافة الحقول، search/sort/index intent، والـRead Models/Views ثم نفّذ التحقق قبل التوليد.', 'For advanced models, edit JSON directly to add fields, search/sort/index intent and Read Models/Views, then validate before generation.'],
        ['هذا هو العقد الذي يستهلكه Composer؛ يمكنك تعديله يدويًا قبل Validate/Generate.', 'This is the contract consumed by Composer; you can edit it manually before Validate/Generate.'],
        ['نتيجة Composer الفعلية', 'Actual Composer result'],
        ['ComposerManifestParser + CompositionAnalyzer يعملان...', 'ComposerManifestParser + CompositionAnalyzer are running…'],
        ['اضغط «تحقق» قبل التوليد.', 'Select Validate before generation.'],
        ['لا توجد maturity warnings في هذا التحليل.', 'There are no maturity warnings in this analysis.'],
        ['Generate إلى جهازك بدون Path حر من المتصفح', 'Generate to your machine without a browser-controlled free path'],
        ['المخرج مقيد دائمًا إلى', 'Output is always constrained to'],
        ['داخل نسخة FoundationKit المحلية.', 'inside the local FoundationKit checkout.'],
        ['إذا كان المجلد موجودًا فلن يُستبدل افتراضيًا.', 'If the folder already exists, it is not replaced by default.'],
        ['يتم الآن إنشاء Solution والمشاريع والـSQL/API artifacts...', 'Creating the Solution, projects and SQL/API artifacts…'],
        ['تم توليد', 'Generated'],
        ['بنجاح في', 'successfully in'],
        ['تعذر استدعاء Composer validation endpoint.', 'Could not call the Composer validation endpoint.'],

        // Evidence.
        ['كل طبقة لها دليلها، ولا نخلط نجاح الـBuild مع Production approval.', 'Every layer has its own evidence; build success is not confused with production approval.'],
        ['FoundationKit يثبت العقود والتنفيذ داخل المستودع. حماية main والمراجعة المستقلة وإجراءات التشغيل الفعلية تبقى متطلبات حوكمة منفصلة قبل أي Go‑Live حقيقي.', 'FoundationKit proves contracts and implementation inside the repository. Protected main, independent review and real operating procedures remain separate governance requirements before any real go-live.'],
        ['المراحل أدناه هي طبقات إثبات مستقلة؛ فشل إحداها لا يُخفى بنجاح الأخرى.', 'The stages below are independent evidence layers; failure in one is not hidden by success in another.'],
        ['ما الذي لا تدّعيه هذه الواجهة؟', 'What does this interface not claim?'],
        ['توليد deterministic مع force-regeneration وحماية ملكية الملفات.', 'Deterministic generation with force-regeneration and generated-file ownership protection.'],
        ['API generated projects تعمل على SQL Server مع isolation وmigrations product-owned.', 'Generated API projects run on SQL Server with isolation and product-owned migrations.'],
        ['WHERE/ORDER BY/paging/indexes وSQL Views تُثبت على provider حقيقي.', 'WHERE/ORDER BY/paging/indexes and SQL Views are proved against a real provider.'],
        ['Runtime OpenAPI يُشتق منه Postman والـTyped Client deterministically.', 'Postman and the Typed Client are deterministically derived from Runtime OpenAPI.'],
        ['Build/tests/security scans/CodeQL/Windows/package count تبقى بوابات مستقلة.', 'Build, tests, security scans, CodeQL, Windows and package count remain independent gates.'],
        ['UI ليست Authorization Boundary', 'UI is not an Authorization Boundary'],
        ['إخفاء زر أو صفحة لا يمنح ولا يسحب صلاحية. القرار الأمني يبقى داخل الـAPI والسياسات الخلفية.', 'Hiding a button or page neither grants nor revokes permission. The security decision stays in the API and backend policies.'],
        ['UI لا تنفذ Joins', 'UI does not execute joins'],
        ['الشاشات متعددة الجداول والتقارير تستهلك Read Models/Views بدل نسخ منطق الربط إلى المتصفح.', 'Multi-table and report screens consume Read Models/Views instead of copying join logic into the browser.'],
        ['Green CI ≠ Production Approved', 'Green CI ≠ Production Approved'],
        ['نجاح المستودع دليل هندسي، لكنه لا يثبت protected main أو المراجعة البشرية المستقلة أو إجراءات التشغيل الفعلية.', 'Repository success is engineering evidence; it does not prove protected main, independent human review, or real operating procedures.'],
        ['لا أسرار في الواجهة', 'No secrets in the UI'],
        ['Composer metadata والكتالوج والعقود يجب أن تبقى bounded وغير سرية؛ credentials والسياسات الحساسة لا تُولد داخل client assets.', 'Composer metadata, catalog data and contracts must stay bounded and non-secret; credentials and sensitive policies are never generated into client assets.'],

        // Design System.
        ['نظام تصميم خفيف، لطيف، وقابل للتوليد.', 'A light, gentle and generatable design system.'],
        ['هذه الصفحة تعرض المكونات الحقيقية من FoundationKit.Blazor. نفس الـTokens والمكونات هي التي يستهلكها Core Studio والـGenerated Apps؛ ليست Mockups منفصلة.', 'This page renders real FoundationKit.Blazor components. Core Studio and Generated Apps consume the same tokens and components; these are not separate mockups.'],
        ['مساحات مريحة، سطوح هادئة، حركة صغيرة ذات معنى، وOrbit Nodes تربط بصريًا بين Project وAPI وSQL وUI.', 'Comfortable spacing, calm surfaces, small meaningful motion, and Orbit Nodes that visually connect Project, API, SQL and UI.'],
        ['خفيف · واضح · ذكي · ودود', 'Light · clear · smart · friendly'],
        ['Neon، gaming، heavy glass، black dashboard، أو generic admin template.', 'No neon, gaming, heavy glass, black-dashboard aesthetic or generic admin template.'],
        ['المكونات لا تعرف “بنفسجي” أو “رمادي 3”. تعرف Primary وCanvas وSurface وText وBorder.', 'Components do not know “purple” or “gray 3”. They know Primary, Canvas, Surface, Text and Border.'],
        ['Arabic-first stack مع Latin stack مستقل، وأوزان محدودة تمنع الواجهة من أن تصبح ثقيلة.', 'An Arabic-first stack with an independent Latin stack and restrained weights that keep the interface light.'],
        ['نبني الأساس مرة واحدة.', 'Build the foundation once.'],
        ['واجهة جاهزة للمشروع الحقيقي.', 'An interface ready for a real product.'],
        ['Read Models وعقود واضحة.', 'Read Models and clear contracts.'],
        ['النص الأساسي مريح للقراءة ولا يعتمد على أحجام صغيرة أو تباين ضعيف.', 'Body text stays comfortable to read without relying on tiny sizes or weak contrast.'],
        ['كل المسافات والقيم الهندسية تأتي من Scale محدد، لا أرقام عشوائية بين شاشة وأخرى.', 'Spacing and geometry come from a defined scale, not random values from one screen to another.'],
        ['الحالات التالية هي نفس المكونات العامة، وليست CSS demo منفصلًا.', 'The following states use the same public components; they are not a separate CSS demo.'],
        ['حفظ', 'Save'], ['معاينة', 'Preview'], ['إلغاء', 'Cancel'], ['حذف', 'Delete'],
        ['هادئة افتراضيًا', 'Calm by default'], ['الحدود والسطح يصنعان hierarchy قبل الظلال.', 'Borders and surface hierarchy do the work before shadows.'],
        ['للمحتوى الثانوي', 'For secondary content'], ['سطح أخف بدون elevation غير ضروري.', 'A lighter surface without unnecessary elevation.'],
        ['يتحرك فقط لأنه قابل للتفاعل', 'It moves only because it is interactive'], ['Hover بسيط: -2px وحدود Primary Soft.', 'A subtle hover: -2px with Primary Soft borders.'],
        ['وظيفة النظام أهم من الزخرفة؛ Labels واضحة، Focus مرئي، Error لا يعتمد على اللون وحده.', 'System function matters more than decoration: clear labels, visible focus and errors that do not rely on color alone.'],
        ['اسم المشروع', 'Project name'], ['⚠ استخدم مسارًا bounded بدون فراغات.', '⚠ Use a bounded route without spaces.'], ['حفظ الإعدادات', 'Save settings'],
        ['ابحث في العملاء…', 'Search customers…'], ['الاسم', 'Name'], ['الحالة', 'Status'], ['آخر تحديث', 'Last updated'], ['اليوم', 'Today'], ['أمس', 'Yesterday'], ['قبل 3 أيام', '3 days ago'],
        ['Orbit Nodes تصبح اللغة البصرية الخاصة بالتوليد والانتظار والحالات الفارغة بدل رسومات مالية أو 3D ثقيل.', 'Orbit Nodes become the visual language for generation, waiting and empty states instead of finance imagery or heavy 3D.'],
        ['لا توجد عناصر بعد', 'No items yet'], ['ابدأ بإضافة أول عنصر؛ الواجهة تشرح الخطوة التالية بدل رسالة No data الجافة.', 'Add the first item; the interface explains the next step instead of showing a dry “No data” message.'], ['+ إضافة عنصر', '+ Add item'],
        ['نبني المسار', 'Building the path'], ['نحافظ على اللطافة بالانضباط؛ ليس بإضافة مؤثرات أكثر.', 'Gentleness comes from discipline, not from adding more effects.'],
        ['Neutral surfaces ومساحات تنفس.', 'Neutral surfaces and breathing room.'], ['Primary color للأهمية فقط.', 'Primary color only for importance.'], ['Motion قصير وله معنى.', 'Short, meaningful motion.'], ['Read Models للتقارير بدل joins في المتصفح.', 'Read Models for reports instead of browser joins.'], ['Responsive layout حقيقي، لا تكبير Mobile.', 'Real responsive layout, not an enlarged mobile screen.'],
        ['Glass وGradients في كل مكوّن.', 'Glass and gradients in every component.'], ['Shadow ثقيل أو Radius واحد لكل شيء.', 'Heavy shadows or one radius for everything.'], ['Business authorization داخل الـUI.', 'Business authorization inside the UI.'], ['نسخ شعار أو ألوان JAIB أو Wallet-specific patterns.', 'Copying JAIB branding/colors or wallet-specific patterns.']
    ]);

    const originals = new WeakMap();
    let applying = false;

    function currentLanguage() {
        return window.FoundationKitLocale?.current?.() || (document.documentElement.lang === 'ar' ? 'ar' : 'en');
    }

    function translateTextNode(node, language) {
        if (!node?.nodeValue || !node.parentElement) return;
        if (node.parentElement.closest('script,style,code,pre')) return;

        let original = originals.get(node);
        const current = node.nodeValue;
        const trimmed = current.trim();

        if (!original && translations.has(trimmed)) {
            original = current;
            originals.set(node, original);
        }
        if (!original) return;

        if (language === 'ar') {
            node.nodeValue = original;
            return;
        }

        const source = original.trim();
        const translated = translations.get(source);
        if (!translated) return;
        const leading = original.match(/^\s*/)?.[0] || '';
        const trailing = original.match(/\s*$/)?.[0] || '';
        node.nodeValue = `${leading}${translated}${trailing}`;
    }

    function translateAttributes(element, language) {
        if (!(element instanceof Element)) return;
        for (const attribute of ['placeholder', 'aria-label', 'title']) {
            const current = element.getAttribute(attribute);
            if (!current) continue;
            const key = `fkOriginal${attribute.replace(/-([a-z])/g, (_, char) => char.toUpperCase()).replace(/^./, char => char.toUpperCase())}`;
            let original = element.dataset[key];
            if (!original && translations.has(current.trim())) {
                original = current;
                element.dataset[key] = current;
            }
            if (!original) continue;
            element.setAttribute(attribute, language === 'ar' ? original : (translations.get(original.trim()) || original));
        }
    }

    function translateTree(root, language = currentLanguage()) {
        if (!root) return;
        applying = true;
        try {
            if (root.nodeType === Node.TEXT_NODE) {
                translateTextNode(root, language);
                return;
            }
            if (!(root instanceof Element) && root !== document.body) return;
            if (root instanceof Element) translateAttributes(root, language);

            const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
            while (walker.nextNode()) translateTextNode(walker.currentNode, language);
            if (root.querySelectorAll) root.querySelectorAll('*').forEach(element => translateAttributes(element, language));
        } finally {
            applying = false;
        }
    }

    document.addEventListener('foundationkit:languagechange', event => {
        translateTree(document.body, event.detail?.language === 'ar' ? 'ar' : 'en');
    });

    const observer = new MutationObserver(records => {
        if (applying) return;
        const language = currentLanguage();
        for (const record of records) {
            if (record.type === 'characterData') translateTree(record.target, language);
            for (const node of record.addedNodes) translateTree(node, language);
        }
    });

    observer.observe(document.body, { childList: true, subtree: true, characterData: true });
    translateTree(document.body, currentLanguage());
})();
