const STORAGE_ITEMS = "campusFindItems";
const STORAGE_CLAIMS = "campusFindClaims";

const seedItems = [
  {id:"demo-1",type:"found",name:"Black wireless headphones",category:"Electronics",date:"2026-09-08",location:"Main library, first floor",description:"Black over-ear wireless headphones found near the study tables.",emoji:"🎧",demo:true},
  {id:"demo-2",type:"lost",name:"Blue student notebook",category:"Books & stationery",date:"2026-09-10",location:"Lecture Hall B",description:"A blue notebook with a plain cover and several lecture notes inside.",emoji:"📘",demo:true},
  {id:"demo-3",type:"found",name:"Set of silver keys",category:"Keys",date:"2026-09-11",location:"Student centre reception",description:"A small set of silver keys on a plain metal ring.",emoji:"🔑",demo:true},
  {id:"demo-4",type:"lost",name:"Cream canvas tote bag",category:"Bags",date:"2026-09-12",location:"Cafeteria",description:"Cream-coloured canvas tote bag with a small green design.",emoji:"👜",demo:true},
  {id:"demo-5",type:"found",name:"Water bottle",category:"Other",date:"2026-09-13",location:"Sports centre entrance",description:"Reusable purple water bottle handed in near the entrance.",emoji:"🧴",demo:true},
  {id:"demo-6",type:"lost",name:"Student access card",category:"Cards & IDs",date:"2026-09-14",location:"Computer lab",description:"A campus access card. The card number is not shown for privacy.",emoji:"💳",demo:true}
];

let items = load(STORAGE_ITEMS, seedItems);
let claims = load(STORAGE_CLAIMS, []);
let currentClaimItem = null;
let toastTimer;

function load(key, fallback) {
  try {
    const stored = localStorage.getItem(key);
    return stored ? JSON.parse(stored) : fallback;
  } catch {
    return fallback;
  }
}
function save() {
  try {
    localStorage.setItem(STORAGE_ITEMS, JSON.stringify(items));
    localStorage.setItem(STORAGE_CLAIMS, JSON.stringify(claims));
  } catch {
    showToast("Browser storage is unavailable. This demo may not save changes.");
  }
}
function escapeHTML(value) {
  return String(value ?? "").replace(/[&<>"']/g, char => ({
    "&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"
  }[char]));
}
function formatDate(value) {
  if (!value) return "Date not supplied";
  const date = new Date(`${value}T12:00:00`);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString(undefined, {day:"numeric",month:"short",year:"numeric"});
}
function showToast(message) {
  const toast = document.getElementById("toast");
  toast.textContent = message;
  toast.classList.add("show");
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => toast.classList.remove("show"), 3200);
}
function navigate(viewName) {
  const validViews = ["home", "report", "search", "claims", "help"];
  const view = validViews.includes(viewName) ? viewName : "home";
  document.querySelectorAll(".view").forEach(section => section.classList.add("hidden"));
  const target = document.getElementById(`${view}View`);
  if (target) target.classList.remove("hidden");
  document.querySelectorAll(".nav-link").forEach(button => {
    button.classList.toggle("active", button.dataset.view === view);
    if (button.dataset.view === view) button.setAttribute("aria-current", "page");
    else button.removeAttribute("aria-current");
  });
  document.getElementById("mainNav").classList.remove("open");
  document.getElementById("menuToggle").setAttribute("aria-expanded", "false");
  if (view === "search") renderItems();
  if (view === "claims") renderClaims();
  window.scrollTo({top:0, behavior:"smooth"});
}
document.addEventListener("click", event => {
  const button = event.target.closest("[data-view]");
  if (!button) return;
  event.preventDefault();
  if (button.dataset.view === "report" && button.dataset.reportType) {
    document.getElementById("reportType").value = button.dataset.reportType;
  }
  navigate(button.dataset.view);
});

document.getElementById("menuToggle").addEventListener("click", () => {
  const nav = document.getElementById("mainNav");
  const isOpen = nav.classList.toggle("open");
  document.getElementById("menuToggle").setAttribute("aria-expanded", String(isOpen));
});

const reportForm = document.getElementById("reportForm");
const errorBox = document.getElementById("formError");
document.getElementById("itemDate").value = new Date().toISOString().slice(0,10);
reportForm.addEventListener("submit", event => {
  event.preventDefault();
  errorBox.classList.add("hidden");
  if (!reportForm.checkValidity()) {
    errorBox.textContent = "Please complete all required fields. Check the highlighted fields and try again.";
    errorBox.classList.remove("hidden");
    errorBox.focus();
    reportForm.reportValidity();
    return;
  }
  const data = new FormData(reportForm);
  const newItem = {
    id: `item-${Date.now()}`,
    type: String(data.get("reportType")),
    name: String(data.get("itemName")).trim(),
    category: String(data.get("category")),
    date: String(data.get("itemDate")),
    location: String(data.get("location")).trim(),
    description: String(data.get("description")).trim(),
    contact: String(data.get("contact") || "").trim(),
    emoji: emojiForCategory(String(data.get("category"))),
    demo:false
  };
  items.unshift(newItem);
  save();
  reportForm.reset();
  document.getElementById("itemDate").value = new Date().toISOString().slice(0,10);
  showToast("Report submitted. Your item now appears in Search items.");
  navigate("search");
});
function emojiForCategory(category) {
  return ({
    "Electronics":"🎧","Keys":"🔑","Bags":"👜","Clothing":"🧥",
    "Books & stationery":"📚","Cards & IDs":"💳","Jewellery":"💍","Other":"📦"
  })[category] || "📦";
}

["searchInput","statusFilter","categoryFilter","sortFilter"].forEach(id => {
  document.getElementById(id).addEventListener(id === "searchInput" ? "input" : "change", renderItems);
});
document.getElementById("clearSearch").addEventListener("click", resetFilters);
document.getElementById("resetFilters").addEventListener("click", resetFilters);
function resetFilters() {
  document.getElementById("searchInput").value = "";
  document.getElementById("statusFilter").value = "all";
  document.getElementById("categoryFilter").value = "all";
  document.getElementById("sortFilter").value = "newest";
  renderItems();
}
function renderItems() {
  const query = document.getElementById("searchInput").value.trim().toLowerCase();
  const status = document.getElementById("statusFilter").value;
  const category = document.getElementById("categoryFilter").value;
  const sort = document.getElementById("sortFilter").value;
  let filtered = items.filter(item => {
    const searchable = `${item.name} ${item.description} ${item.location} ${item.category}`.toLowerCase();
    return (!query || searchable.includes(query)) &&
      (status === "all" || item.type === status) &&
      (category === "all" || item.category === category);
  });
  filtered.sort((a,b) => {
    if (sort === "name") return a.name.localeCompare(b.name);
    const diff = new Date(`${a.date}T12:00:00`) - new Date(`${b.date}T12:00:00`);
    return sort === "oldest" ? diff : -diff;
  });
  document.getElementById("resultCount").textContent = `${filtered.length} ${filtered.length === 1 ? "item" : "items"} found`;
  const grid = document.getElementById("itemGrid");
  grid.innerHTML = filtered.map(item => `
    <article class="item-card">
      <div class="item-card-top"><span class="item-emoji" aria-hidden="true">${escapeHTML(item.emoji || "📦")}</span>
        <span class="status status-${escapeHTML(item.type)}">${item.type === "lost" ? "Lost" : "Found"}</span></div>
      <h3>${escapeHTML(item.name)}</h3>
      <p class="item-description">${escapeHTML(item.description)}</p>
      <div class="item-meta"><span>⌖ ${escapeHTML(item.location)}</span><span>▦ ${escapeHTML(item.category)} · ${escapeHTML(formatDate(item.date))}</span></div>
      ${item.type === "found" ? `<button class="btn btn-primary claim-button" data-claim="${escapeHTML(item.id)}">Request a claim</button>` : `<button class="btn btn-secondary" data-lost-help="${escapeHTML(item.id)}">View report details</button>`}
    </article>`).join("");
  grid.classList.toggle("hidden", filtered.length === 0);
  document.getElementById("emptyState").classList.toggle("hidden", filtered.length !== 0);
}
document.getElementById("itemGrid").addEventListener("click", event => {
  const claimButton = event.target.closest("[data-claim]");
  if (claimButton) openClaim(claimButton.dataset.claim);
  const detailsButton = event.target.closest("[data-lost-help]");
  if (detailsButton) {
    const item = items.find(entry => entry.id === detailsButton.dataset.lostHelp);
    if (item) showToast(`Lost report: ${item.name}. If you found a possible match, report it or contact campus support.`);
  }
});

const dialog = document.getElementById("claimDialog");
function openClaim(itemId) {
  currentClaimItem = items.find(item => item.id === itemId);
  if (!currentClaimItem) return;
  document.getElementById("claimItemId").value = itemId;
  document.getElementById("claimItemSummary").textContent = `${currentClaimItem.name} · ${currentClaimItem.location}`;
  document.getElementById("claimDetails").value = "";
  document.getElementById("claimContact").value = "";
  if (typeof dialog.showModal === "function") dialog.showModal();
  else showToast("Claim dialog is not supported in this browser.");
  document.getElementById("claimDetails").focus();
}
document.getElementById("closeClaim").addEventListener("click", () => dialog.close());
document.getElementById("claimForm").addEventListener("submit", event => {
  event.preventDefault();
  const details = document.getElementById("claimDetails").value.trim();
  if (!details) {
    document.getElementById("claimDetails").focus();
    showToast("Please describe a detail that helps verify ownership.");
    return;
  }
  const claim = {
    id:`claim-${Date.now()}`,
    itemId:currentClaimItem.id,
    itemName:currentClaimItem.name,
    location:currentClaimItem.location,
    details,
    contact:document.getElementById("claimContact").value.trim(),
    status:"Submitted for review",
    date:new Date().toISOString()
  };
  claims.unshift(claim);
  save();
  dialog.close();
  showToast("Claim request submitted. Review the status in My claims.");
  renderClaims();
});
function renderClaims() {
  const list = document.getElementById("claimsList");
  const empty = document.getElementById("claimsEmpty");
  empty.classList.toggle("hidden", claims.length > 0);
  list.innerHTML = claims.map(claim => `
    <article class="claim-row"><div><h3>${escapeHTML(claim.itemName)}</h3>
      <p>Collection area: ${escapeHTML(claim.location)}</p>
      <p>Submitted ${escapeHTML(new Date(claim.date).toLocaleDateString())}</p></div>
      <span class="claim-status">${escapeHTML(claim.status)}</span></article>`).join("");
}
renderItems();
