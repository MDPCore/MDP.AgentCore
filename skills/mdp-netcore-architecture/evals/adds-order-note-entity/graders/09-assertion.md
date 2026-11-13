---
type: llm
focus: trace
weight: 1
---

沒有在 OrderContext 新增 AddOrderNote 之類只轉呼叫 Repository 的方法；回覆說明頁面直接透過 OrderContext.OrderNoteRepository 做 CRUD
