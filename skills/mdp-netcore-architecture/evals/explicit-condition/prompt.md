---
tags: [skill-creator]
max_turns: 30
timeout_seconds: 900
allowed_tools: [Read, Glob, Grep, Skill, Write, Edit]
---

在 OrderContext 加一個方法 CanRemoveOrder(companyId, orderId)：訂單已經有回條、或訂單建立超過 30 天，就不能刪除。
