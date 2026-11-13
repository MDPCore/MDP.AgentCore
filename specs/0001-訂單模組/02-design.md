# 0001 訂單模組 — 系統設計

依據：`01-requirement.md`。本文件只設計需求中列出的行為，未列出者不做。

用語：「**客戶回條**」指 01-requirement.md「客人欄位」一節中客人填寫的欄位（不含交易規範同意的勾選本身），對應實體 `OrderReply`。填寫期限（逾期時間）放在 `OrderToken.ExpireTime`，填寫網址中的憑證放在 `OrderToken.AccessKey`。

## 1. 專案架構

新增第四個 bounded-context 模組 `MBS.Orders`，結構比照 `MBS.Appointments`：Domain 專案、Accesses 專案、Tests 專案，入口為 `OrderContext`。`MBS.Orders` 不參考其他 `MBS.*` 模組（只以 `string CompanyId` 關聯商家）。

| 專案 | 變更 |
|---|---|
| `MBS.Orders`（新專案） | §2 的領域物件；套件 `MDP.Domain`、`MDP.Registration`（同 `MBS.Appointments`） |
| `MBS.Orders.Accesses`（新專案） | `SqlOrderRepository`、`SqlOrderReplyRepository`、`SqlOrderTokenRepository`、`StringListTypeHandler`、建表腳本 `Scripts/CreateTables.sql`（§2.10）；參考 `MBS.Orders`，套件同 `MBS.Appointments.Accesses` |
| `MBS.Orders.Tests`（新專案） | xUnit，`MockOrderRepository`、`MockOrderReplyRepository`、`MockOrderTokenRepository` 與測試類別 |
| `CLK.OrderGain` | `.csproj` 加入 `MBS.Orders` 的 ProjectReference（讓 CoreApp 經由 `CLK.OrderGain` 取得，與其他三個模組相同）；程式碼不變 |
| `CLK.OrderGain.Accesses` | `.csproj` 加入 `MBS.Orders.Accesses` 的 ProjectReference，WebApp 才會在執行期載入三個 SQL repository |
| `CLK.OrderGain.sln` | 三個新專案加入 `01.MBS` 方案資料夾 |
| `CLK.OrderGain.CoreApp` | `_Imports.razor` 加 `@using MBS.Orders;`；新增 `Components/Companies/Orders/*`（店員頁面）與 `Components/Sites/OrderReplys/UpdatePage`（客戶回條頁）；修改 `Components/Shared/MainFooter.razor`、`Components/Shared/MainSidebar.razor` |
| `appsettings.json` | **不變**（既有權限規則已涵蓋，見 §3） |
| 資料庫 `DefaultDatabase` | 新增 `Orders_Orders`、`Orders_OrderReplys`、`Orders_OrderTokens` 三張資料表（§2.10） |

依賴方向（A → B 表示 A 參考 B）：

- `MBS.Orders.Accesses` → `MBS.Orders`
- `CLK.OrderGain` → `MBS.Orders`（新增）
- `CLK.OrderGain.Accesses` → `MBS.Orders.Accesses`（新增）
- CoreApp 頁面注入 `OrderContext`（同既有頁面直接注入 `CompanyContext`、`AppointmentContext`），不碰 SQL repository。`OrderGainContext` 不需要協調訂單與其他模組的寫入（Q11：不寫入 `Customer`），因此不修改。

## 2. 物件模型

### 2.1 物件一覽

| 物件 | 專案／位置 | 新增／異動 | 說明 |
|---|---|---|---|
| `Order` | `MBS.Orders/Order.cs` | 新增 | 訂單：訂單號碼、工作項目、店內欄位（§2.2） |
| `OrderReply` | `MBS.Orders/OrderReply.cs` | 新增 | 客戶回條，與訂單一對一（§2.3） |
| `OrderToken` | `MBS.Orders/OrderToken.cs` | 新增 | 填寫期限，與訂單一對一（§2.4） |
| `OrderOptions` | `MBS.Orders/Defaults/OrderOptions.cs` | 新增 | 選項清單、交易規範、填寫期限長度（§2.5） |
| `IOrderRepository` | `MBS.Orders/Interfaces/IOrderRepository.cs` | 新增 | `Order` 的存取介面（§2.6） |
| `IOrderReplyRepository` | `MBS.Orders/Interfaces/IOrderReplyRepository.cs` | 新增 | `OrderReply` 的存取介面（§2.7） |
| `IOrderTokenRepository` | `MBS.Orders/Interfaces/IOrderTokenRepository.cs` | 新增 | `OrderToken` 的存取介面（§2.8） |
| `OrderContext` | `MBS.Orders/OrderContext.cs` | 新增 | 模組入口，客戶回條相關的商業邏輯都在這裡（§2.9） |
| `SqlOrderRepository` | `MBS.Orders.Accesses/SqlOrderRepository.cs` | 新增 | `IOrderRepository` 的 SQL 實作（§2.10） |
| `SqlOrderReplyRepository` | `MBS.Orders.Accesses/SqlOrderReplyRepository.cs` | 新增 | `IOrderReplyRepository` 的 SQL 實作（§2.10） |
| `SqlOrderTokenRepository` | `MBS.Orders.Accesses/SqlOrderTokenRepository.cs` | 新增 | `IOrderTokenRepository` 的 SQL 實作（§2.10） |
| `StringListTypeHandler` | `MBS.Orders.Accesses/StringListTypeHandler.cs` | 新增 | `List<string>` 與 JSON 字串欄位的轉換（§2.10） |
| `MockOrderRepository`、`MockOrderReplyRepository`、`MockOrderTokenRepository`（暫代） | `MBS.Orders.Accesses/Mock*Repository.cs` | 新增（暫代，SQL 完成後刪除） | SQL repository 完成前的記憶體實作，`singleton` 註冊，資料於 WebApp 重新啟動後清空（§2.11） |
| `MockOrderRepository` | `MBS.Orders.Tests/MockOrderRepository.cs` | 新增 | `IOrderRepository` 的記憶體實作，測試用（§2.11） |
| `MockOrderReplyRepository` | `MBS.Orders.Tests/MockOrderReplyRepository.cs` | 新增 | `IOrderReplyRepository` 的記憶體實作，測試用（§2.11） |
| `MockOrderTokenRepository` | `MBS.Orders.Tests/MockOrderTokenRepository.cs` | 新增 | `IOrderTokenRepository` 的記憶體實作，測試用（§2.11） |

既有的 `MBS.*` 領域物件與 `OrderGainContext` 都不異動。

共同原則：

- **實體不實作 `IValidatableObject`**；實體方法只讀取或改動自己的屬性（例如 `OrderToken.CanUpdateReply`），不存取 repository。
- **每個實體都有 `CreateTime`、`UpdateTime`**，只在 repository 寫入時變更：`Add` 設定兩者為寫入當下時間，`Update` 只更新 `UpdateTime`、不寫入 `CreateTime`。頁面與 `OrderContext` 都不設定這兩個屬性。
- **商業邏輯放在 `OrderContext` 的方法中**（§2.9）：更新填寫期限、客人送出（含客戶回條欄位檢查）。判斷是否仍在填寫期限內是 `OrderToken.CanUpdateReply`（§2.4）。
- **`Order`** 的新增、查詢、修改、刪除由頁面直接呼叫 `OrderContext.OrderRepository`（同 `Companies/Services/*` 直接呼叫 `AppointmentContext.ServiceRepository`）；`Order` 的欄位檢查與訂單號碼重複檢查在頁面完成（§4.3、§4.5）。
- **`OrderReply`** 預設不存在，建立訂單時不建立；客人第一次送出時由 `OrderContext.ApplyOrderReply` 新增。寫入只有客人送出一種，一律經過 `ApplyOrderReply`；店員不能更正客人欄位，只會查詢與隨訂單刪除，由頁面直接呼叫 `OrderContext.OrderReplyRepository`。
- **`OrderToken`** 預設不存在，建立訂單時不建立；第一次出示 QR Code 時由 `OrderContext.RefreshOrderToken` 新增，之後的出示都更新同一份。寫入一律經過 `RefreshOrderToken`；頁面只透過 `OrderContext.OrderTokenRepository` 查詢與隨訂單刪除。沒有 `OrderToken` 表示尚未出示過，視同逾期（AC-2.3）。
- 依 01-requirement.md Q19，不記錄同意時間、不顯示訂單狀態，因此沒有對應的欄位。
- 與時間有關的方法都接收選填參數 `DateTime? nowTime = null`，方法內以 `nowTime ?? DateTime.Now` 取得現在時間；頁面不傳，測試一律傳入固定時間。WebApp 的 Docker 映像已設定 `TZ="Asia/Taipei"`，`DateTime.Now` 即台灣時間。

### 2.2 `Order`

#### 屬性

| 屬性 | 型別 | 對應需求 | 說明 |
|---|---|---|---|
| `CompanyId` | `string` | 一張訂單只屬於一間商家 | |
| `OrderId` | `string` | 填寫網址 | `Identifier.NewId()`；同時用於店員頁面網址與客戶回條頁的填寫網址（§2.9 填寫網址） |
| `OrderNumber` | `string` | 訂單號碼 | 店員人工填寫，必填；同一商家內不可重複（不分大小寫）；可修改 |
| `Content` | `string` | 本次裱框內容物 | 必填 |
| `PaymentStatus` | `string` | 付款狀況 | 必填 |
| `Amount` | `int` | 成交金額 | `>= 0` |
| `Note` | `string` | 小備註 | 選填 |
| `WorkCategoryList` | `List<string>` | 作品分類 | 至少一項；值必須是 `OrderOptions.WorkCategoryList` 之一 |
| `WorkCategoryOther` | `string` | 作品分類「其他」的文字 | 勾選「其他」時必填；未勾選時不清除也不檢查，保留原值 |
| `DeliveryDate` | `DateTime?` | 預計交貨日期 | 必填（頁面以日曆選取，預設今天）；只取日期。型別維持 `DateTime?`，由頁面檢查不可為空 |
| `CreateTime` | `DateTime` | 列表排序（AC-4.2） | 由 repository 寫入 |
| `UpdateTime` | `DateTime` | — | 由 repository 寫入 |

#### 方法

無。

### 2.3 `OrderReply`

#### 屬性

以 `CompanyId` + `OrderId` 對應到一張訂單，一對一。預設不存在：建立訂單時不建立，客人第一次送出時新增，之後的送出都更新同一份；刪除訂單時一併刪除。不含交易規範同意的勾選（只在送出時檢查，不儲存）。

| 屬性 | 型別 | 對應需求 | 說明 |
|---|---|---|---|
| `CompanyId` | `string` | — | 所屬商家 |
| `OrderId` | `string` | — | 所屬訂單 |
| `Name` | `string` | 客戶姓名 | 必填 |
| `Phone` | `string` | 客戶手機 | 必填 |
| `Mail` | `string` | 客戶信箱 | 必填 |
| `Area` | `string` | 住家區域 | 單選，必填 |
| `AreaOther` | `string` | 住家區域「其他」的文字 | 選「其他」時必填；未選時不清除也不檢查，保留原值 |
| `Gender` | `string` | 性別 | 單選，必填 |
| `Age` | `string` | 年齡 | 單選，選填 |
| `SourceList` | `List<string>` | 從哪裡認識 裱室 ? | 複選，至少一項 |
| `SourceOther` | `string` | 「其他」的文字 | 選「其他」時必填；未選時不清除也不檢查，保留原值 |
| `NoticeMethod` | `string` | 通知取件方式 | 單選，必填 |
| `CreateTime` | `DateTime` | — | 由 repository 寫入 |
| `UpdateTime` | `DateTime` | — | 由 repository 寫入 |

「必填」等規則是客人送出時的檢查（§2.9）。

#### 方法

無。

### 2.4 `OrderToken`

#### 屬性

以 `CompanyId` + `OrderId` 對應到一張訂單，一對一。預設不存在：建立訂單時不建立，第一次出示 QR Code 時新增，之後的出示都更新同一份（重新產生 `AccessKey`、重新計算 `ExpireTime`）；刪除訂單時一併刪除。

| 屬性 | 型別 | 對應需求 | 說明 |
|---|---|---|---|
| `CompanyId` | `string` | — | 所屬商家 |
| `OrderId` | `string` | — | 所屬訂單 |
| `AccessKey` | `string` | 填寫網址（AC-2.4、2.5） | 放在填寫網址中；每次出示都以 `Identifier.NewId()` 重新產生，舊值隨即失效；只由 `OrderContext.RefreshOrderToken` 改變 |
| `ExpireTime` | `DateTime` | 填寫期限 | 逾期時間，超過即不能開啟與送出客戶回條；只由 `OrderContext.RefreshOrderToken` 改變 |
| `CreateTime` | `DateTime` | — | 由 repository 寫入 |
| `UpdateTime` | `DateTime` | — | 由 repository 寫入 |

#### 方法

| 方法 | 用途 | AC |
|---|---|---|
| `bool CanUpdateReply(string accessKey, DateTime? nowTime = null)` | 客人以這個 `AccessKey` 目前是否可開啟與送出客戶回條 | 2.2、2.3、2.4、3.5 |

**`CanUpdateReply`**：取 `now = nowTime ?? DateTime.Now`；`accessKey == AccessKey` 且 `now <= ExpireTime` 時為 `true`，否則為 `false`（舊 QR Code 的 `AccessKey` 不符，視同逾期，AC-2.4）。沒有 `OrderToken`（尚未出示過）時由呼叫端視同逾期（AC-2.3）。

### 2.5 `OrderOptions`（static）

依 Q12，選項與交易規範寫在程式中，所有商家共用。放在 `Defaults/`，比照 `MBS.Companies/Defaults/RoleDefaults.cs`。內容逐字照 01-requirement.md「欄位」一節，存放選項的顯示文字本身，不另設代碼。

| 成員 | 內容 |
|---|---|
| `OtherOption` | `"其他"` |
| `WorkCategoryList` | 作品分類 12 項，含「其他」（「書法、水墨畫」為一項） |
| `AreaList` | 住家區域 7 項，含「其他」 |
| `GenderList` | `男`、`女` |
| `AgeList` | `15-25歲`、`26-35歲`、`35-45歲`、`46歲以上` |
| `SourceList` | 從哪裡認識 6 項，含「其他」 |
| `NoticeMethodList` | `電話通知`、`簡訊通知`、`LINE通知(有加入官方line)` |
| `TermsContent` | 交易規範內文，逐字照 01-requirement.md「交易規範原文」（含換行與空白），以字串常數寫死在程式中，所有商家共用；修改內文即修改程式 |
| `UpdateDuration` | `TimeSpan.FromMinutes(30)`；出示 QR Code 時由 DetailPage 計算填寫期限使用（§4.4） |

### 2.6 `IOrderRepository`

```csharp
public interface IOrderRepository
{
    // Methods
    void Add(Order order);                                              // 設定 CreateTime、UpdateTime
    void Update(Order order);                                           // 設定 UpdateTime；不寫入 CompanyId、OrderId、CreateTime
    void Remove(string companyId, string orderId);
    Order FindByOrderId(string companyId, string orderId);              // 找不到回傳 null
    List<Order> FindAllByCompanyId(string companyId);                   // 依 CreateTime 由新到舊
}
```

- 所有查詢都以 `CompanyId` + 鍵值查詢，不提供只用 `OrderId` 的查詢，確保跨商家拿不到資料（AC-4.4）。
- 不提供依訂單號碼查詢：訂單號碼重複檢查由頁面以 `FindAllByCompanyId` 讀出同一商家的訂單，在程式中以 `string.Equals(..., StringComparison.OrdinalIgnoreCase)` 比對（只忽略大小寫，AC-1.3）；只讀同一商家，因此重複檢查只在同一商家內（AC-1.4）。資料庫不設唯一限制，同時建立時可能出現重複，本版接受。

### 2.7 `IOrderReplyRepository`

```csharp
public interface IOrderReplyRepository
{
    // Methods
    void Add(OrderReply orderReply);                                // 設定 CreateTime、UpdateTime；同一訂單已有客戶回條時丟出例外
    void Update(OrderReply orderReply);                             // 設定 UpdateTime；不寫入 CompanyId、OrderId、CreateTime
    void Remove(string companyId, string orderId);                  // 沒有客戶回條時不做任何事
    OrderReply FindByOrderId(string companyId, string orderId);     // 找不到回傳 null
    List<OrderReply> FindAllByCompanyId(string companyId);
}
```

- 所有查詢都以 `CompanyId` + 鍵值查詢（AC-4.4）。

### 2.8 `IOrderTokenRepository`

```csharp
public interface IOrderTokenRepository
{
    // Methods
    void Add(OrderToken orderToken);                                // 設定 CreateTime、UpdateTime；同一訂單已有 OrderToken 時丟出例外
    void Update(OrderToken orderToken);                             // 設定 UpdateTime；不寫入 CompanyId、OrderId、CreateTime
    void Remove(string companyId, string orderId);                  // 沒有 OrderToken 時不做任何事
    OrderToken FindByOrderId(string companyId, string orderId);     // 找不到回傳 null
}
```

- 所有查詢都以 `CompanyId` + 鍵值查詢（AC-4.4）。

### 2.9 `OrderContext`

`[Service<OrderContext>(singleton: true)]`，建構子注入 `IOrderRepository`、`IOrderReplyRepository`、`IOrderTokenRepository`。

#### 屬性

| 屬性 | 型別 | 對應需求 | 說明 |
|---|---|---|---|
| `OrderRepository` | `IOrderRepository` | US-1、US-5（AC-1.1–1.4、4.1–4.4、5.1–5.3） | 公開 repository（同 `AppointmentContext.ServiceRepository`）；`Order` 的新增、查詢、修改、刪除由頁面直接呼叫 |
| `OrderReplyRepository` | `IOrderReplyRepository` | AC-3.4、4.1、4.2、5.2 | 公開 repository；店員頁面的查詢、刪除與客戶回條頁帶出上次內容由頁面直接呼叫；頁面不呼叫 `Add`、`Update`，寫入一律經過 `ApplyOrderReply` |
| `OrderTokenRepository` | `IOrderTokenRepository` | AC-2.2–2.4、3.5、5.2 | 公開 repository；客戶回條頁判斷填寫期限與刪除訂單時由頁面直接呼叫 `FindByOrderId`、`Remove`；頁面不呼叫 `Add`、`Update`，寫入一律經過 `RefreshOrderToken` |

#### 方法

| 方法 | 用途 | AC |
|---|---|---|
| `OrderToken RefreshOrderToken(string companyId, string orderId, DateTime expireTime)` | 出示 QR Code 時，重新產生 `AccessKey`，把逾期時間設為傳入的時間，回傳 `OrderToken` | 2.1、2.4 |
| `void ApplyOrderReply(OrderReply orderReply, string accessKey, DateTime? nowTime = null)` | 客人送出客戶回條（檢查 `AccessKey` 與逾期時間） | 2.4、3.2–3.8 |

**填寫網址**：`https://ordergain.tw/Sites/{CompanyId}/OrderReplys/Update/{OrderId}/{AccessKey}`（主機名沿用 `Employees/NewPage` 的寫法）。`OrderId`、`AccessKey` 都由 `MDP.Domain.Identifier.NewId()` 產生（8 碼識別碼，例如 `01VEKXI2`），不從訂單號碼推導，因此知道訂單號碼或其他訂單的網址也組不出這張訂單的填寫網址（AC-2.5）。每次出示都重新產生 `AccessKey`，QR Code 內容隨之改變，之前出示過的 QR Code 因 `AccessKey` 不符而失效（AC-2.4）。期限與 `AccessKey` 存在資料庫，不採用 `Employees/NewPage` 的 JWT 簽章做法。

**`RefreshOrderToken`**：新增或更新 `OrderToken`，不寫入 `Order`、`OrderReply`。

1. `OrderRepository.FindByOrderId(companyId, orderId)`；不存在 → `InvalidOperationException`。
2. `OrderTokenRepository.FindByOrderId(companyId, orderId)`：
   - 不存在（第一次出示）→ 建立 `CompanyId`、`OrderId` 為傳入值、`AccessKey = Identifier.NewId()`、`ExpireTime = expireTime` 的 `OrderToken`，`OrderTokenRepository.Add`（AC-2.1）。
   - 已存在 → `orderToken.AccessKey = Identifier.NewId()`、`orderToken.ExpireTime = expireTime`，不論目前是否已過期都直接覆蓋，`OrderTokenRepository.Update`（AC-2.4）。
3. 回傳該 `OrderToken`，頁面以它的 `AccessKey` 組填寫網址、以 `ExpireTime` 顯示截止時間。

**`ApplyOrderReply`**：檢查 `OrderToken` 的 `AccessKey` 與逾期時間後寫入 `OrderReply`。

1. 客戶回條欄位檢查（見下）不通過 → `ValidationException`，不寫入。例外的 `ValidationResult.MemberNames` 帶出第一個不通過的屬性名稱（例如 `Mail`），讓頁面在對應欄位顯示錯誤（AC-3.3、AC-3.8；Email 格式只在這裡檢查，其他規則前端已先擋，這是伺服器端的保護）。
2. `OrderRepository.FindByOrderId(orderReply.CompanyId, orderReply.OrderId)`；不存在 → `InvalidOperationException`，不寫入（AC-4.4、5.2）。
3. `OrderTokenRepository.FindByOrderId(...)` 為 `null`（尚未出示過）或 `orderToken.CanUpdateReply(accessKey, nowTime) == false`（舊 QR Code 或已逾期）→ `InvalidOperationException`，不寫入（AC-2.3、2.4、3.5）。
4. `OrderReplyRepository.FindByOrderId(orderReply.CompanyId, orderReply.OrderId)`：
   - 不存在（第一次送出）→ 以 `CompanyId`、`OrderId` 與傳入的客人欄位建立 `OrderReply`，`OrderReplyRepository.Add`（AC-3.2）。
   - 已存在 → 以傳入的客人欄位覆蓋讀出的 `OrderReply`，`OrderReplyRepository.Update`（AC-3.4、3.7）。
5. 不寫入 `Order`、`OrderToken`，因此工作項目、店內欄位與填寫期限不變（AC-3.6）。

交易規範同意（AC-3.3）不在 `ApplyOrderReply` 內檢查，由客戶回條頁的 `PageController` 在呼叫前檢查（§4.7）。

**客戶回條欄位檢查**（private，`ApplyOrderReply` 使用，AC-3.3）：

- `CompanyId`、`OrderId` 不可空白。
- 不整理資料（不去除前後空白、不清除 `*Other`），以傳入值檢查與儲存；資料整理由頁面在呼叫前完成（§4.7）。
- `Mail`、`Name`、`Phone`、`Area`、`Gender`、`NoticeMethod` 不可空白（只有空白字元也視為空白）。
- `Mail` 必須符合 Email 格式（`System.Net.Mail.MailAddress.TryCreate`，且結果的 `Address` 等於輸入值）；前端不檢查 Email 格式（AC-3.8）。
- `Area`、`Gender`、`Age`（有值時）、`NoticeMethod` 必須是 `OrderOptions` 中對應清單的值；`SourceList` 至少一項且每項都在清單中。
- `Area` 或 `SourceList` 選了「其他」時，對應的 `*Other` 不可空白；沒選「其他」時 `*Other` 不清除也不檢查，原樣儲存。

說明：

- 同時寫入：`Order`、`OrderToken`、`OrderReply` 各只有一種寫入（店員修改訂單、出示 QR Code、客人送出），彼此不會互相覆蓋。同一訂單第一次出示（或客人第一次送出）幾乎同時發生兩次時，後到的 `Add` 會因重複而丟出例外。本版接受這個限制，不做樂觀鎖定。

### 2.10 `SqlOrderRepository`、`SqlOrderReplyRepository`、`SqlOrderTokenRepository`

分別以 `[Service<IOrderRepository>()]`、`[Service<IOrderReplyRepository>()]`、`[Service<IOrderTokenRepository>()]` 註冊，依 §2.6–§2.8 的約定實作，寫法沿用既有 `Sql*Repository`（不呼叫 `Validator.ValidateObject`，因為實體不實作 `IValidatableObject`）。`CreateTime`、`UpdateTime` 以寫入當下的 `DateTime.Now` 設定。

#### 資料庫結構

資料表命名沿用既有 `{模組}_{實體複數}`（如 `Appointments_Services`），放在 `DefaultDatabase` 的 `dbo`。字串欄位一律 `NOT NULL`，空值存空字串（同實體屬性預設 `String.Empty`）。由系統產生的鍵值（`CompanyId`、`OrderId`、`AccessKey`）為 `nvarchar(50)`；其餘使用者輸入或選項的文字欄位一律 `nvarchar(max)`，因此不設輸入長度限制。不建立 `OrderNumber` 索引（重複檢查在程式中比對，§2.6）。不設外鍵，三張表的關聯與一併刪除由頁面處理（§4.6）。

`Orders_Orders`

| 欄位 | 型別 | 說明 |
|---|---|---|
| `CompanyId` | `nvarchar(50) NOT NULL` | PK（1） |
| `OrderId` | `nvarchar(50) NOT NULL` | PK（2） |
| `OrderNumber` | `nvarchar(max) NOT NULL` | 不設唯一限制（§2.6） |
| `Content` | `nvarchar(max) NOT NULL` | |
| `PaymentStatus` | `nvarchar(max) NOT NULL` | |
| `Amount` | `int NOT NULL` | |
| `Note` | `nvarchar(max) NOT NULL` | |
| `WorkCategoryList` | `nvarchar(max) NOT NULL` | JSON 字串陣列（見下） |
| `WorkCategoryOther` | `nvarchar(max) NOT NULL` | |
| `DeliveryDate` | `datetime2 NULL` | 只存日期部分（時間為 00:00） |
| `CreateTime` | `datetime2 NOT NULL` | |
| `UpdateTime` | `datetime2 NOT NULL` | |

`Orders_OrderReplys`

| 欄位 | 型別 | 說明 |
|---|---|---|
| `CompanyId` | `nvarchar(50) NOT NULL` | PK（1） |
| `OrderId` | `nvarchar(50) NOT NULL` | PK（2）；一張訂單一筆，重複 `Add` 因 PK 衝突丟出例外（§2.7） |
| `Mail` | `nvarchar(max) NOT NULL` | |
| `Name` | `nvarchar(max) NOT NULL` | |
| `Phone` | `nvarchar(max) NOT NULL` | |
| `Area` | `nvarchar(max) NOT NULL` | |
| `AreaOther` | `nvarchar(max) NOT NULL` | |
| `Gender` | `nvarchar(max) NOT NULL` | |
| `Age` | `nvarchar(max) NOT NULL` | |
| `SourceList` | `nvarchar(max) NOT NULL` | JSON 字串陣列（見下） |
| `SourceOther` | `nvarchar(max) NOT NULL` | |
| `NoticeMethod` | `nvarchar(max) NOT NULL` | |
| `CreateTime` | `datetime2 NOT NULL` | |
| `UpdateTime` | `datetime2 NOT NULL` | |

`Orders_OrderTokens`

| 欄位 | 型別 | 說明 |
|---|---|---|
| `CompanyId` | `nvarchar(50) NOT NULL` | PK（1） |
| `OrderId` | `nvarchar(50) NOT NULL` | PK（2）；一張訂單一筆，重複 `Add` 因 PK 衝突丟出例外（§2.8） |
| `AccessKey` | `nvarchar(50) NOT NULL` | |
| `ExpireTime` | `datetime2 NOT NULL` | |
| `CreateTime` | `datetime2 NOT NULL` | |
| `UpdateTime` | `datetime2 NOT NULL` | |

- **複選欄位**（`WorkCategoryList`、`SourceList`）：以 JSON 字串陣列儲存（例如 `["書法、水墨畫","其他"]`），避免選項文字內的「、」「/」與分隔符號衝突。寫入時以 `System.Text.Json.JsonSerializer.Serialize`（`JavaScriptEncoder.UnsafeRelaxedJsonEscaping`，同 `SqlNoticeRepository` 的 `_jsonSerializerOptions`，中文不跳脫）序列化後 `AddParameter`；讀取時在 `ExecuteParse`／`ExecuteParseAll` 傳入 `StringListTypeHandler`（寫法比照既有的 `TimeSpanTypeHandler` 用法），把 JSON 字串轉回 `List<string>`；空字串或 `null` 轉成空清單。
- 建表 SQL 隨 `MBS.Orders.Accesses` 一併提供（`MBS.Orders.Accesses/Scripts/CreateTables.sql`），由開發者手動在資料庫執行；本專案沒有既有的 migration 機制。

### 2.11 `MockOrderRepository`、`MockOrderReplyRepository`、`MockOrderTokenRepository`

`MBS.Orders.Tests` 內手寫的記憶體實作，依 `CompanyId` 隔離，遵守 §2.6–§2.8 的約定（`CreateTime`／`UpdateTime` 的寫入、重複時丟例外、`Update` 不寫入的欄位、排序）。不使用 mocking 框架。

**暫代的執行期實作**：SQL repository 完成前（03-tasks.md T3–T9），`MBS.Orders.Accesses` 內另有同名的三個記憶體 repository，以 `[Service<I*Repository>(singleton: true)]` 註冊並以 `lock` 保護同時存取，讓 WebApp 能執行訂單頁面；行為同上。SQL repository 完成時（T10）刪除。

## 3. 權限與匿名存取

既有規則（`appsettings.json` 的 `Authorization:Permissions`）不需修改：

| 頁面 | 路由 | 適用的既有規則 | 頁面屬性 |
|---|---|---|---|
| 店員各頁 | `/Companies/{CompanyId}/Orders/*` | `[CompanyId]:Employee` → `/Companies/[CompanyId]/*` | `[Authorize]` |
| 客戶回條頁 | `/Sites/{CompanyId}/OrderReplys/Update/{OrderId}/{AccessKey}` | `*` → `/Sites/*` | `[AllowAnonymous]`（同 `Sites/Home/IndexPage`） |

- AC-4.4：店員頁面由既有規則擋下非該商家成員；另外所有查詢都帶 `CompanyId`，即使把 B 商家的 `OrderId` 放進 A 商家網址也查不到。
- AC-2.2：客戶回條頁的授權完全由網址的 `OrderId` + `AccessKey`（`OrderToken` 存在且 `CanUpdateReply(accessKey)`）判斷，不看登入身分。
- 客戶回條頁的 `PageModel` 會送到瀏覽器，因此只放 §4.7 列出的欄位，**不包含** `PaymentStatus`、`Note`（US-4、AC-3.1）。

## 4. CoreApp 頁面

各頁依既有慣例：`{Page}.razor` + `{Page}.razor.cs`，巢狀 `PageController : InteropController` 與 `PageModel`；前端驗證、`mdp.blazorCore.interopManager.invokeAsync`、`toastManager`、`orderGain.viewManager` 切換 view 的寫法沿用 `Companies/Services/*`、`Sites/Appointments/*`。日期欄位使用既有的 pikaday（同 `Companies/BreakTimes/NewPage`）。

畫面上的欄位標籤一律四個字，讓欄位對齊（欄位本身與 01「欄位」一節相同，只是標籤文字縮短）：

| 01 欄位名稱 | 畫面標籤 |
|---|---|
| 本次裱框內容物 | 裱框內容 |
| 作品分類「其他」的文字 | 其他分類 |
| 預計交貨日期 | 交貨日期 |
| 小備註 | 訂單備註 |
| 住家區域「其他」的文字 | 其他區域 |
| 從哪裡認識 裱室 ? | 認識管道（明細頁）；客戶回條頁的選項標題保留原文 |
| 「從哪裡認識」的「其他」文字 | 其他管道 |
| 性別 | 客戶性別（明細頁） |
| 年齡 | 年齡區間（明細頁） |
| 通知取件方式 | 取件通知（明細頁） |
| 姓名（列表、明細、刪除確認頁） | 客戶姓名 |
| 列表的查詢框 | 訂單查詢 |

店員的建立、修改、明細頁分成兩個區塊，依序為：

1. **訂單資料**（對應 01 的「店內欄位」）：訂單號碼、成交金額、付款狀況、交貨日期、訂單備註。
2. **裱框資料**（對應 01 的「工作項目」）：裱框內容、作品分類（與其他分類）。

明細頁其後為「客戶資料」區塊。

畫面文字一律稱「客戶」（例如「客戶資料」「客戶姓名」）；本文件與 01 的說明文字仍以「客人」指同一對象。

其餘標籤（訂單號碼、作品分類、成交金額、付款狀況、客戶姓名、客戶手機、客戶信箱、住家區域）原本就是四個字。客戶回條頁的單選／複選題是在選項上方顯示題目，不受四字限制，保留 01 的欄位名稱。

沒有資料的欄位一律顯示「(無)」。

店員頁面的 `PageController` 注入 `OrderContext`；DetailPage 另注入 `QRCodeContext`（同 `Employees/NewPage`）；客戶回條頁另注入 `CompanyContext` 取商家名稱。

`Companies/Orders/UpdatePage`（店員修改訂單）與 `Sites/OrderReplys/UpdatePage`（客戶回條）同名但在不同資料夾與 namespace（`...Companies.Orders`、`...Sites.OrderReplys`），路由也不同。

### 4.1 `Components/Shared/MainFooter.razor`、`Components/Shared/MainSidebar.razor`（修改，AC-6.1、6.2）

兩個元件的 `CreateNavigationResourceList` 都在「預約行事曆」與「系統設定」之間新增：

```csharp
// 訂單
navigationResourceList.Add(new NavigationResource()
{
    Uri = $"/Companies/{this.CompanyId}/Orders/List",
    Text = "訂單",
    IconUri = "/img/icons/detail.svg"
});
```

兩個元件既有的授權過濾會讓非商家成員看不到這一項（AC-6.2），不需額外判斷。圖示沿用既有的 `detail.svg`，不新增圖檔。

### 4.2 `Companies/Orders/ListPage`（新增，AC-4.2、4.3、6.1）

- 路由 `/Companies/{CompanyId}/Orders/List`，含 `MainHeader`（右上「新增」→ NewPage，同 `Services/ListPage`）、`MainFooter` 與 `MainSidebar`（同 `Settings/IndexPage`；`PageController` 另注入 `CompanyContext` 取商家名稱給 `MainSidebar`）。
- 資料來源 `OrderRepository.FindAllByCompanyId` 與 `OrderReplyRepository.FindAllByCompanyId`，依 `OrderId` 對應；沒有對應的客戶回條表示客人尚未送出。
- 每筆顯示：訂單號碼、客人姓名、客戶手機、成交金額、預計交貨日期、付款狀況；沒有客戶回條、或姓名或電話為空時顯示「(無)」。點整筆進入 DetailPage。
- 查詢（AC-4.3）：頁首一個文字輸入框，輸入時在瀏覽器端即時過濾已渲染的清單——每筆以 `data-search` 存「訂單號碼 + 客人姓名 + 客戶手機」，比對是否包含輸入文字（不分大小寫）。本版列表一次載入該商家全部訂單，不分頁。

### 4.3 `Companies/Orders/NewPage`（新增，AC-1.1–1.4）

- 路由 `/Companies/{CompanyId}/Orders/New`。
- 欄位：訂單號碼、內容物（textarea）、作品分類（checkbox 清單，選「其他」時顯示文字框）、成交金額（`inputmode="numeric"`，前端以 `/^\d+$/` 檢查並確認不超過 `int` 上限）、預計交貨日期（pikaday，同 `BreakTimes/NewPage` 直接顯示日曆，預設選今天，不可清除；`disableDayFn` 停用今天以前的日期）、付款狀況、小備註。
- 前端驗證：不符 AC-1.2 時在對應欄位加 `invalid` 樣式並顯示錯誤，「建立」維持 disabled 不能按（同 `Services/UpdatePage` 的 `validate()`）。取消勾選「其他」時只隱藏文字框，不清除其內容。
- `AddOrderAsync(...)`：
  1. 整理資料：各文字欄位去除前後空白（`*Other` 也去除，但不清除）。
  2. 伺服器端再檢查一次 AC-1.2 的必填與格式（預計交貨日期不可為空，且不早於今天（`OrderInput.IsDeliveryDateAllowed`）；作品分類每項都在 `OrderOptions.WorkCategoryList` 中；選「其他」時 `WorkCategoryOther` 不可空白，未選「其他」時 `WorkCategoryOther` 不清除也不檢查），不通過回傳 `{ result: "invalid" }`。
  3. `OrderRepository.FindAllByCompanyId(companyId)` 中有任何一筆的 `OrderNumber` 與整理後的訂單號碼以 `OrdinalIgnoreCase` 相等 → 回傳 `{ result: "duplicate" }`，前端在訂單號碼欄位顯示「訂單號碼重複」（AC-1.3、1.4）。
  4. 建立 `Order`（`OrderId = Identifier.NewId()`，其餘帶入輸入值），`OrderRepository.Add(order)`（不建立 `OrderReply`、`OrderToken`）；回傳 `{ result: "success", orderId }`，前端導向 DetailPage。

### 4.4 `Companies/Orders/DetailPage`（新增，AC-2.1、2.4、4.1）

- 路由 `/Companies/{CompanyId}/Orders/Detail/{OrderId}`，資料來源 `OrderRepository.FindByOrderId` 與 `OrderReplyRepository.FindByOrderId`。
- 顯示：訂單號碼、工作項目、店內欄位全部、客戶回條全部（沒有客戶回條或空白欄位顯示「(無)」）。`*Other` 文字只在勾選（選取）「其他」時接在「其他」後面顯示，未勾選時不顯示（`*Other` 可能保留舊值）。重新整理即讀到最新資料（AC-4.1）。
- 操作（按鈕文字皆四個字）：「客戶填寫」→ 出示 QR Code（見下）；「訂單修改」→ `Companies/Orders/UpdatePage`；「訂單取消」→ RemovePage（實體刪除訂單，同 §4.6；按鈕文字為「取消」，行為仍是刪除）。
- 出示 QR Code：呼叫 `PresentOrderAsync()`，伺服器計算 `expireTime = DateTime.Now + OrderOptions.UpdateDuration`（從這次出示起 30 分鐘，AC-2.1、2.4），呼叫 `OrderContext.RefreshOrderToken(companyId, orderId, expireTime)` 取得 `OrderToken`，以它的 `AccessKey` 組出填寫網址，`QRCodeContext.CreateToBase64(填寫網址)` 產生圖片，回傳 `{ qrCodePicture, orderNumber, expireTime }`；前端切換到 `qrcode-view` 顯示 QR Code、訂單號碼與「填寫期限：yyyy-MM-dd HH:mm」。用 interop 而不是另開頁面，避免單純開網址（重新整理、預先載入）就重新產生 `AccessKey`、延長期限。`qrcode-view` 的「返回」重新載入明細頁。

### 4.5 `Companies/Orders/UpdatePage`（新增，AC-5.1）

- 路由 `/Companies/{CompanyId}/Orders/Update/{OrderId}`，欄位與驗證同 NewPage（帶出現值，訂單號碼可編輯；日曆選在原本的預計交貨日期，沒有時選今天；今天以前的日期停用，但原本的日期即使已過仍可選，伺服器端同樣允許保留原日期）。`UpdateOrderAsync(...)`：整理資料與伺服器端檢查同 NewPage 第 1、2 步；重複檢查同 NewPage 第 3 步但排除自己（`OrderId` 相同者），有相符 → `"duplicate"`（AC-5.1；只改大小寫的自己的號碼可儲存）；否則讀出訂單、只改訂單號碼、內容物、作品分類、成交金額、預計交貨日期、付款狀況、小備註，再 `OrderRepository.Update(order)`。
- 完成後 toast「已完成」並返回 DetailPage。
- 店員不能更正客人欄位，沒有修改客戶回條的頁面；客人資料需要修改時，店員重新出示 QR Code 由客人自行送出（AC-2.4、3.4）。

### 4.6 `Companies/Orders/RemovePage`（新增，AC-5.2、5.3）

- 路由 `/Companies/{CompanyId}/Orders/Remove/{OrderId}`，沿用既有 `Services/RemovePage`、`Sites/Appointments/RemovePage` 的刪除確認頁（AC-5.2、5.3）：顯示訂單號碼、客人姓名（沒有客戶回條或姓名為空時顯示「(無)」）與成交金額，按鈕「取消」（返回明細頁，不刪除）與「確認刪除」。
- `RemoveOrderAsync()`：依序 `OrderTokenRepository.Remove`、`OrderReplyRepository.Remove`（沒有時不做任何事）、`OrderRepository.Remove`（實體刪除），完成後導向 ListPage。先刪 `OrderToken` 讓填寫網址立即失效，縮小刪除途中客人送出的時間窗；客人若在 `OrderToken` 刪除前已通過 `ApplyOrderReply` 的期限檢查，仍可能在 `OrderReply` 刪除後才寫入，留下沒有訂單的 `OrderReply`。本版接受這個限制：這筆資料不會被任何頁面讀到（所有頁面都先查 `Order`）。

### 4.7 `Sites/OrderReplys/UpdatePage`（新增，客戶回條頁，AC-2.2、2.3、2.5、3.1–3.8、5.2）

- 路由 `/Sites/{CompanyId}/OrderReplys/Update/{OrderId}/{AccessKey}`，`@attribute [AllowAnonymous]`。不放 `MainFooter`、`MainSidebar`，讓客人專注填寫。
- `OnInitializedAsync(companyId, orderId, accessKey)` 呼叫 `OrderRepository.FindByOrderId`，訂單存在時再以 `OrderTokenRepository.FindByOrderId` 取得 `OrderToken`，`CanUpdate = orderToken != null && orderToken.CanUpdateReply(accessKey)`，`PageModel`：
  - `OrderExists`、`CanUpdate`（`bool`）
  - 兩者皆為 `true` 時才填入：`CompanyName`、`OrderNumber`、`Content`、`WorkCategoryList`、`WorkCategoryOther`（只在 `WorkCategoryList` 含「其他」時填入，否則為空字串，避免未勾選時保留的舊文字送到瀏覽器）、`Amount`、`DeliveryDate`、`TermsContent`，以及 `OrderReplyRepository.FindByOrderId` 取得的客人欄位（不含 `CreateTime`、`UpdateTime`；客人尚未送出過、沒有客戶回條時為空白）。
- 呈現：
  - `OrderExists == false` → 只顯示「此訂單已不存在」。
  - `CanUpdate == false` → 只顯示「填寫連結已過期，請洽店員重新出示 QR Code」（含尚未出示過、舊 QR Code 的 `AccessKey`，AC-2.3、2.4）。
  - 否則 → 上方唯讀區塊（商家名稱、訂單號碼、工作項目、成交金額、預計交貨日期；作品分類的「其他」文字只在勾選「其他」時顯示），下方客戶回條表單（帶出上次內容，AC-3.4），交易規範內文（`TermsContent`，以 `white-space: pre-wrap` 保留換行與空白）與「沒問題，我已了解」checkbox（**一律不預先勾選**），「送出」。
- 前端驗證同 §2.9 客戶回條欄位檢查（**Email 格式除外**，前端只檢查不可空白）加上必須勾選同意；不通過時對應欄位標 `invalid` 並顯示錯誤，「送出」disabled 不能按（AC-3.3）。取消選取「其他」時只隱藏文字框，不清除其內容。
- `ApplyOrderReplyAsync(...)`：`PageController` 依序判斷，回傳 `{ result }`：
  - `OrderRepository.FindByOrderId` 不存在 → `"notFound"`，前端切到「此訂單已不存在」。
  - `OrderToken` 不存在或 `orderToken.CanUpdateReply(accessKey) == false` → `"expired"`，前端切到過期訊息，不寫入（AC-2.4 用舊 QR Code 開啟的頁面、AC-3.5）。
  - `isAgreed != true` → `"invalid"`，不寫入（AC-3.3；前端已先擋，這是伺服器端的保護）。
  - 否則先整理資料：各文字欄位去除前後空白（`*Other` 也去除，但不清除）；再以網址的 `CompanyId`、`OrderId` 和整理後的欄位建立 `OrderReply`，呼叫 `OrderContext.ApplyOrderReply(orderReply, accessKey)`（`accessKey` 取自網址）；丟出 `ValidationException` → `{ result: "invalid", memberName }`（`memberName` 取自 `ValidationResult.MemberNames` 第一項），前端在該欄位標 `invalid`，`Mail` 時顯示「Email 格式錯誤」（AC-3.8）；丟出 `InvalidOperationException`（頁面判斷後才逾期、店員剛好重新出示或訂單剛好被刪除）→ `"expired"`；成功 → `"success"`，前端切到 `result-view` 顯示送出成功訊息（AC-3.2）。頁面先判斷逾期是為了顯示過期訊息，`ApplyOrderReply` 內的期限檢查是最後一道保護。
- 送出的參數只有客戶回條欄位與同意勾選；`ApplyOrderReply` 只寫入客人欄位，即使竄改請求也改不到工作項目、店內欄位與填寫期限（AC-3.6）。

## 5. 對既有行為的影響

| 項目 | 變更 | 對應 01-requirement.md |
|---|---|---|
| `Components/Shared/MainFooter.razor`、`Components/Shared/MainSidebar.razor` | 員工多一項「訂單」，共四項；非商家成員不變 | 對既有行為的影響 第 1 列 |
| 匿名存取 | 新增第二個 `[AllowAnonymous]` 頁面 `Sites/OrderReplys/UpdatePage`；權限設定檔不變 | 對既有行為的影響 第 2 列 |
| `OrderGainContext` | 不修改 | — |
| MauiApp | 經由 CoreApp → `CLK.OrderGain` 會參考到 `MBS.Orders`，但原本就沒有接 SQL 存取層（其他模組的 repository 也沒有實作），新增的 repository 依賴不改變現況；本功能不在 MAUI 驗收 | 不在範圍內 |
| 客戶資料 | 不寫入 `Customer` | Q11 |
| 資料庫 | 新增 `Orders_Orders`、`Orders_OrderReplys`、`Orders_OrderTokens` 三張資料表，既有資料表不變 | 內部變更 |
| 方案結構 | 新增 `MBS.Orders`、`MBS.Orders.Accesses`、`MBS.Orders.Tests` 並加入 `CLK.OrderGain.sln`；`CLK.OrderGain`、`CLK.OrderGain.Accesses` 各多一個 ProjectReference；`CLAUDE.md` 的模組說明（三個模組 → 四個）、依賴方向、Commands 與測試專案描述需同步更新 | 內部變更 |

其餘既有頁面、預約、客戶資料、估價工具、員工邀請 QR Code 都不修改。

## 6. 測試

新專案 `src/MBS.Orders.Tests`（xUnit，套件版本同 `MBS.Companies.Tests`，參考 `MBS.Orders`），以 `MockOrderRepository`、`MockOrderReplyRepository`、`MockOrderTokenRepository`（§2.11）取代 SQL。

商業邏輯都在 `OrderToken`、`OrderContext` 的方法中，以單元測試涵蓋。`Order` 的新增、修改、刪除與其欄位檢查、訂單號碼重複檢查，客戶回條的資料整理（去除前後空白），以及隨訂單刪除客戶回條與 `OrderToken`，都在頁面完成（AC-1.1–1.4、5.1、5.2），以手動在 WebApp 驗收；頁面行為（版面、前端驗證、導覽、權限擋下）也以手動驗收。

### 6.1 單元測試（`OrderTokenTests`、`OrderContextTests`、`MockRepositoryTests`）

- `CanUpdateReply_AfterExpireTime_ReturnsFalse`、`CanUpdateReply_WithinExpireTime_ReturnsTrue`（AC-2.3）
- `CanUpdateReply_AtExpireTime_ReturnsTrue`：`nowTime == ExpireTime` 時為 `true`；`CanUpdateReply_OneTickAfterExpireTime_ReturnsFalse`：`nowTime == ExpireTime + 1 tick` 時為 `false`（邊界，AC-2.3、3.5）
- `CanUpdateReply_WrongAccessKey_ReturnsFalse`（AC-2.4、2.5）
- `RefreshOrderToken_FirstTime_AddsTokenAndReturnsIt`：`AccessKey` 不為空、`ExpireTime` 為傳入值（AC-2.1）
- `RefreshOrderToken_Again_RegeneratesAccessKeyAndOverwritesExpireTime`：逾期後再次出示，`AccessKey` 與前次不同、`ExpireTime` 為新值（AC-2.4）
- `RefreshOrderToken_OrderNotFound_Throws`
- `RefreshOrderToken_DoesNotChangeOrderOrReply`：訂單與客戶回條不變
- `ApplyOrderReply_FirstTime_AddsReply`：沒有客戶回條時新增一筆（AC-3.2）
- `ApplyOrderReply_OrderNotFound_ThrowsAndDoesNotSave`、`ApplyOrderReply_OtherCompanyId_ThrowsAndDoesNotSave`（AC-4.4、5.2）
- `ApplyOrderReply_InvalidFields_ThrowsAndDoesNotSave`：各必填空白、來源為空、選「其他」未填文字、選項不在清單各自丟 `ValidationException`，且 `MemberNames` 為該屬性（AC-3.3）
- `ApplyOrderReply_InvalidMail_ThrowsWithMailMemberName`：Email 格式錯誤丟 `ValidationException`，`MemberNames` 為 `Mail`，不寫入（AC-3.8）
- `ApplyOrderReply_OtherTextWithoutOtherOption_SavesAsIs`：未選「其他」但 `AreaOther`、`SourceOther` 有值時不丟例外，原樣儲存
- `ApplyOrderReply_AlreadyReplied_OverwritesReplyFields`（AC-3.4）
- `ApplyOrderReply_NeverPresented_ThrowsAndDoesNotSave`（沒有 `OrderToken`，AC-2.3）、`ApplyOrderReply_AfterExpireTime_ThrowsAndDoesNotSave`（AC-3.5）
- `ApplyOrderReply_OldAccessKey_ThrowsAndDoesNotSave`：期限內再次出示後，以前一次的 `AccessKey` 送出（AC-2.4）
- `ApplyOrderReply_DoesNotChangeOrderOrToken`：訂單（工作項目、店內欄位）與 `OrderToken`（`AccessKey`、`ExpireTime`）不變（AC-3.6）
- `ApplyOrderReply_AfterStaffUpdateAndRefresh_Succeeds`：以 `OrderRepository.Update` 修改工作項目、`RefreshOrderToken` 重新出示後以新的 `AccessKey` 再次送出成功，客戶回條被覆蓋（AC-3.7）
- `MockRepository_AddAndUpdate_SetCreateTimeAndUpdateTime`：`Add` 設定兩者、`Update` 只改 `UpdateTime`（驗證三個 Mock 遵守 §2.6–§2.8 約定）

### 6.2 手動驗收（WebApp）

- 導覽：員工帳號的底部導覽與側邊選單都看到四項且順序正確；非商家成員兩處都看不到「訂單」（AC-6.1、6.2）。
- 建立與列表：建立後進入明細頁；必填錯誤時對應欄位顯示錯誤且「建立」不能按；同一商家重複訂單號碼（含只差大小寫，如 `A001` 與 `a001`）顯示「訂單號碼重複」、全形 `Ａ００１` 不視為重複；訂單號碼前後的空白會被去除；另一商家用相同號碼可建立；勾選「其他」輸入文字後取消勾選仍可建立，明細頁不顯示該文字；列表依建立時間由新到舊與查詢；以 A 商家帳號開 B 商家的訂單網址被拒（AC-1.1–1.4、4.2–4.4）。
- 出示 QR Code：顯示 QR Code、訂單號碼與截止時間（現在起 30 分鐘）；逾期後再次出示，截止時間從這次起重新計算；新 QR Code 可開啟，舊 QR Code 顯示過期訊息；用舊 QR Code 開著填寫頁時店員重新出示，按「送出」顯示過期訊息且不儲存（AC-2.1、2.4）。
- 客戶回條頁：用未登入的手機瀏覽器掃描；交易規範內文與 01-requirement.md「交易規範原文」一致且保留換行；畫面與頁面原始碼都沒有付款狀況與小備註；第一次開啟表單為空白；Email 格式錯誤時可以按「送出」，送出後客戶信箱欄位顯示「Email 格式錯誤」且不儲存；Email 前後有空白時可正常送出，儲存值不含空白；送出成功訊息、明細頁顯示客人欄位；期限內重開帶出上次內容且同意未勾選；用瀏覽器開發者工具送出未勾選同意的請求被拒；把 `OrderToken` 的 `ExpireTime` 改成過去時間後重開與送出都顯示過期訊息；隨意改網址中的 `AccessKey` 顯示過期訊息；隨意改網址中的 `OrderId` 顯示「此訂單已不存在」（AC-2.2、2.3、2.5、3.1–3.8、4.1）。
- 修改與刪除：修改後明細正確；訂單號碼可修改、改成同商家其他訂單的號碼時顯示「訂單號碼重複」、維持原號碼或只改自己號碼的大小寫可儲存；明細頁沒有修改客戶回條的入口；刪除確認頁按「取消」不刪除；有與沒有客戶回條、`OrderToken` 的訂單都能刪除，刪除後列表不見、填寫網址顯示「此訂單已不存在」；修改後重新出示，客戶回條頁看到最新內容（AC-3.7、5.1–5.3）。
- SQL repository：新增、讀取、複選欄位（含選項文字內的「、」「/」）往返正確，`CreateTime`／`UpdateTime` 有寫入；長文字（例如超過 1,000 字的內容物、小備註）可正常儲存與讀出。

## 7. 待確認事項

無。
