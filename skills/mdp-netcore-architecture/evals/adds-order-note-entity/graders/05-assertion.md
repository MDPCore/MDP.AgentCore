---
type: llm
focus: trace
weight: 1
---

Orders 模組的 Mocks 專案有 MockOrderNoteRepository（不放在測試專案或 Accesses 專案），讀寫都 Clone，Update 保留原 CreateTime 只更新 UpdateTime
