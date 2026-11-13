---
type: llm
focus: trace
weight: 1
---

News 的屬性表直接補上 CreateTime、UpdateTime（News 目前沒有這兩個屬性），日期時間屬性放最後並以 CreateTime、UpdateTime 結尾；SqlNewsRepository 與資料表變更也包含這兩個欄位
