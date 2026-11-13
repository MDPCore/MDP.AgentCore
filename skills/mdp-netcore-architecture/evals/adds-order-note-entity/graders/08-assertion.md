---
type: llm
focus: trace
weight: 1
---

有處理 OrderContext 建構子變更的連帶影響：更新既有建構 OrderContext 的測試，改用 Mocks 專案的 MockOrderNoteRepository；WebApp 執行期經由 Accesses 專案的 SqlOrderNoteRepository 解析 IOrderNoteRepository，或在回覆中明確指出
