---
type: llm
focus: trace
weight: 1
---

OrderContext 的建構子新增 IOrderNoteRepository 參數，含 ArgumentNullException.ThrowIfNull 與 // Default 賦值，並以 OrderNoteRepository 唯讀屬性公開
