# خطة تسليم مدار للتشغيل المحلي والنشر

> هذا الملف خطة تنفيذ مؤقتة لمرحلة التسليم المحلي. يُحذف أو يُدمج محتواه في الأدلة النهائية قبل اعتماد المرحلة.

## الهدف

تسليم Madar بحالة يكون فيها دور المستخدم النهائي هو تشغيل المنتج محليًا وتجربته، لا إصلاح المستودع أو تخمين الإعدادات.

## مخرجات المرحلة

- تشغيل محلي موحد عبر `foundationkit.ps1` وDocker.
- عرض آمن لبيانات حسابات التطوير المحلية عند الطلب.
- أمر Publish واضح ينتج Release artifact محليًا.
- مواصفة عربية موحدة للمنتج الحالي v0.10.
- دليل تشغيل وتجربة ونشر محلي خطوة بخطوة.
- GitHub Pages محدثة مع Demo ثابتة واضحة لمدار دون ادعاء أنها الـruntime الحقيقي.
- README وروابط الوثائق متزامنة مع الوضع الحالي.
- CI يتحقق من ملفات الـDemo ومن مسار Publish.
- لا تغيير business schema أو capability maturity أو عدد حزم FoundationKit.

## بوابات التسليم

1. Repository verification.
2. Release build/test.
3. Madar publish.
4. Madar Docker readiness + API/UI + E2E + routing SQL workflow.
5. Pages validation and JavaScript syntax.
6. Security/CodeQL.
7. توثيق الأوامر النهائية التي سيستخدمها المستخدم محليًا.
