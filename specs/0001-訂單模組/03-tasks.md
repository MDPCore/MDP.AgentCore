# 0001 訂單模組 — 任務拆解

依據：`01-requirement.md`、`02-design.md`。依 T1 → T10 的順序實作，每個任務完成後執行 `dotnet build src/CLK.OrderGain.sln`（有單元測試的任務另執行 `dotnet test src/MBS.Orders.Tests/MBS.Orders.Tests.csproj`），並把該任務的「完成」打勾。規格有缺漏或矛盾時停下來詢問，不自行補上。

## T1 建立 `MBS.Orders` 領域專案與測試專案

- [x] 完成

內容：

- 新增 `src/MBS.Orders`（套件同 `MBS.Appointments`）：`Order`、`OrderReply`、`OrderToken`（含 `CanUpdateReply`）、`Defaults/OrderOptions.cs`（選項清單、`TermsContent` 逐字照 01「交易規範原文」、`UpdateDuration`）、`Interfaces/IOrderRepository.cs`、`IOrderReplyRepository.cs`、`IOrderTokenRepository.cs`（02 §2.2–§2.8）。
- 新增 `src/MBS.Orders.Tests`（xUnit，套件版本同 `MBS.Companies.Tests`，參考 `MBS.Orders`）：`MockOrderRepository`、`MockOrderReplyRepository`、`MockOrderTokenRepository`（02 §2.11）。
- 兩個專案加入 `CLK.OrderGain.sln` 的 `01.MBS` 方案資料夾（`MBS.Orders.Accesses` 在 T3 加入）。

AC：2.2、2.3、2.4、2.5、3.5（`CanUpdateReply` 部分）

單元測試：

- `OrderTokenTests`：`CanUpdateReply_AfterExpireTime_ReturnsFalse`、`CanUpdateReply_WithinExpireTime_ReturnsTrue`、`CanUpdateReply_AtExpireTime_ReturnsTrue`、`CanUpdateReply_OneTickAfterExpireTime_ReturnsFalse`、`CanUpdateReply_WrongAccessKey_ReturnsFalse`
- `MockRepositoryTests`：`MockRepository_AddAndUpdate_SetCreateTimeAndUpdateTime`

手動驗收：

- `OrderOptions` 的選項數量與文字和 01「欄位」一節一致（作品分類 12 項、住家區域 7 項、從哪裡認識 6 項，「書法、水墨畫」為一項）；`TermsContent` 與 01「交易規範原文」逐字一致（含換行與空白）。

## T2 `OrderContext`

- [x] 完成

內容：

- 新增 `MBS.Orders/OrderContext.cs`（`[Service<OrderContext>(singleton: true)]`）：公開三個 repository 屬性；`RefreshOrderToken`、`ApplyOrderReply` 與 private 的客戶回條欄位檢查（02 §2.9），`ValidationException` 的 `MemberNames` 帶出不通過的屬性名稱；`ApplyOrderReply` 不整理資料。

AC：2.1、2.3、2.4、3.2、3.3、3.4、3.5、3.6、3.7、3.8、4.4、5.2（Context 部分）

單元測試（`OrderContextTests`）：

- `RefreshOrderToken_FirstTime_AddsTokenAndReturnsIt`
- `RefreshOrderToken_Again_RegeneratesAccessKeyAndOverwritesExpireTime`
- `RefreshOrderToken_OrderNotFound_Throws`
- `RefreshOrderToken_DoesNotChangeOrderOrReply`
- `ApplyOrderReply_FirstTime_AddsReply`
- `ApplyOrderReply_OrderNotFound_ThrowsAndDoesNotSave`、`ApplyOrderReply_OtherCompanyId_ThrowsAndDoesNotSave`
- `ApplyOrderReply_InvalidFields_ThrowsAndDoesNotSave`
- `ApplyOrderReply_InvalidMail_ThrowsWithMailMemberName`
- `ApplyOrderReply_OtherTextWithoutOtherOption_SavesAsIs`
- `ApplyOrderReply_AlreadyReplied_OverwritesReplyFields`
- `ApplyOrderReply_NeverPresented_ThrowsAndDoesNotSave`、`ApplyOrderReply_AfterExpireTime_ThrowsAndDoesNotSave`
- `ApplyOrderReply_OldAccessKey_ThrowsAndDoesNotSave`
- `ApplyOrderReply_DoesNotChangeOrderOrToken`
- `ApplyOrderReply_AfterStaffUpdateAndRefresh_Succeeds`

手動驗收：無（全部以單元測試涵蓋）。

## T3 `MBS.Orders.Accesses` 與專案串接

- [x] 完成

內容：

- 新增 `src/MBS.Orders.Accesses`（參考 `MBS.Orders`，套件同 `MBS.Appointments.Accesses`），加入 `CLK.OrderGain.sln` 的 `01.MBS`。
- **暫以記憶體 repository 代替 SQL**：`MockOrderRepository`、`MockOrderReplyRepository`、`MockOrderTokenRepository`，以 `[Service<I*Repository>(singleton: true)]` 註冊，行為遵守 02 §2.6–§2.8 的約定（同 `MBS.Orders.Tests` 的 Mock，另加 `lock` 以支援同時存取）。資料只存在記憶體，WebApp 重新啟動即清空。SQL repository、`StringListTypeHandler`、`Scripts/CreateTables.sql` 移到 T10，屆時刪除這三個記憶體 repository。
- `CLK.OrderGain.csproj` 加入 `MBS.Orders` 的 ProjectReference；`CLK.OrderGain.Accesses.csproj` 加入 `MBS.Orders.Accesses` 的 ProjectReference。
- `CLK.OrderGain.CoreApp/_Imports.razor` 加 `@using MBS.Orders;`。

AC：無直接對應（為 T4–T8 的頁面提供資料存取）

單元測試：無（記憶體 repository 與 `MBS.Orders.Tests` 的 Mock 行為相同，已由 T1 的 `MockRepositoryTests` 涵蓋其約定）。

手動驗收：

- `dotnet run --project src/CLK.OrderGain.WebApp` 啟動成功，既有頁面正常。
- `OrderContext` 與三個記憶體 repository 的 DI 解析，要等頁面注入 `OrderContext` 後才能驗證，在 T4 透過頁面驗收。

## T4 建立訂單與訂單明細（顯示）

- [x] 完成

內容：

- `Components/Companies/Orders/NewPage`（02 §4.3）：欄位、前端驗證（錯誤時「建立」不能按，取消勾選「其他」不清除文字）、`AddOrderAsync`（整理資料 → 伺服器端檢查 → 以 `FindAllByCompanyId` 做不分大小寫的重複檢查 → 新增）。
- `Components/Companies/Orders/DetailPage` 的顯示部分（02 §4.4）：訂單、工作項目、店內欄位、客戶回條（沒有時顯示「(無)」），`*Other` 只在勾選「其他」時顯示；「訂單修改」「訂單取消」按鈕連到 T6 的頁面（T6 完成前點擊會找不到頁面，屬預期），「客戶填寫」按鈕在 T7 實作。

AC：1.1、1.2、1.3、1.4、4.1（顯示部分）、4.4

單元測試：無（頁面邏輯以手動驗收）。

手動驗收：

- 開啟建立頁時 `OrderContext` 與記憶體 repository 能被 DI 解析（T3 補驗）。
- 建立後進入明細頁，各欄位顯示正確；尚無客戶回條時客人欄位顯示「(無)」（有客戶回條的顯示在 T8 驗收）。
- 訂單號碼、內容物、付款狀況空白，未勾選作品分類，勾選「其他」未輸入文字，成交金額空白、非數字或超過 `int` 上限時，對應欄位顯示錯誤且「建立」不能按。
- 交貨日期的日曆直接顯示，預設選今天；今天以前的日期不能選；選其他日期後明細頁顯示該日期。
- 裱框內容、訂單備註的輸入框高度同 `Sites/Appointments/NewPage` 的需求備註（`rows="10"`）。
- 欄位標籤都是四個字並對齊（02 §4 標籤表）；沒有資料的欄位顯示「(無)」。
- 同一商家以 `A001`、`a001` 建立顯示「訂單號碼重複」；全形 `Ａ００１` 可建立；另一商家用 `A001` 可建立；訂單號碼前後空白被去除。
- 勾選「其他」輸入文字後取消勾選仍可建立，明細頁不顯示該文字。
- 以 A 商家帳號開 B 商家的建立頁與明細頁網址被拒。
- 作品分類（含「書法、水墨畫」「框物手作(黑板、網格、軟木留言板...等)」）存入後在明細頁讀出正確（SQL 的往返在 T10 驗收）。

## T5 訂單列表與底部導覽

- [x] 完成

內容：

- `Components/Companies/Orders/ListPage`（02 §4.2）：`MainHeader`「新增」→ NewPage、`MainFooter`；依 `CreateTime` 由新到舊列出，對應客戶回條顯示姓名與電話（沒有時「(無)」）；瀏覽器端即時查詢（不分大小寫）。
- `Components/Shared/MainFooter.razor`、`Components/Shared/MainSidebar.razor`（02 §4.1）：在「預約行事曆」與「系統設定」之間加入「訂單」。

AC：4.2、4.3、4.4、6.1、6.2

單元測試：無。

手動驗收：

- 員工帳號底部導覽與側邊選單都依序為「商家首頁、預約行事曆、訂單、系統設定」，點「訂單」進入列表，「新增」進入建立頁。
- 非商家成員瀏覽商家首頁、預約頁，底部導覽與側邊選單都看不到「訂單」，與原本相同。
- 列表依建立時間由新到舊；以訂單號碼、客人姓名、客戶手機的部分文字查詢只顯示符合的訂單。
- 以 A 商家帳號開 B 商家的列表網址被拒。

## T6 修改與刪除訂單

- [x] 完成

內容：

- `Components/Companies/Orders/UpdatePage`（02 §4.5）：帶出現值，整理資料與檢查同 NewPage，重複檢查排除自己；只更新店員欄位；完成後 toast「已完成」並返回明細頁。
- `Components/Companies/Orders/RemovePage`（02 §4.6）：刪除確認頁（訂單號碼、客人姓名或「(無)」、成交金額）；「確認刪除」依序刪除 `OrderToken`、`OrderReply`、`Order` 後導向列表；「取消」返回明細頁。

AC：5.1、5.2（店員端）、5.3

單元測試：無。

手動驗收：

- 修改各欄位後明細正確；改成同商家其他訂單的號碼（含只差大小寫）顯示「訂單號碼重複」；維持原號碼或只改自己號碼的大小寫可儲存。
- 明細頁沒有修改客戶回條的入口。
- 刪除確認頁按「取消」不刪除；按「確認刪除」後列表不見該訂單。
- 有與沒有客戶回條、`OrderToken` 的訂單都能刪除（有 `OrderToken`、客戶回條的情形在 T7、T8 完成後補驗）。

## T7 出示 QR Code

- [x] 完成

內容：

- `DetailPage` 加入「客戶填寫」按鈕（02 §4.4）：`PresentOrderAsync` 計算 `DateTime.Now + OrderOptions.UpdateDuration`，呼叫 `RefreshOrderToken`，以 `AccessKey` 組填寫網址、`QRCodeContext.CreateToBase64` 產生圖片；前端切到 `qrcode-view` 顯示 QR Code、訂單號碼與「填寫期限：yyyy-MM-dd HH:mm」，「返回」重新載入明細頁。

AC：2.1、2.4（店員端）

單元測試：無（`RefreshOrderToken` 已在 T2 涵蓋）。

手動驗收：

- 按「客戶填寫」按鈕顯示 QR Code、訂單號碼與截止時間（現在起 30 分鐘）。
- 再次出示（含逾期後），截止時間從這次起重新計算，QR Code 內容（網址中的 `AccessKey`）改變。
- 重新整理明細頁不會改變 `AccessKey` 與期限。

## T8 客戶回條頁

- [x] 完成

內容：

- `Components/Sites/OrderReplys/UpdatePage`（02 §4.7）：`[AllowAnonymous]`，不放 `MainFooter`、`MainSidebar`；`OnInitializedAsync` 判斷訂單存在與 `CanUpdateReply`，`PageModel` 只放允許的欄位（不含 `PaymentStatus`、`Note`；`WorkCategoryOther` 只在勾選「其他」時填入）；三種呈現（不存在／過期／表單）；交易規範以 `pre-wrap` 顯示、同意不預先勾選；前端驗證（Email 格式除外）；`ApplyOrderReplyAsync`（notFound → expired → 同意檢查 → 整理資料 → `ApplyOrderReply`，`invalid` 帶 `memberName`）。

AC：2.2、2.3、2.4、2.5、3.1、3.2、3.3、3.4、3.5、3.6、3.7、3.8、4.1、5.2（客人端）

單元測試：無（`ApplyOrderReply` 已在 T2 涵蓋）。

手動驗收：

- 用未登入的手機瀏覽器掃描 QR Code，不要求登入即開啟填寫頁。
- 畫面顯示商家名稱、訂單號碼、工作項目、成交金額、預計交貨日期與交易規範（與 01「交易規範原文」一致且保留換行）；這些內容都不能修改；畫面與頁面原始碼都沒有付款狀況與小備註。
- 店員先勾選作品分類「其他」並輸入文字，再取消勾選後出示：填寫頁畫面與頁面原始碼都沒有該文字。
- 第一次開啟表單為空白；任一必填空白、「從哪裡認識 裱室 ?」未勾選、勾選「其他」未輸入文字或未勾選同意時，對應欄位顯示錯誤且「送出」不能按；Email 格式錯誤時可按「送出」，送出後客戶信箱欄位顯示「Email 格式錯誤」且不儲存；Email 前後有空白可正常送出，儲存值不含空白。
- 送出成功顯示成功訊息；店員重新整理明細頁看到客人欄位。
- 期限內重開帶出上次內容且同意未勾選，再次送出覆蓋前一次。
- 用瀏覽器開發者工具送出未勾選同意的請求被拒；在請求中加入 `PaymentStatus`、`Amount`、`Content` 等欄位送出，訂單的工作項目與店內欄位不變（AC-3.6）。
- 把 `OrderToken.ExpireTime` 改成過去時間後，重開與送出都顯示「填寫連結已過期，請洽店員重新出示 QR Code」。
- 用舊 QR Code 開著填寫頁時店員重新出示，按「送出」顯示過期訊息且不儲存；舊 QR Code 重開也顯示過期訊息。
- 隨意改網址中的 `AccessKey` 顯示過期訊息；隨意改網址中的 `OrderId` 或刪除訂單後開啟，顯示「此訂單已不存在」。
- 店員修改工作項目、成交金額後重新出示，填寫頁顯示最新內容，客人欄位帶出上次資料。
- 補驗 T6：有客戶回條與 `OrderToken` 的訂單可以刪除。

## T9 文件同步與整體驗收

- [x] 完成

內容：

- 更新 `CLAUDE.md`（02 §5）：模組說明（三個模組 → 四個，加入 `MBS.Orders`）、依賴方向、Commands（加入 `dotnet test src/MBS.Orders.Tests/MBS.Orders.Tests.csproj`）、測試專案描述。

AC：全部（回歸）

單元測試：`dotnet test src/MBS.Companies.Tests/MBS.Companies.Tests.csproj`、`dotnet test src/MBS.Appointments.Tests/MBS.Appointments.Tests.csproj`、`dotnet test src/MBS.Orders.Tests/MBS.Orders.Tests.csproj` 全部通過。

手動驗收：

- 依 02 §6.2 從頭走一次完整流程：建立 → 列表查詢 → 出示 QR Code → 客人填寫 → 店員重新整理 → 修改 → 重新出示 → 客人再次填寫 → 刪除。
- 既有功能（預約、行事曆、各項設定、員工邀請 QR Code）不受影響。

## T10 SQL repository（取代記憶體 repository）

- [ ] 完成

內容：

- `MBS.Orders.Accesses` 新增 `SqlOrderRepository`、`SqlOrderReplyRepository`、`SqlOrderTokenRepository`（`[Service<I*Repository>()]`）、`StringListTypeHandler`（複選欄位存 JSON）、`Scripts/CreateTables.sql`（三張資料表），依 02 §2.10。
- 刪除 T3 的三個記憶體 repository（`MBS.Orders.Accesses/Mock*Repository.cs`）。

AC：無直接對應（資料持久化）

單元測試：無（SQL repository 不寫單元測試）。

手動驗收：

- 在**開發用**資料庫執行 `CreateTables.sql`，三張資料表建立成功，欄位型別與 02 §2.10 一致（不可對正式環境資料庫執行，需確認連線字串）。
- WebApp 啟動成功，訂單頁面能解析 SQL repository。
- 新增、讀取、修改、刪除訂單與客戶回條正確；作品分類（含「書法、水墨畫」「框物手作(黑板、網格、軟木留言板...等)」）與「從哪裡認識 裱室 ?」的複選欄位往返正確；`CreateTime`／`UpdateTime` 有寫入，`Update` 不改 `CreateTime`；超過 1,000 字的內容物、小備註可儲存與讀出。
- WebApp 重新啟動後資料仍在。
- 依 02 §6.2 重跑 T4–T8 的手動驗收。
