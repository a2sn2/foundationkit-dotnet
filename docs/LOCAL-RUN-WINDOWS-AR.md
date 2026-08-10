# تشغيل FoundationKit Workbench على Windows

استخدم المدير الموحد:

```powershell
.\foundationkit.ps1 start -Target Workbench
.\foundationkit.ps1 status -Target Workbench
.\foundationkit.ps1 logs -Target Workbench
.\foundationkit.ps1 stop -Target Workbench
```

أو شغل `samples/FoundationKit.Workbench/FoundationKit.Workbench.Api.csproj` مباشرة بعد ضبط `ConnectionStrings:Workbench` لقاعدة SQL Server محلية.

Workbench هو مرجع التنفيذ الحالي للـCore، وmigrations الخاصة به تبقى داخله ولا تنتقل إلى reusable packages.
