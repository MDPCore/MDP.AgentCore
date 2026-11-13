---
type: llm
focus: trace
weight: 1
---

指出不應在 Program.cs 用 AddSingleton 手動註冊，應以 [Service<T>(singleton: true)] 註冊 Context（或併入既有 CompanyContext，沿用其註冊）
