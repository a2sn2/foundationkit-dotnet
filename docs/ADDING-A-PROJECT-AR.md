# إضافة مشروع جديد باستخدام FoundationKit

المسار المفضل هو Composer لإنشاء الهيكل ثم تسجيل Project Identity والـmodules المطلوبة داخل Host مستقل.

كل مشروع جديد يملك قاعدة بياناته/DbContext/migrations/configuration/business managers/policies. FoundationKit يوفر العقود والمنطق المتكرر ولا يشارك runtime state بين المشاريع.

ابدأ من `docs/PROJECT-ISOLATION-AND-COMPATIBILITY.md` ثم `docs/CRUD-MODULE-ENGINE.md`، واستخدم Workbench كمرجع تنفيذي.
