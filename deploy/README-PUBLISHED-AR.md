# تشغيل حزمة أثَر التجريبية على Windows

هذه الحزمة ناتجة من GitHub Actions وتحتوي على نسخة منشورة من `Athar.Api` مع Blazor WebAssembly.

## المتطلبات

- Windows x64.
- .NET 10 Runtime.
- SQL Server يعمل محليًا.
- اتصال Windows Authentication ناجح إلى `Server=.`.

## التشغيل السريع

1. فك ضغط الحزمة كاملة.
2. تأكد أن البنية بقيت كما هي:

```text
athar-windows-x64/
├── app/
│   └── Athar.Api.exe
├── START-PUBLISHED-ATHAR.cmd
├── START-PUBLISHED-ATHAR.ps1
└── README-AR.md
```

3. شغّل:

```text
START-PUBLISHED-ATHAR.cmd
```

4. سيطلب منك كلمة مرور مؤقتة للمسؤول. استخدم كلمة قوية من 12 حرفًا أو أكثر وتحتوي على حروف كبيرة وصغيرة ورقم ورمز.
5. سيفتح أثَر على:

```text
http://localhost:5068
```

ويستخدم افتراضيًا:

```text
Server=.;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

سيطبق التطبيق EF Core migrations تلقائيًا عند أول تشغيل.

## بيانات المسؤول الافتراضية

```text
Email: admin@athar.local
Display name: مسؤول منصة أثَر
Password: الكلمة التي أدخلتها عند التشغيل
```

## استخدام اسم SQL Server مختلف

افتح PowerShell داخل مجلد الحزمة وشغّل:

```powershell
.\START-PUBLISHED-ATHAR.ps1 `
  -ConnectionString "Server=ALHASSANASUSROG;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True" `
  -AdminPassword "ضع-هنا-كلمة-قوية"
```

ولـSQL Express:

```powershell
.\START-PUBLISHED-ATHAR.ps1 `
  -ConnectionString "Server=.\SQLEXPRESS;Database=Athar;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True" `
  -AdminPassword "ضع-هنا-كلمة-قوية"
```

## الإيقاف

اضغط داخل نافذة التطبيق:

```text
Ctrl + C
```

لا تشغّل الحزمة من نافذتين في الوقت نفسه، لأن المنفذ وملفات التشغيل ستكون مستخدمة من العملية الأولى.

## حدود الحزمة

- هذه نسخة تجريبية وليست استضافة عامة.
- قاعدة البيانات موجودة على جهازك.
- لا تدخل بيانات عملاء أو بيانات حساسة.
- تشغيل GitHub Pages منفصل عن هذه النسخة ولا يحتوي Backend حقيقيًا.
