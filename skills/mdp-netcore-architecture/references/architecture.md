# MDP.NetCore 架構

.NET 專案的架構與程式碼規範，所有 .NET 專案一律適用。

## 目錄

01. [Workspace 設計規範](#01-workspace-設計規範)
02. [Architecture 設計規範](#02-architecture-設計規範)
03. [Namespace 設計規範](#03-namespace-設計規範)
04. [Class 設計規範](#04-class-設計規範)
05. [Field 設計規範](#05-field-設計規範)
06. [Constructor 設計規範](#06-constructor-設計規範)
07. [Properties 設計規範](#07-properties-設計規範)
08. [Method 設計規範](#08-method-設計規範)
09. [Context 設計規範](#09-context-設計規範)
10. [Entity 設計規範](#10-entity-設計規範)
11. [Repository 設計規範](#11-repository-設計規範)
12. [Dependency Injection 設計規範](#12-dependency-injection-設計規範)
13. [Razor 設計規範](#13-razor-設計規範)

## 用語

| 符號 | 意思 | 例子 |
|---|---|---|
| `{Domain}` | 領域模組專案（一個 bounded context） | `MDP.Orders` |
| `{App}` | 組合多個領域模組的應用程式專案 | `MDP.Shop` |
| `{Entity}` | 實體類別 | `Order`、`OrderToken` |
| `{Context}` | 領域模組的 Context 類別，以模組主題的單數名詞命名 | `OrderContext` |
| 呼叫端 | 注入並呼叫 Context 的程式 | `Controller`、`{App}Context` |

---

## 01. Workspace 設計規範

```
{repository}/
├── README.md
├── LICENSE
├── CLAUDE.md          ← 專案指引：本專案特有的 AI 規則與指令
├── docs/              ← 設計資料：產品設計、領域模型圖
├── specs/             ← 功能規格：需求分析、系統設計、任務拆解
└── src/
    ├── {Domain}/
    ├── {Domain}.Tests/
    ├── {Domain}.Mocks/
    ├── {Domain}.Accesses/
    ├── {App}/
    ├── {App}.Tests/
    ├── {App}.Mocks/
    ├── {App}.Accesses/
    ├── {App}.WebApp/ ...
    └── {App}.sln
```

- **repository 根目錄只放文件與設定**，所有程式碼都在 `src/`。打開 repository 先看到的就是文件。
- **`.sln` 放在 `src/`**，檔名是 `{App}.sln`。專案和方案在同一層，相對路徑最單純。
- **測試專案和被測專案並排**，`{Domain}` 旁邊就是 `{Domain}.Tests`，找測試不用換資料夾。

## 02. Architecture 設計規範

### 專案種類

| 專案 | 種類 | 內容 |
|---|---|---|
| `{Domain}` | 類別庫 | `{Domain}` 的 Context、Entity、Repository 介面、Defaults 常數 |
| `{Domain}.Tests` | xUnit | `{Domain}` 的單元測試 |
| `{Domain}.Mocks` | 類別庫 | `{Domain}` 的 Repository 資料模擬實作（`Mock{Entity}Repository`） |
| `{Domain}.Accesses` | 類別庫 | `{Domain}` 的 Repository 資料存取實作（例如 `Sql{Entity}Repository`） |
| `{App}` | 類別庫 | `{App}` 的 Entity、Repository 介面、`{App}Context`、跨模組協調的 `{動作}Service` |
| `{App}.Tests` | xUnit | `{App}` 的單元測試 |
| `{App}.Mocks` | 類別庫 | `{App}` 的 Repository 資料模擬實作（`Mock{Entity}Repository`） |
| `{App}.Accesses` | 類別庫 | `{App}` 的 Repository 資料存取實作（例如 `Sql{Entity}Repository`） |
| `{App}.CoreApp` | Razor 類別庫 | `{App}` 的 Blazor 頁面與元件 |
| `{App}.WebApp` | ASP.NET Core（`Sdk.Web`） | `{App}` 的網站、驗證設定、MVC Controller |
| `{App}.MauiApp` | MAUI | `{App}` 的原生 App |
| `{App}.ConsoleApp` | Console | `{App}` 的批次、排程工具 |

`{App}.WebApp`、`{App}.MauiApp`、`{App}.ConsoleApp` 是 Host，共用 `{App}.CoreApp` 的頁面。

只有一個領域、不需要組合的小專案，省略 `{Domain}`、`{Domain}.Tests`、`{Domain}.Mocks` 與 `{Domain}.Accesses`：Entity、Repository 介面與 Context 直接寫在 `{App}`，測試寫在 `{App}.Tests`，Mock 寫在 `{App}.Mocks`，Repository 實作寫在 `{App}.Accesses`。

### 依賴方向

A ──▶ B 表示 A 參考 B。

```
{Domain}.Tests    ──▶ {Domain} ＋ {Domain}.Mocks
{Domain}.Mocks    ──▶ {Domain}
{Domain}.Accesses ──▶ {Domain}
{App}             ──▶ 各 {Domain}
{App}.Tests       ──▶ {App} ＋ {App}.Mocks ＋ 各 {Domain}.Mocks
{App}.Mocks       ──▶ {App}
{App}.Accesses    ──▶ {App} ＋ 各 {Domain}.Accesses
{App}.CoreApp     ──▶ {App}
{App}.WebApp      ──▶ {App}.CoreApp ＋ {App}.Accesses
```

- **領域專案不參考 Accesses 與 Host。** 商業規則不依賴資料庫與 UI，只用記憶體 Repository 就能測試。
- **領域模組之間預設互不參考**，只用 `string` Id 關聯（例如 `CompanyId`）。確實需要另一個模組的型別或規則時才參考，並在 `CLAUDE.md` 寫明（例如 `MDP.Appointments` → `MDP.Companies`）。模組之間耦合越少，越能獨立修改與測試。
- **UI 不參考 Accesses。** `{App}.CoreApp` 只參考 `{App}`，頁面只拿得到 Context。
- **實作要被 Host 參考到才會生效。** DI 靠掃描組件註冊（見 12 章），Host 沒有經由 `{App}.Accesses` 參考到的 Repository 實作，執行時就不存在，會出現「無法解析 `I{Entity}Repository`」。新增 `{Domain}.Accesses` 時，記得在 `{App}.Accesses` 加上 ProjectReference。

### 測試專案

- **用 xUnit 的 `[Fact]`，不用 mocking 框架**，改用手寫的 `Mock{Entity}Repository`（見 11 章）。Mock 本身就是 Repository 約定的可執行說明，讀測試也不用懂 mocking 語法。
- **測試方法命名 `{方法}_{情境}_{結果}`**，例如 `CanUpdateReply_AfterExpireTime_ReturnsFalse`。
- **方法內以 `// Arrange`、`// Act`、`// Assert` 分段。**

## 03. Namespace 設計規範

```csharp
using System;
using System.Collections.Generic;

namespace MDP.Orders
{
    public class Order
    {
    }
}
```

- **用大括號區塊**，不用 file-scoped namespace。
- **類別庫專案的 namespace 一律等於專案名稱**，不隨資料夾加後綴。`MDP.Orders/Interfaces/IOrderRepository.cs`、`MDP.Orders/Defaults/OrderOptions.cs` 的 namespace 都是 `MDP.Orders`。資料夾只用來整理檔案，呼叫端只要 `using MDP.Orders;` 就拿得到整個模組。
- **`using` 放在檔案最上面**、`namespace` 之前，保留 Visual Studio 範本預設的 `System.*` using。

## 04. Class 設計規範

```csharp
public class OrderContext
{
    // Fields
    private readonly IOrderRepository _orderRepository = null;

    private readonly IOrderTokenRepository _orderTokenRepository = null;


    // Constructors
    public OrderContext(IOrderRepository orderRepository, IOrderTokenRepository orderTokenRepository)
    {
        ...
    }


    // Properties
    public IOrderRepository OrderRepository { get { return _orderRepository; } }
}
```

- **一個檔案一個型別**，檔名等於型別名稱。
- **成員依分類分組**，每組以單行註解標頭開始，依 `// Constants`、`// Fields`、`// Constructors`、`// Properties`、`// Methods` 的順序排列，沒有內容的分類不寫。每個類別都長得一樣，打開任何檔案都知道東西在哪裡。
- **同一組內的成員之間空一行，組與組之間空兩行。**
- **常數與選項放 `Defaults/`**，寫成 `static class`，命名 `{Entity}Options`（選項清單、預設值）或 `{名詞}Defaults`（例如 `RoleDefaults`），namespace 仍是專案名稱。
- **擴充方法放 `Extensions/`**，命名 `{型別}Extensions`。
- **列舉放 `Enumerations/`**，namespace 仍是專案名稱；每個成員明確給數值，從 1 開始，行尾加中文註解：

  ```csharp
  public enum AppointmentStatus : int
  {
      Booked = 1,    // 已預約
      Cancelled = 2  // 已取消
  }
  ```

## 05. Field 設計規範

```csharp
// Constants
public const string OtherOption = "其他";


// Fields
public static readonly TimeSpan UpdateDuration = TimeSpan.FromMinutes(30); // 填寫期限長度

private readonly IOrderRepository _orderRepository = null;

private readonly object _syncRoot = new object();
```

- **私有欄位命名 `_camelCase`**，加 `readonly` 並明確初始化。寫出 `= null`，一眼就知道這個欄位會在建構子賦值。
- **公開靜態欄位命名 `PascalCase`**，用 `static readonly`；編譯期常數用 `const`，放在 `// Constants`。
- **清單型的唯讀選項用 `IReadOnlyList<T>`**，避免被外部修改。
- **欄位意義不明顯時**，在行尾加中文註解。

## 06. Constructor 設計規範

```csharp
public OrderContext(
    IOrderRepository orderRepository,
    IOrderTokenRepository orderTokenRepository)
{
    #region Contracts

    ArgumentNullException.ThrowIfNull(orderRepository);
    ArgumentNullException.ThrowIfNull(orderTokenRepository);

    #endregion

    // Default
    _orderRepository = orderRepository;
    _orderTokenRepository = orderTokenRepository;
}
```

- **依賴一律從建構子注入**，本體固定為 `#region Contracts` → `// Default`。
- **每個參數都檢查**：參考型別用 `ArgumentNullException.ThrowIfNull`，字串用 `ArgumentException.ThrowIfNullOrEmpty`。DI 設定錯誤時會在建構當下就失敗，而不是等到第一次呼叫才出現 `NullReferenceException`。舊程式碼的 `if (x == null) throw new ArgumentException(nameof(x))` 不必回頭改。
- **參數多時一行一個。**
- **賦值之後還要建立內部物件時**（例如 `{App}Context` 建立 Service），另起一個標頭，例如 `// Service`。

## 07. Properties 設計規範

```csharp
public string OrderNumber { get; set; } = String.Empty; // 訂單號碼

public List<string> ItemList { get; set; } = new List<string>(); // 品項

public string ItemOther { get; set; } = String.Empty; // 其他品項

public IOrderRepository OrderRepository { get { return _orderRepository; } }
```

- **可讀寫的屬性寫成 `{ get; set; }`，並明確給預設值**。專案關閉 Nullable（`<Nullable>disable</Nullable>`），靠預設值保證新物件的字串和清單不是 null。

  | 型別 | 預設值 |
  |---|---|
  | `string` | `String.Empty` |
  | 數值 | `0` |
  | `bool` | `false` |
  | `DateTime` | `DateTime.MinValue` |
  | `TimeSpan` | `TimeSpan.Zero` |
  | 列舉 | 明確指定成員，例如 `AppointmentStatus.Booked` |
  | `DateTime?` 等可為 null 的型別 | `null` |
  | `List<T>` | `new List<T>()` |

- **公開欄位用唯讀屬性**，寫成 `{ get { return _欄位; } }`。
- **建構子建立的物件用 `{ get; private set; }` 公開**，例如 `{App}Context` 在建構子建立的 `{動作}Service`。
- **清單屬性命名 `{名稱}List`**；「其他」選項的自填文字命名 `{名稱}Other`。
- **屬性名稱用英文**，業務意義在行尾以中文註解標示。

## 08. Method 設計規範

```csharp
public OrderToken RefreshOrderToken(string companyId, string orderId, DateTime expireTime)
{
    #region Contracts

    ArgumentException.ThrowIfNullOrEmpty(companyId);
    ArgumentException.ThrowIfNullOrEmpty(orderId);

    #endregion

    // Order
    var order = _orderRepository.FindByOrderId(companyId, orderId);
    if (order == null) throw new InvalidOperationException($"{nameof(order)}=null");

    // OrderToken
    var orderToken = _orderTokenRepository.FindByOrderId(companyId, orderId);
    if (orderToken == null)
    {
        // Create
        ...

        // Add
        _orderTokenRepository.Add(orderToken);
    }

    // Return
    return orderToken;
}
```

- **本體固定為 `#region Contracts` → 以標頭分段的平鋪程式碼。**
- **Contracts 只檢查參數本身**（null、空字串），業務檢查放在本體。
- **分段標頭用一個字**：處理的物件名（`// Order`）或動作（`// Require`、`// Search`、`// Create`、`// Add`、`// Update`、`// Remove`、`// Copy`、`// Execute`、`// Return`），不使用巢狀 `#region`。讀標頭就能看懂流程，像是方法的目錄。
- **布林比較寫明確**：正向判斷寫 `== true`，反向寫 `== false`，不用 `!`，也不直接放裸布林；方法呼叫的結果也一樣，例如 `if (list.Any(...) == true)`、`if (list.Contains(o) == false)`。`!` 容易看漏，統一寫出比較值，閱讀時不用分辨正反。
- **提前結束寫成單行**：條件不符就結束時，`if` 與 `return`／`throw` 寫在同一行，例如 `if (order == null) throw new InvalidOperationException($"{nameof(order)}=null");`。
- **呼叫自己的實例成員加 `this.`**，例如 `this.ValidateOrderReply(orderReply)`。
- **例外依情況選型別**：
  - 找不到資料、狀態不允許：`InvalidOperationException`，例如 `$"{nameof(order)}=null"`、`$"{nameof(orderToken)} expired"`。
  - 欄位驗證失敗：`System.ComponentModel.DataAnnotations.ValidationException`，以 `ValidationResult.MemberNames` 帶出不通過的屬性名稱，讓呼叫端在對應欄位顯示錯誤。
- **依賴現在時間的方法加選填參數 `DateTime? nowTime = null`**，本體用 `var now = nowTime ?? DateTime.Now;`。呼叫端不傳，測試一律傳固定時間，不必 mock 時鐘就能測邊界。
- **public 方法在前**，private 輔助方法在後。
- **領域與 Repository 的方法是同步的**，不回傳 `Task`。

## 09. Context 設計規範

```csharp
[Service<OrderContext>(singleton: true)]
public class OrderContext
{
    // Fields
    private readonly IOrderRepository _orderRepository = null;


    // Constructors
    public OrderContext(IOrderRepository orderRepository) { ... }


    // Properties
    public IOrderRepository OrderRepository { get { return _orderRepository; } }


    // Methods
    public OrderToken RefreshOrderToken(...) { ... }
}
```

- **Context 是領域模組唯一的入口**：呼叫端只能注入 `{Context}`，透過它的屬性與方法操作模組，不注入 Repository 介面或模組裡的其他類別。
- **`{Context}` 放 `{Domain}` 根目錄**，以 `[Service<{Context}>(singleton: true)]` 註冊。
- **一個領域模組只有一個 Context。** 模組新增 Entity 時，把它的 Repository 與商業方法加進既有的 Context，不另開第二個（例如 `MDP.Companies` 新增 `CustomerNote`，就加進 `CompanyContext`）。內容太多時可用 `partial class` 分檔（例如 `CompanyContext.CustomerNotes.cs`），仍是同一個類別。有兩個入口，呼叫端就要猜該注入哪一個，模組內的規則也會散在兩處。
- **建構子注入本模組所有 Repository 介面**，並以唯讀屬性公開。
- **商業邏輯寫在 Context 的方法裡**，方法以操作命名（`RefreshOrderToken`、`ApplyOrderReply`、`CanUpdateOrderReply`）。標準流程是：依 Id 讀出 Entity → 呼叫 Entity 的方法或套用規則 → 經由 Repository 寫回。
- **單純 CRUD 不包 Context 方法**：沒有商業規則的 Entity，呼叫端直接呼叫 `{Context}.{Entity}Repository` 新增、查詢、修改、刪除。例如訂單備忘只有新增、修改、刪除，Context 只公開 `OrderNoteRepository`，不加 `AddOrderNote` 這類只轉呼叫的方法。
- **有規則的寫入只走 Context 方法**：Entity 的寫入帶有商業規則時（例如客人送出回條要檢查期限與欄位），它的 Repository 仍可公開給呼叫端查詢與刪除，但 `Add`／`Update` 只由擁有規則的 Context 方法呼叫。
- **規則型寫入的欄位驗證放在 Context**（private 的 `Validate{Entity}`），擲出 `ValidationException`。
- **`{App}Context` 組合各模組**：注入各 `{Domain}` 的 Context 與 `{App}` 的 Repository。跨模組的寫入協調放在 `{動作}Service` 類別（例如 `AppointmentService`：新增預約時同步更新客戶資料），由 `{App}Context` 在建構子建立並以屬性公開，不另外註冊 DI。
- **Context 之間只透過建構子協作**：一個 Context 可以在建構子注入另一個 Context。

## 10. Entity 設計規範

```csharp
public class OrderToken
{
    // Properties
    public string CompanyId { get; set; } = String.Empty;

    public string OrderId { get; set; } = String.Empty;

    public DateTime ExpireTime { get; set; } = DateTime.MinValue;

    public DateTime CreateTime { get; set; } = DateTime.MinValue;

    public DateTime UpdateTime { get; set; } = DateTime.MinValue;


    // Methods
    public bool CanUpdateReply(string accessKey, DateTime? nowTime = null) { ... }
}
```

- **Entity 是純資料類別**：放 `{Domain}` 根目錄，只有屬性，不繼承基底類別，也不實作 `IValidatableObject`。驗證規則依操作而不同（店員建立與客人送出檢查的欄位不一樣），放在 Context 方法或呼叫端，才知道是哪一種操作。
- **鍵在最前，日期時間在最後**：先租戶鍵 `CompanyId`，再自己的 Id，中間是業務屬性，最後以 `CreateTime`、`UpdateTime` 結尾。
- **Id 是 `string`**，以 `MDP.Domain.Identifier.NewId()` 產生，不用 `Guid` 或自動編號。
- **每個 Entity 都有 `CreateTime`、`UpdateTime`**（只由 Repository 設定，見 11 章）。修改既有 Entity 時發現缺少這兩個屬性，就直接補上，連同 Repository 的寫入與資料表欄位一起，不另外詢問，在回覆或設計文件中列為異動即可。這是每個 Entity 的基本要求，不需要使用者決定。
- **方法只能讀取或改變自己的屬性**，例如 `OrderToken.CanUpdateReply(accessKey, nowTime)`、`Order.RefreshReplyTime(nowTime)`。Entity 方法從不存取 Repository，需要讀寫其他資料的規則放在 Context。
- **不加不需要的狀態**：需求沒要求就不加狀態列舉或狀態欄位；能從既有資料推導的狀態（例如「沒有 `OrderToken` 就是尚未出示」）不另外儲存，否則衍生狀態會和來源資料不同步。
- **一對一的附屬資料拆成獨立 Entity**，例如 `Order` 與 `OrderReply`、`OrderToken`，以相同的 `CompanyId` + `OrderId` 對應，各自有 Repository。不同的人在不同時間寫入不同部分，拆開後寫入就不會互相覆蓋。

## 11. Repository 設計規範

### 介面

```csharp
public interface IOrderRepository
{
    // Methods
    void Add(Order order);

    void Update(Order order);

    void Remove(string companyId, string orderId);

    Order FindByOrderId(string companyId, string orderId);

    List<Order> FindAllByCompanyId(string companyId);
}
```

- **命名 `I{Entity}Repository`**，放 `{Domain}/Interfaces/`，namespace 是 `{Domain}`；一個 Entity 一個 Repository。
- **方法命名固定**：

  | 方法 | 用途 | 找不到時 |
  |---|---|---|
  | `void Add({Entity} entity)` | 新增 | 重複時擲出例外 |
  | `void Update({Entity} entity)` | 依鍵更新 | 不做事 |
  | `void Remove({鍵...})` | 依鍵刪除 | 不做事 |
  | `{Entity} FindBy{鍵}({鍵...})` | 查一筆 | 回傳 `null` |
  | `List<{Entity}> FindAllBy{鍵}({鍵...})` | 查多筆 | 回傳空清單 |

- **鍵參數包含租戶鍵**，例如 `FindByOrderId(string companyId, string orderId)`。每次存取都帶租戶，就不可能讀到別家的資料。
- **`CreateTime`、`UpdateTime` 只由 Repository 在寫入時設定**：`Add` 把兩者設為寫入當下的時間；`Update` 只設 `UpdateTime`，不寫入 `CreateTime`。呼叫端與 Context 從不設定這兩個屬性。

### 資料存取實作

- **依通訊協定命名 `{通訊協定}{Entity}Repository`**，例如 `Sql{Entity}Repository`、`Http{Entity}Repository`、`Grpc{Entity}Repository`。看名稱就知道資料怎麼取得，換通訊協定時只要換掉實作。
- **放 `{Domain}.Accesses` 根目錄**，namespace 是 `{Domain}.Accesses`，以 `[Service<I{Entity}Repository>()]` 註冊。

### SQL 實作

```csharp
// SqlClient
using (var sqlClient = _sqlClientFactory.CreateClient("DefaultDatabase"))
{
    // CommandText
    sqlClient.CommandText = @"
        SELECT *
        FROM   [dbo].[Orders_Orders]
        WHERE  [CompanyId] = @CompanyId
        AND    [OrderId] = @OrderId
    ";

    // CommandParameters
    sqlClient.AddParameter("@CompanyId", companyId);
    sqlClient.AddParameter("@OrderId", orderId);

    // Execute
    return sqlClient.ExecuteParse<Order>();
}
```

- **命名 `Sql{Entity}Repository`**，放置與註冊同上。
- **不用 EF Core**：建構子注入 `SqlClientFactory`，每個方法以 `CreateClient("DefaultDatabase")` 取得連線，依 `// SqlClient`、`// CommandText`、`// CommandParameters`、`// Execute` 分段。
- **一律參數化**（`AddParameter`），不拼接 SQL 字串。
- **依用途選執行方法**：寫入用 `ExecuteNonQuery`，查一筆用 `ExecuteParse<T>`，查多筆用 `ExecuteParseAll<T>`。
- **資料表命名 `[dbo].[{模組}_{Entity複數}]`**，例如 `Orders_Orders`、`Orders_OrderTokens`、`Companies_News`。
- **連線字串設定在** `MDP.Data.MSSql:SqlClientFactory:{名稱}:ConnectionString`。

### Mock 實作

- **命名 `Mock{Entity}Repository`**，放 `{Domain}.Mocks` 根目錄，namespace 是 `{Domain}.Mocks`，以 `List<{Entity}>` 把資料存在記憶體。
- **遵守與 SQL 實作相同的約定**：`Add` 重複時擲出例外、`Update` 只改 `UpdateTime`、讀寫都 Clone（存進去和拿出來的都是複本，模擬資料庫不共用物件參考）。
- **Mock 只給測試使用**：不加 `[Service]` 註冊，由測試直接建立；Accesses 與 Host 都不參考 `{Domain}.Mocks`。

## 12. Dependency Injection 設計規範

```csharp
// Host
MDP.BlazorCore.Web.Host.Run<App>(typeof(MainLayout));
```

- **以 Attribute 註冊，不寫 `services.Add...`**：在類別宣告加上 `[Service<T>]`（`MDP.Registration`），Host 啟動時掃描組件自動註冊。

  | 類別 | 註冊 |
  |---|---|
  | `{Context}`、`{App}Context` | `[Service<{Context}>(singleton: true)]` |
  | `{通訊協定}{Entity}Repository` | `[Service<I{Entity}Repository>()]` |
  | `Mock{Entity}Repository`（由測試建立） | 不註冊 |
  | `{動作}Service`（由 `{App}Context` 建立） | 不註冊 |
  | Entity、`Defaults/` | 不註冊 |

- **Host 只有一兩行**：WebApp 如上，MAUI 用 `MDP.BlazorCore.Maui.Host.CreateMauiApp<T>(...)`，不在 `Program.cs` 手動註冊服務。

## 13. Razor 設計規範

```csharp
namespace MDP.Shop.Components.Companies.Orders
{
    public partial class UpdatePage
    {
        // Controller
        public class PageController : InteropController
        {
            // Fields
            private readonly OrderContext _orderContext = null;


            // Constructors
            public PageController(OrderContext orderContext) { ... }


            // Methods
            public Task<PageModel> OnInitializedAsync(string companyId, string orderId) { ... }
        }


        // Class
        public class PageModel
        {
            // Properties
            public Order Order { get; set; } = null;
        }
    }
}
```

- **namespace 依資料夾**：頁面是 `{App}.Components.{Area}.{Feature}`，例如 `MDP.Shop.Components.Companies.Orders`，因為頁面路由與資料夾一一對應。
- **code-behind 另有兩個成員分類**：`// Controller` 放巢狀的 `PageController`，`// Class` 放巢狀的 `PageModel`、`{動作}ResultModel`，其餘依 04 章的分類與順序。
- **`PageController` 的方法回傳 `Task<T>`**（MDP.BlazorCore 的要求）。
