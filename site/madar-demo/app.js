const baseCases = [
  {
    id: "MDR-1042",
    title: "طلب مراجعة عملية مالية",
    type: "مراجعة تشغيلية",
    department: "العمليات",
    priority: "high",
    status: "in-progress",
    sla: "42 دقيقة",
    risk: true,
    assigned: "مشغل العمليات",
    description: "حالة تجريبية توضح الإسناد والمتابعة وسجل النشاط داخل مدار."
  },
  {
    id: "MDR-1041",
    title: "استفسار يحتاج تحويلًا لقسم مختص",
    type: "خدمة داخلية",
    department: "الدعم",
    priority: "medium",
    status: "new",
    sla: "2 ساعة",
    risk: false,
    assigned: "غير مسند",
    description: "توضح هذه الحالة فكرة طابور القسم قبل claim أو الإسناد."
  },
  {
    id: "MDR-1038",
    title: "مراجعة مستند مرتبط بحالة",
    type: "توثيق",
    department: "التوثيق",
    priority: "critical",
    status: "assigned",
    sla: "18 دقيقة",
    risk: true,
    assigned: "مشغل التوثيق",
    description: "معاينة لمفهوم المرفقات الخاصة مع بقاء صلاحية الحالة هي الحاجز الأساسي للرؤية."
  },
  {
    id: "MDR-1034",
    title: "اعتماد إغلاق حالة حساسة",
    type: "موافقة",
    department: "الإشراف",
    priority: "medium",
    status: "resolved",
    sla: "مكتمل",
    risk: false,
    assigned: "مشرف العمليات",
    description: "مثال على حالة تمر ببوابة maker-checker قبل الإغلاق النهائي."
  }
];

const roleSelect = document.querySelector("#roleSelect");
const searchInput = document.querySelector("#searchInput");
const statusFilter = document.querySelector("#statusFilter");
const rows = document.querySelector("#caseRows");
const createDemo = document.querySelector("#createDemo");
const dialog = document.querySelector("#caseDialog");
const dialogTitle = document.querySelector("#dialogTitle");
const dialogDescription = document.querySelector("#dialogDescription");
const dialogDetails = document.querySelector("#dialogDetails");

let cases = [...baseCases];

const statusLabels = {
  "new": "جديدة",
  "assigned": "مسندة",
  "in-progress": "قيد التنفيذ",
  "resolved": "محلولة",
  "closed": "مغلقة"
};

const priorityLabels = {
  low: "منخفضة",
  medium: "متوسطة",
  high: "عالية",
  critical: "حرجة"
};

function visibleCases() {
  const role = roleSelect.value;
  const term = searchInput.value.trim().toLocaleLowerCase("ar");
  const status = statusFilter.value;

  return cases.filter((item, index) => {
    const roleVisible = role === "administrator" || index < 3;
    const textVisible = !term || `${item.title} ${item.type} ${item.department}`.toLocaleLowerCase("ar").includes(term);
    const statusVisible = status === "all" || item.status === status;
    return roleVisible && textVisible && statusVisible;
  });
}

function updateStats(items) {
  document.querySelector("#totalCount").textContent = items.length;
  document.querySelector("#newCount").textContent = items.filter(item => item.status === "new").length;
  document.querySelector("#activeCount").textContent = items.filter(item => ["assigned", "in-progress"].includes(item.status)).length;
  document.querySelector("#slaCount").textContent = items.filter(item => item.risk).length;
}

function rowHtml(item) {
  return `
    <tr data-case-id="${item.id}">
      <td class="case-title"><strong>${item.title}</strong><small>${item.id} · ${item.type}</small></td>
      <td>${item.department}</td>
      <td><span class="badge ${item.priority}">${priorityLabels[item.priority]}</span></td>
      <td><span class="badge ${item.status}">${statusLabels[item.status]}</span></td>
      <td class="${item.risk ? "sla-risk" : ""}">${item.sla}</td>
    </tr>`;
}

function render() {
  const items = visibleCases();
  rows.innerHTML = items.length
    ? items.map(rowHtml).join("")
    : `<tr><td colspan="5">لا توجد حالات مطابقة في هذه المعاينة.</td></tr>`;
  updateStats(items);

  document.querySelectorAll(".admin-only").forEach(element => {
    element.hidden = roleSelect.value !== "administrator";
  });

  rows.querySelectorAll("tr[data-case-id]").forEach(row => {
    row.addEventListener("click", () => openCase(row.dataset.caseId));
  });
}

function openCase(id) {
  const item = cases.find(candidate => candidate.id === id);
  if (!item) return;

  dialogTitle.textContent = item.title;
  dialogDescription.textContent = item.description;
  dialogDetails.innerHTML = `
    <div><span>رقم الحالة</span><strong>${item.id}</strong></div>
    <div><span>القسم</span><strong>${item.department}</strong></div>
    <div><span>الوضع</span><strong>${statusLabels[item.status]}</strong></div>
    <div><span>الأولوية</span><strong>${priorityLabels[item.priority]}</strong></div>
    <div><span>الإسناد</span><strong>${item.assigned}</strong></div>
    <div><span>SLA</span><strong>${item.sla}</strong></div>`;
  dialog.showModal();
}

function addDemoCase() {
  const next = cases.length + 1043;
  cases = [
    {
      id: `MDR-${next}`,
      title: "حالة جديدة داخل Demo",
      type: "تجربة واجهة",
      department: "العمليات",
      priority: "low",
      status: "new",
      sla: "3 ساعات",
      risk: false,
      assigned: "غير مسند",
      description: "هذه الحالة موجودة داخل ذاكرة الصفحة فقط وستختفي عند إعادة التحميل."
    },
    ...cases
  ];
  searchInput.value = "";
  statusFilter.value = "all";
  render();
}

roleSelect.addEventListener("change", render);
searchInput.addEventListener("input", render);
statusFilter.addEventListener("change", render);
createDemo.addEventListener("click", addDemoCase);

render();
