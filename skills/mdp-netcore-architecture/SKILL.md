---
name: mdp-netcore-architecture
description: MDP.NetCore 的 .NET 架構規範：方案與專案分層（{Domain}／{Domain}.Tests／{Domain}.Mocks／{Domain}.Accesses／{App}／Host）、依賴方向、Namespace／Class／Field／Constructor／Properties／Method 寫法，以及 Context、Entity、Repository、Dependency Injection、Razor 頁面 code-behind 的設計規則。只要在任何 .NET 專案（有 .sln、.csproj 或 .cs 檔）中撰寫、修改、審查 C# 程式碼，新增專案、Entity、Repository、Context、頁面，或在系統設計文件中設計物件模型，都要使用這個 Skill，即使使用者沒有提到「架構」或「規範」。
---

# MDP.NetCore 架構

規則全文在 [references/architecture.md](references/architecture.md)，這是給人看的文件，也是規則的唯一來源。本檔只說明怎麼使用它。

## 使用方式

1. **動手前讀完整份 [references/architecture.md](references/architecture.md)**，不挑章節。

2. **寫完後對照規範逐條檢查**，再交付。

3. **規範與現有程式碼不一致時**：新寫的程式碼照規範；不主動改寫既有程式碼（除非使用者要求），但在回覆中指出不一致的地方。專案 `CLAUDE.md` 對該專案有明確不同的規定時，以專案為準，並一樣指出差異。

4. **規範沒涵蓋的情況**：依相關規則的用意推論，在回覆中說明判斷依據。
